/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */

using System;
using System.Collections.Generic;
using System.Linq;

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Class Methods ---
        private static bool ShowDebug(DreamContext context) {
            switch(context.GetParam("format", null) ?? context.GetParam("output", "cooked")) {
            case "raw":
            case "debug":
                return true;
            case "seared":
            case "xml":
                return false;
            case "cooked":
            case "html":
                return false;
            default:
                throw new OutputParameterInvalidArgumentException();
            }
        }

        private static bool ShowXml(DreamContext context) {
            switch(context.GetParam("format", null) ?? context.GetParam("output", "cooked")) {
            case "raw":
            case "debug":
                return false;
            case "seared":
            case "xml":
                return true;
            case "cooked":
            case "html":
                return false;
            default:
                throw new OutputParameterInvalidArgumentException();
            }
        }

        //--- Features ---
        [DreamFeature("GET:site/nav/{pageid}/full", "Retrieve <div> tags of the full navigation tree for the given page.")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("width", "int?", "Max width for visible text")]
        [DreamFeatureParam("format", "{debug, xml, html}?", "Output format (default: html).")]
        [DreamFeatureParam("type", "{compact, expandable}?", "Navigation type (default: compact).")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Browse access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetNavigationFull(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            CheckResponseCache(context, false);

            PageBE page = PageBL_GetPageFromUrl(context, false);
            if (page.Title.IsTalk) {
                page = PageBL.GetPageByTitle(page.Title.AsFront());
            }

            // check if requestest page exists, otherwise find nearest parent
            uint new_page_id = NavBL.NEW_PAGE_ID;
            ulong homepageId = DekiContext.Current.Instance.HomePageId;
            List<NavBE> list;
            bool expandableNav = context.GetParam("type", "compact").EqualsInvariantIgnoreCase("expandable");
            
            // check if a page was found
            if(page.ID == 0) {

                // find nearest ancestor and authorize access
                PageBE ancestor = page;
                while(!ancestor.Title.IsHomepage) {

                    // fetch ancestor page based on title
                    ulong id = DbUtils.CurrentSession.Nav_GetNearestParent(ancestor.Title);
                    ancestor = PageBL.GetPageById(id);
                    if(PermissionsBL.IsUserAllowed(DekiContext.Current.User, ancestor, Permissions.BROWSE)) {
                        break;
                    }

                    // determine parent page title
                    Title title = ancestor.Title.GetParent();
                    if(title == null) {

                        // current ancestor was the homepage
                        break;
                    }
                    ancestor = new PageBE { Title = title };
                }
                if(ancestor.ID == 0) {
                    ancestor = PageBL.GetHomePage();
                }
                list = NavBL.QueryNavTreeData(ancestor, context.Culture, expandableNav).ToList();

                // find the nearest parent node and increase its child count
                foreach(NavBE nearestAncestors in list) {
                    if(nearestAncestors.Id == ancestor.ID) {
                        nearestAncestors.ChildCount = nearestAncestors.ChildCount + 1;
                        break;
                    }
                }

                // for each missing node, generate a dummy page and insert it into result set
                ulong ancestor_id = ancestor.ID;
                string[] ancestor_segments = ancestor.Title.AsUnprefixedDbSegments();
                string[] new_page_segments = page.Title.AsUnprefixedDbSegments();
                List<NavBE> newNodes = new List<NavBE>(32);
                for(int i = ancestor_segments.Length; i < new_page_segments.Length; ++i) {
                    string title = string.Join("/", new_page_segments, 0, i + 1);

                    // create dummy node with <page><page_id /><page_namespace /><page_title ><page_parent /><page_children /></page>
                    NavBE newPage = new NavBE {
                        Id = new_page_id, 
                        NameSpace = (ushort)page.Title.Namespace, 
                        Title = title, 
                        ParentId = (ancestor_id == homepageId) ? 0 : ancestor_id, 
                        ChildCount = (i == new_page_segments.Length - 1) ? 0 : 1
                    };
                    newNodes.Add(newPage);

                    // update page information
                    page.ID = new_page_id;
                    page.ParentID = ancestor_id;
                    ancestor_id = new_page_id++;
                }

                // check if we need to remove the children nodes of the ancestor
                ancestor_id = (ancestor.ID == homepageId) ? 0 : (uint)ancestor.ID;
                if(!expandableNav && (new_page_segments.Length - ancestor_segments.Length) > 1) {

                    // remove ancestor children and add new dummy nodes
                    for(int start = 0; start < list.Count; ++start ) {

                        // check if we found a matching child
                        if((list[start].ParentId == ancestor_id) && (list[start].Id != homepageId)) {

                            // look for last child to remove so we can remove an entire range at once
                            int end = start + 1;
                            for(; (end < list.Count) && (list[end].ParentId == ancestor_id) && (list[end].Id != homepageId); ++end) { }
                            list.RemoveRange(start, end - start);
                            --start;
                        }
                    }
                } else {

                    // find where among the ancestor children we need to insert the dummy node
                    for(int index = 0; index < list.Count; ++index) {
                        NavBE current = list[index];
                        if((current.ParentId == ancestor_id) && (current.Id != homepageId)) {
                            string[] parts = Title.FromDbPath(NS.UNKNOWN, current.Title, current.DisplayName).AsUnprefixedDbSegments();
                            if((parts.Length > 0) && (new_page_segments.Length > 0) && (string.Compare(parts[parts.Length - 1], new_page_segments[parts.Length - 1], true, context.Culture) > 0)) {

                                // found the spot
                                list.InsertRange(index, newNodes);
                                newNodes = null;
                                break;
                            }
                        }
                    }
                }

                // check if we didn't find the spot
                if(newNodes != null) {

                    // add new nodes to the end
                    list.AddRange(newNodes);
                }
            } else {
                list = NavBL.QueryNavTreeData(page, context.Culture, expandableNav).ToList();
            }

            // find first parent
            ulong parent_id = homepageId;
            int parent_index = -1;
            for(int i = 0; i < list.Count; ++i) {
                if(list[i].Id == parent_id) {
                    parent_index = i;
                    break;
                }
            }
            if(parent_index == -1) {
                throw new Exception("unexpected [homepage not found]");
            }

            // add any missing ancestor nodes (might have been removed by permission check or might simply not exist)
            string[] page_segments = page.Title.AsUnprefixedDbSegments();
            ushort ns = (ushort)page.Title.Namespace;
            for(int i = 0; i <= page_segments.Length; ++i) {
                string title = string.Join("/", page_segments, 0, i);

                // loop over all nodes
                bool found = false;
                for(int j = 0; j < list.Count; ++j) {
                    NavBE node = list[j];

                    // NOTE (steveb): we walk the path one parent at a time; however, there are a few special cases, because of namespaces
                    //      for instance, the parent page of User:Bob is the homepage (ditto for Template:Page), but the parent page of Admin:Config is Admin:

                    // check if we found a node matching the current title
                    if((string.Compare(node.Title, title, true, context.Culture) == 0) && (((i == 0) && (ns != (ushort)NS.ADMIN)) ? (node.NameSpace == (ushort)NS.MAIN) : (node.NameSpace == ns))) {
                        found = true;

                        // let's make sure node is pointing to right parent
                        node.ParentId = (parent_id == homepageId) ? 0 : parent_id;
                        parent_id = node.Id;
                        parent_index = j;
                        break;
                    }
                }
                if(!found) {

                    // node is missing, let's create a new one
                    NavBE newPage = new NavBE {
                        Id = new_page_id, 
                        NameSpace = (ushort)page.Title.Namespace, 
                        Title = title, 
                        ParentId = (parent_id == homepageId) ? 0 : parent_id, ChildCount = 1
                    };

                    // add new page after parent
                    list.Insert(parent_index + 1, newPage);
                    parent_id = new_page_id++;
                    parent_index = parent_index + 1;
                }
            }

            // build response
            if(ShowDebug(context)) {
                response.Return(DreamMessage.Ok(NavBL.ConvertNavPageListToDoc(list)));
            } else {
                XDoc result = expandableNav ? NavBL.ComputeExpandableNavigationDocument(list, page, 0, 0, false) : NavBL.ComputeNavigationDocument(list, page, 0, 0, false, context.GetParam("width", int.MaxValue));
                if(ShowXml(context)) {
                    response.Return(DreamMessage.Ok(result));
                } else {
                    response.Return(DreamMessage.Ok(new XDoc("tree").Value(result.Contents)));
                }
            }
            yield break;
        }

        [DreamFeature("GET:site/nav/{pageid}/children", "Retrieve <div> tags for the sub-pages of the given page.")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("exclude", "int?", "Page to exclude from result set.")]
        [DreamFeatureParam("width", "int?", "Max width for visible text")]
        [DreamFeatureParam("format", "{debug, xml, html}?", "Output format (default: html).")]
        [DreamFeatureParam("type", "{compact, expandable}?", "Navigation type (default: compact).")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Browse access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetNavigationChildren(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            CheckResponseCache(context, false);

            PageBE page = PageBL_GetPageFromUrl(context, false);
            if (page.Title.IsTalk) {
                page = PageBL.GetPageByTitle(page.Title.AsFront());
            }

            // build response
            uint exclude = context.GetParam<uint>("exclude", 0);
            IList<NavBE> list = NavBL.QueryNavChildrenData(page, context.Culture);
            if(ShowDebug(context)) {
                response.Return(DreamMessage.Ok(NavBL.ConvertNavPageListToDoc(list)));
            } else {
                bool expandableNav = context.GetParam("type", "compact").EqualsInvariantIgnoreCase("expandable");
                XDoc doc = expandableNav ? NavBL.ComputeExpandableNavigationDocument(list, page, 0, exclude, true) : NavBL.ComputeNavigationDocument(list, page, 0, exclude, true, context.GetParam("width", int.MaxValue));
                if(ShowXml(context)) {
                    response.Return(DreamMessage.Ok(doc));
                } else {
                    XDoc result = new XDoc("tree");
                    result.Start("children");

                    // add name of children nodes
                    System.Text.StringBuilder nodes = new System.Text.StringBuilder();
                    ulong homepageId = DekiContext.Current.Instance.HomePageId;
                    ulong parentId = (page.ID == homepageId) ? 0 : page.ID;
                    foreach(NavBE child in list) {
                        if((child.ParentId == parentId) && (child.Id != homepageId)) {
                            if(nodes.Length != 0) {
                                nodes.Append(",");
                            }
                            nodes.AppendFormat("n{0}", child.Id);
                        }
                    }
                    result.Elem("nodes", nodes.ToString());

                    // add <div> list
                    result.Start("html");
                    if(exclude != 0) {
                        result.Start("pre").Value(doc["children-pre"].Contents).End();
                        result.Start("post").Value(doc["children-post"].Contents).End();
                    } else {
                        result.Value(doc.Contents);
                    }
                    result.End();
                    result.End();
                    response.Return(DreamMessage.Ok(result));
                }
            }
            yield break;
        }

        [DreamFeature("GET:site/nav/{pageid}/children,siblings", "Retrieve <div> tags for the sub-pages and sibling pages of the given page.")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("exclude", "int?", "Page to exclude from result set.")]
        [DreamFeatureParam("width", "int?", "Max width for visible text")]
        [DreamFeatureParam("format", "{debug, xml, html}?", "Output format (default: html).")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Browse access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetNavigationChildrenSiblings(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            CheckResponseCache(context, false);

            PageBE page = PageBL_GetPageFromUrl(context, false);
            if (page.Title.IsTalk) {
                page = PageBL.GetPageByTitle(page.Title.AsFront());
            }

            // build response
            uint exclude = context.GetParam<uint>("exclude", 0);
            IList<NavBE> list = NavBL.QueryNavSiblingsAndChildrenData(page, context.Culture);
            if(ShowDebug(context)) {
                response.Return(DreamMessage.Ok(NavBL.ConvertNavPageListToDoc(list)));
            } else {
                XDoc doc = NavBL.ComputeNavigationDocument(list, page, (uint) page.ID, exclude, true, context.GetParam("width", int.MaxValue));
                if(ShowXml(context)) {
                    response.Return(DreamMessage.Ok(doc));
                } else {
                    XDoc result = new XDoc("tree");
                    result.Start("siblings");

                    // add name of sibling nodes
                    System.Text.StringBuilder nodes = new System.Text.StringBuilder();
                    ulong homepageId = DekiContext.Current.Instance.HomePageId;
                    foreach(NavBE sibling in list) {
                        if((sibling.ParentId == page.ParentID) && (sibling.Id != homepageId)) {
                            if(nodes.Length != 0) {
                                nodes.Append(",");
                            }
                            nodes.AppendFormat("n{0}", sibling.Id);
                        }
                    }
                    result.Elem("nodes", nodes.ToString());

                    // add sibling nodes
                    result.Start("html");
                    result.Elem("pre", doc["siblings-pre"].Contents);
                    result.Elem("post", doc["siblings-post"].Contents);
                    result.End();
                    result.End();

                    // add name of children nodes
                    result.Start("children");
                    nodes = new System.Text.StringBuilder();
                    ulong parentId = (page.ID == homepageId) ? 0 : page.ID;
                    foreach(NavBE child in list) {
                        if((child.ParentId == parentId) && (child.Id != homepageId)) {
                            if(nodes.Length != 0) {
                                nodes.Append(",");
                            }
                            nodes.AppendFormat("n{0}", child.Id);
                        }
                    }
                    result.Elem("nodes", nodes.ToString());

                    // add <div> list
                    result.Start("html");
                    if(exclude != 0) {
                        result.Elem("pre", doc["children-pre"].Contents);
                        result.Elem("post", doc["children-post"].Contents);
                    } else {
                        result.Value(doc["children-post"].Contents);
                    }
                    result.End();
                    result.End();
                    response.Return(DreamMessage.Ok(result));
                }
            }
            yield break;
        }

        [DreamFeature("GET:site/nav/{pageid}/siblings", "Retrieve <div> tags for sibling pages of the given page.")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("width", "int?", "Max width for visible text")]
        [DreamFeatureParam("format", "{debug, xml, html}?", "Output format (default: html).")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Browse access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetNavigationSiblings(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            CheckResponseCache(context, false);

            PageBE page = PageBL_GetPageFromUrl(context, false);
            if (page.Title.IsTalk) {
                page = PageBL.GetPageByTitle(page.Title.AsFront());
            }

            // build response
            IList<NavBE> list = NavBL.QueryNavSiblingsData(page, context.Culture);
            if(ShowDebug(context)) {
                response.Return(DreamMessage.Ok(NavBL.ConvertNavPageListToDoc(list)));
            } else {
                XDoc doc = NavBL.ComputeNavigationDocument(list, page, (uint)page.ID, 0, true, context.GetParam("width", int.MaxValue));
                if(ShowXml(context)) {
                    response.Return(DreamMessage.Ok(doc));
                } else {
                    XDoc result = new XDoc("tree");
                    result.Start("siblings");

                    // add name of sibling nodes
                    System.Text.StringBuilder nodes = new System.Text.StringBuilder();
                    ulong homepageId = DekiContext.Current.Instance.HomePageId;
                    foreach(NavBE sibling in list) {
                        if((sibling.ParentId == page.ParentID) && (sibling.Id != homepageId)) {
                            if(nodes.Length != 0) {
                                nodes.Append(",");
                            }
                            nodes.AppendFormat("n{0}", sibling.Id);
                        }
                    }
                    result.Elem("nodes", nodes.ToString());

                    // add sibling nodes
                    result.Start("html");
                    result.Elem("pre", doc["siblings-pre"].Contents);
                    result.Elem("post", doc["siblings-post"].Contents);
                    result.End();
                    result.End();
                    response.Return(DreamMessage.Ok(result));
                }
            }
            yield break;
        }
    }
}
