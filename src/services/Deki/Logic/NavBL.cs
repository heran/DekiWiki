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
using System.Globalization;
using System.Linq;

using MindTouch.Deki.Data;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public static class NavBL {

        //--- Constants ---
        internal const int NEW_PAGE_ID = 1000000000;

        //--- Types ---
        internal enum NavDocStage {
            None,
            Ancestors,
            SiblingPre,
            ChildrenPre,
            ChildrenPost,
            SiblingPost
        }

        public class ExpandableNavPage {

            //--- Fields ---
            public List<ExpandableNavPage> Children = new List<ExpandableNavPage>();
            public bool IsHomepage;
            public ExpandableNavPage Parent;
            public bool LastParent;
            internal NavBE NavPage;
            private bool _isAncestor;
            private bool _isSelected;

            //--- Constructors ---
            public ExpandableNavPage(NavBE navPage) {
                this.NavPage = navPage;
            }

            //--- Properties ---
            public bool IsAncestor {
                get { return _isAncestor; }
                set {
                    _isAncestor = value;
                    if(value && Parent != null && Parent.NavPage.Id != DekiContext.Current.Instance.HomePageId) {
                        Parent.IsAncestor = true;
                    }
                }
            }

            public bool IsSelected {
                get { return _isSelected; }
                set {
                    _isSelected = value;
                    if(value && Parent != null) {
                        Parent.IsAncestor = true;
                    }
                }
            }
        }

        //--- Class Methods ---
        public static IList<NavBE> QueryNavTreeData(PageBE page, CultureInfo culture, bool includeAllChildren) {
            IList<NavBE> navPages = DbUtils.CurrentSession.Nav_GetTree(page, includeAllChildren);
            navPages = FilterDisallowedNavRecords(navPages, page);
            navPages = SortNavRecords(navPages, culture);
            return navPages;
        }

        public static IList<NavBE> QueryNavSiblingsData(PageBE page, CultureInfo culture) {
            IList<NavBE> navPages = DbUtils.CurrentSession.Nav_GetSiblings(page);
            navPages = FilterDisallowedNavRecords(navPages);
            navPages = SortNavRecords(navPages, culture);
            return navPages;
        }

        public static IList<NavBE> QueryNavChildrenData(PageBE page, CultureInfo culture) {
            IList<NavBE> navPages = DbUtils.CurrentSession.Nav_GetChildren(page);
            navPages = FilterDisallowedNavRecords(navPages);
            navPages = SortNavRecords(navPages, culture);
            return navPages;
        }

        public static IList<NavBE> QueryNavSiblingsAndChildrenData(PageBE page, CultureInfo culture) {
            IList<NavBE> navPages = DbUtils.CurrentSession.Nav_GetSiblingsAndChildren(page);
            navPages = FilterDisallowedNavRecords(navPages);
            navPages = SortNavRecords(navPages, culture);
            return navPages;
        }

        public static XDoc ConvertNavPageListToDoc(IList<NavBE> navPages) {
            XDoc ret = new XDoc("pages");
            foreach(NavBE np in navPages) {
                ret.Start("page")
                    .Start("page_id").Value(np.Id).End()
                    .Start("page_namespace").Value(np.NameSpace).End()
                    .Start("page_title").Value(np.Title).End()
                    .Start("page_parent").Value(np.ParentId).End()
                    .Start("page_children").Value(np.ChildCount).End()
                .End();
            }
            return ret;
        }

        public static XDoc ComputeExpandableNavigationDocument(IList<NavBE> list, PageBE page, uint splitSibling, uint splitChildren, bool hidden) {
            return CreateExpandableTree(list, page);
        }

        public static XDoc ComputeNavigationDocument(IList<NavBE> list, PageBE page, uint splitSibling, uint splitChildren, bool hidden, int max_width) {
            XDoc result = new XDoc("tree");
            List<string> css = new List<string>();
            List<string> fetchedChildren = new List<string>();
            List<NavBE> childrenNodes = new List<NavBE>();
            ulong homepageId = DekiContext.Current.Instance.HomePageId;
            NavDocStage stage = NavDocStage.None;
            if(splitSibling > 0) {
                stage = NavDocStage.SiblingPre;
                result.Start("siblings-pre");
            } else if(splitChildren > 0) {
                stage = NavDocStage.ChildrenPre;
                result.Start("siblings-pre").End();
                result.Start("children-pre");
            }
            int splitSiblingIndex = 0;
            int splitChildrenIndex = 0;

            // fix page_parent_id: it's stored as zero for home and children of home
            ulong page_parent_id = page.ParentID;
            if((page_parent_id == 0) && (page.ID != homepageId)) {
                page_parent_id = homepageId;
            }
            int page_index = 0;

            // iterate over result set
            int siblingIndex = 0;
            int childIndex = 0;
            foreach(NavBE node in list) {

                // retrieve page values
                uint node_id = node.Id;
                ulong node_parent_id = node.ParentId;
                bool virtual_node = (node_id >= NEW_PAGE_ID);

                // fix parent_id: it's stored as zero for home and children of home
                if((node_parent_id == 0) && (node_id != homepageId)) {
                    node_parent_id = homepageId;
                }

                // set node index (if possible)
                if(node_id == page.ID) {
                    page_index = siblingIndex;
                }

                // check if we need to split the output
                if(node_id == splitSibling) {
                    splitSiblingIndex = siblingIndex;
                    stage = NavDocStage.ChildrenPre;
                    result.End().Start("children-pre");
                    if(splitChildren == 0) {
                        stage = NavDocStage.ChildrenPost;
                        result.End().Start("children-post");
                    }
                    continue;
                }
                if(node_id == splitChildren) {
                    splitChildrenIndex = childIndex;
                    stage = NavDocStage.ChildrenPost;
                    result.End().Start("children-post");
                    continue;
                }
                if(((stage == NavDocStage.ChildrenPre) || (stage == NavDocStage.ChildrenPost)) && (splitSibling > 0) && (node_parent_id != splitSibling)) {
                    if(stage == NavDocStage.ChildrenPre) {
                        result.End().Start("children-post");
                    }
                    stage = NavDocStage.SiblingPost;
                    result.End().Start("siblings-post");
                }

                // check if this node is part of the result set (only include ancestors, siblings, and children of selected node)
                bool ancestor = false;
                Title nodeTitle = Title.FromDbPath((NS)node.NameSpace, node.Title, node.DisplayName);
                if((node_id != page.ID /* selected */) && (node_parent_id != page_parent_id /* sibling */) && (node_parent_id != page.ID /* child */)) {
                    ancestor = (node_id == page_parent_id /* immediate parent */) || (node_id == homepageId) || nodeTitle.IsParentOf(page.Title);
                    if(!ancestor) {
                        continue;
                    }
                }

                // don't include siblings root user pages
                if(((page.Title.IsUser) || (page.Title.IsSpecial) || (page.Title.IsTemplate)) && (page_parent_id == homepageId) && (node_parent_id == homepageId) && (node_id != page.ID)) {
                    continue;
                }

                // 'div' element
                result.Start("div");

                // 'class' attribute
                css.Clear();
                css.Add("node");
                if(hidden) {
                    css.Add("closedNode");
                }

                // 'c' (children) attribute
                fetchedChildren.Clear();
                childrenNodes.Clear();
                uint parentId = (node_id == homepageId) ? 0 : node_id;
                for(int i = 0; i < list.Count; ++i) {
                    NavBE child = list[i];
                    if((child.ParentId == parentId) & (child.Id != homepageId)) {
                        Title childTitle = Title.FromDbPath((NS)child.NameSpace, child.Title, child.DisplayName);

                        // skip children if they are siblings of the top User: or Template: page
                        if(((page.Title.IsUser) || (page.Title.IsSpecial) || (page.Title.IsTemplate)) && (node_id == homepageId) && !childTitle.IsParentOf(page.Title)) {
                            continue;
                        }
                        childrenNodes.Add(child);
                        fetchedChildren.Add("n" + child.Id);
                    }
                }
                int totalChildrenCount = node.ChildCount ?? fetchedChildren.Count;

                // 'p' (parent) attribute
                string p = null;
                if(node_id != homepageId) {
                    p = "n" + node_parent_id;
                }

                // 'cd' (child-data) and 'sd' (sibling-data) attribute
                string cd = null;
                string sd;
                if(node_id == page.ID) {

                    // active node
                    if(node_id == homepageId) {
                        css.Add("dockedNode");
                        css.Add("homeNode");
                        css.Add("parentClosed");
                        css.Add("homeSelected");
                    } else {
                        css.Add("childNode");
                        if(!hidden) {
                            css.Add("sibling");
                        }
                        if(totalChildrenCount > 0) {
                            css.Add("parentOpen");
                        }
                    }
                    css.Add("selected");

                    // we have all the child data
                    cd = "1";
                    sd = "1";
                } else if(node_parent_id == page_parent_id) {

                    // sibling of active node
                    css.Add("childNode");
                    if(!hidden) {
                        css.Add("sibling");
                    }
                    if(totalChildrenCount > 0) {
                        css.Add("parentClosed");
                    }

                    // if no children exist, then we have all the child data
                    cd = ((totalChildrenCount > 0) ? "0" : "1");
                    sd = "1";
                } else if(node_parent_id == page.ID) {

                    // child of active node
                    css.Add("childNode");
                    if((node_parent_id == homepageId) && !hidden) {
                        css.Add("sibling");
                    }
                    if((page.ID != homepageId) && !hidden) {
                        css.Add("selectedChild");
                    }
                    if(totalChildrenCount > 0) {
                        css.Add("parentClosed");
                    }

                    // if no children exist, then we have all the child data
                    cd = ((totalChildrenCount > 0) ? "0" : "1");
                    sd = "1";
                } else if(ancestor) {

                    // ancestor of active node (parent and above)
                    css.Add("dockedNode");
                    if(node_id == homepageId) {
                        css.Add("homeNode");
                    }
                    if(node_id == page_parent_id) {
                        css.Add("lastDocked");
                    }
                    css.Add("parentClosed");

                    // check if we are the last docked node or have more than one child
                    if((node_id == page_parent_id) || (totalChildrenCount == 1)) {
                        cd = "1";
                    } else {

                        // find the child node that is actually included in the tree
                        foreach(NavBE child in childrenNodes) {
                            Title childTitle = Title.FromDbPath((NS)child.NameSpace, child.Title, child.DisplayName);
                            if(childTitle.IsParentOf(page.Title)) {
                                cd = "n" + child.Id;
                                break;
                            }
                        }
                        if(cd == null) {
#if DEBUG
                            throw new Exception("unexpected [expected to find child nodes]");
#else
                            cd = "1";
#endif
                        }
                    }

                    // check if parent of this node has more than one child
                    if((node_id == homepageId) || (result[string.Format("div[@id='{0}']/@cd", "n" + node_parent_id)].Contents == "1")) {
                        sd = "1";
                    } else {
                        sd = "0";
                    }
                } else {
                    throw new Exception("unexpected");
                }

                // attributes
                result.Attr("class", string.Join(" ", css.ToArray()));
                result.Attr("id", "n" + node_id.ToString());
                if(fetchedChildren.Count > 0) {
                    result.Attr("c", string.Join(",", fetchedChildren.ToArray()));
                }
                if(p != null) {
                    result.Attr("p", p);
                }

                // NOTE (steveb): this is used by the JS in the browser to correlate nodes in the pane (it's never used by anything else; hence the different format)
                string safe_path = nodeTitle.AsPrefixedDbPath().Replace("//", "%2f");
                result.Attr("path", (safe_path.Length == 0) ? string.Empty : (safe_path + "/"));

                // root page always has all children if they belong to User:, Template:, or Special: namespace
                if((cd != "1") && !virtual_node && ((page.Title.IsMain) || (((page.Title.IsTemplate) || (page.Title.IsUser) || (page.Title.IsSpecial)) && (node_id != homepageId)))) {
                    result.Attr("cd", cd);
                }

                // children of root page always have all siblings if they belong to User:, Template:, or Special: namespace
                if((sd != "1") && !virtual_node && ((page.Title.IsMain) || (((page.Title.IsTemplate) || (page.Title.IsUser) || (page.Title.IsSpecial)) && (node_parent_id != homepageId)))) {
                    result.Attr("sd", "0");
                }
                if(virtual_node || ((node_id == homepageId) && (!page.Title.IsMain))) {
                    result.Attr("reload", "1");
                }

                // div contents
                result.Start("a");

                // set page title
                string name = nodeTitle.AsUserFriendlyName();
                result.Attr("href", Utils.AsPublicUiUri(nodeTitle));
                result.Attr("title", name);
                result.Elem("span", DekiWikiService.ScreenFont.Truncate(name, max_width));
                result.End();
                result.End();
                if(node_parent_id == page_parent_id) {
                    ++siblingIndex;
                } else if(node_parent_id == page.ID) {
                    ++childIndex;
                }
            }

            // post-process created list
            if((splitSibling > 0) || (splitChildren > 0)) {
                if(stage == NavDocStage.SiblingPre) {
                    result.End().Start("siblings-post");
                    result.End().Start("children-pre");
                    result.End().Start("children-post");
                } else if(stage == NavDocStage.ChildrenPre) {
                    result.End().Start("children-post");
                    result.End().Start("siblings-post");
                } else if(stage == NavDocStage.ChildrenPost) {
                    result.End().Start("siblings-post");
                }
                result.End();

                // truncate siblings and children
                TruncateList(result["siblings-pre/div | siblings-post/div"], ~splitSiblingIndex, hidden);
                TruncateList(result["children-pre/div | children-post/div"], ~splitChildrenIndex, hidden);
            } else if(hidden) {

                // truncate full list
                TruncateList(result["div"], 0, hidden);
            } else {

                // truncate children of selected node
                TruncateList(result[string.Format("div[@p='n{0}']", page.ID)], 0, hidden);

                // truncate siblings of selected node
                TruncateList(result[string.Format("div[@p='n{0}']", page_parent_id)], page_index, hidden);
            }
            return result;
        }

        private static void TruncateList(XDoc list, int selectedIndex, bool hidden) {
            var resources = DekiContext.Current.Resources;
            int count = list.ListLength;

            // check if the selected node is a phantom node (i.e. it's not in the list, but must be accounted for)
            bool phantom = false;
            if(selectedIndex < 0) {
                phantom = true;
                ++count;
                selectedIndex = ~selectedIndex;
            }
            int max_list_count = DekiContext.Current.Instance.NavMaxItems;
            int firstVisible;
            int lastVisible;
            if(max_list_count <= 0) {
                firstVisible = 0;
                lastVisible = count - 1;
            } else {
                firstVisible = Math.Max(0, selectedIndex - max_list_count / 2);
                lastVisible = Math.Min(count - 1, selectedIndex + max_list_count / 2);
                firstVisible = Math.Max(0, Math.Min(firstVisible, lastVisible - max_list_count + 1));
                lastVisible = Math.Min(count - 1, firstVisible + max_list_count - 1);
            }
            if(firstVisible == 1) {
                firstVisible = 0;
            }
            if(lastVisible == (count - 2)) {
                lastVisible = count - 1;
            }

            // set nodes as hidden
            List<string> firstClosed = new List<string>();
            List<string> lastClosed = new List<string>();
            XDoc first = null;
            XDoc last = null;
            int counter = 0;
            foreach(XDoc current in list) {
                if(!phantom || (counter != selectedIndex)) {
                    if(counter < firstVisible) {
                        XDoc css = current["@class"];
                        if(first == null) {
                            first = current;
                        } else {
                            css.ReplaceValue(css.Contents + (hidden ? " hiddenNode" : " closedNode hiddenNode"));
                            firstClosed.Add(current["@id"].Contents);
                        }
                    } else if(counter > lastVisible) {
                        XDoc css = current["@class"];
                        if(last == null) {
                            last = current;
                        } else {
                            css.ReplaceValue(css.Contents + (hidden ? " hiddenNode" : " closedNode hiddenNode"));
                            lastClosed.Add(current["@id"].Contents);
                        }
                    }
                }
                ++counter;
            }

            // convert first and last node into '...' nodes
            if(first != null) {
                first["@class"].ReplaceValue(first["@class"].Contents + " moreNodes");
                XDoc address = first["a"];
                first.Attr("content", address.AsXmlNode.InnerText);
                first.Attr("contentTitle", address["@title"].Contents);
                first.Attr("hiddenNodes", string.Join(",", firstClosed.ToArray()));
                address.ReplaceValue(string.Empty);
                address["span"].Remove();
                address.Start("span").Attr("class", "more").Value("...").End();
                address["@title"].ReplaceValue(resources.Localize(DekiResources.MORE_DOT_DOT_DOT()));
            }
            if(last != null) {
                last["@class"].ReplaceValue(last["@class"].Contents + " moreNodes");
                XDoc address = last["a"];
                last.Attr("content", address.AsXmlNode.InnerText);
                last.Attr("contentTitle", address["@title"].Contents);
                last.Attr("hiddenNodes", string.Join(",", lastClosed.ToArray()));
                address.ReplaceValue(string.Empty);
                address["span"].Remove();
                address.Start("span").Attr("class", "more").Value("...").End();
                address["@title"].ReplaceValue(resources.Localize(DekiResources.MORE_DOT_DOT_DOT()));
            }
        }

        private static IList<NavBE> FilterDisallowedNavRecords(IList<NavBE> navPages, PageBE page) {
            Dictionary<ulong, NavBE> navLookup = new Dictionary<ulong, NavBE>();

            // NOTE (steveb): we collect the given page and its parent pages into a separate collection since these 
            //                will need to be shown regardless in the nav pane.

            List<NavBE> parents = new List<NavBE>();
            foreach(NavBE nav in navPages) {
                navLookup.Add(nav.Id, nav);
            }
            ulong homepageId = DekiContext.Current.Instance.HomePageId;
            NavBE current;
            ulong id = page.ID;
            do {
                if(!navLookup.TryGetValue(id, out current)) {
                    break;
                }
                parents.Add(current);
                navLookup.Remove(id);
                id = ((current.ParentId == 0) && current.Id != homepageId) ? homepageId : current.ParentId;
            } while(current.Id != homepageId);


            // filter non-parent pages from nav pane
            IList<NavBE> allowed = FilterDisallowedNavRecords(new List<NavBE>(navLookup.Values));

            // add parent page back
            allowed.AddRange(parents);
            return allowed;
        }

        private static IList<NavBE> FilterDisallowedNavRecords(IList<NavBE> navPages) {
            Dictionary<ulong, NavBE> pagesToCheck = new Dictionary<ulong, NavBE>();
            List<NavBE> allowedPages = new List<NavBE>(navPages.Count);
            Permissions userPermissions = PermissionsBL.GetUserPermissions(DekiContext.Current.User);
            foreach(NavBE np in navPages) {
                ulong effectivePageRights = PermissionsBL.CalculateEffectivePageRights(new PermissionStruct((ulong)userPermissions, np.RestrictionFlags ?? ulong.MaxValue, 0));
                if(!PermissionsBL.IsActionAllowed(effectivePageRights, false, Permissions.BROWSE)) {
                    pagesToCheck.Add(np.Id, np);
                } else {
                    allowedPages.Add(np);
                }
            }
            if(pagesToCheck.Count > 0) {
                IEnumerable<ulong> filteredOutPages;
                var allowedIds = PermissionsBL.FilterDisallowed(DekiContext.Current.User, pagesToCheck.Keys.ToArray(), false, out filteredOutPages, Permissions.BROWSE);
                foreach(var allowedId in allowedIds) {
                    allowedPages.Add(pagesToCheck[allowedId]);
                }
                return allowedPages;
            }

            // No changes made.. 
            return navPages;
        }

        private static IList<NavBE> SortNavRecords(IList<NavBE> navPages, CultureInfo culture) {
            List<NavBE> navPagesList = new List<NavBE>(navPages);
            navPagesList.Sort(delegate(NavBE left, NavBE right) {
                int compare = left.NameSpace - right.NameSpace;
                if(compare != 0) {
                    return compare;
                }
                return string.Compare(left.SortableTitle, right.SortableTitle, true, culture);
            });
            return navPagesList;
        }

        private static XDoc CreateExpandableTree(IList<NavBE> list, PageBE page) {
            List<ExpandableNavPage> tree = new List<ExpandableNavPage>();
            bool onlyChildren = list.Count(s => s.ParentId == 0 || s.ParentId == DekiContext.Current.Instance.HomePageId) == 0;
            foreach(NavBE node in list) {
                uint node_id = node.Id;
                ulong homepageId = DekiContext.Current.Instance.HomePageId;
                uint parentId = (node_id == homepageId) ? 0 : node_id;
                ulong node_parent_id = node.ParentId;

                // fix parent_id: it's stored as zero for home and children of home
                if((node_parent_id == 0) && (node_id != homepageId)) {
                    node_parent_id = homepageId;
                }

                // fix page_parent_id: it's stored as zero for home and children of home
                ulong page_parent_id = (ulong)page.ParentID;
                if((page_parent_id == 0) && (page.ID != homepageId)) {
                    page_parent_id = homepageId;
                }

                // don't include siblings root user pages
                if(((page.Title.IsUser) || (page.Title.IsSpecial) || (page.Title.IsTemplate)) && (page_parent_id == homepageId) && (node_parent_id == homepageId) && (node_id != page.ID)) {
                    continue;
                }

                var sNode = new ExpandableNavPage(node);
                BuildExpandableTree(list, sNode, page);

                if(onlyChildren || (node.ParentId == 0 || node.ParentId == DekiContext.Current.Instance.HomePageId)) {
                    tree.Add(sNode);
                }
            }
            tree[tree.Count - 1].LastParent = true;
            XDoc result = new XDoc("tree");
            return CreateExpandableTreeDoc(ref result, tree);
        }

        private static void BuildExpandableTree(IList<NavBE> list, ExpandableNavPage node, PageBE page) {
            uint node_id = node.NavPage.Id;
            ulong homepageId = DekiContext.Current.Instance.HomePageId;
            uint parentId = (node_id == homepageId) ? 0 : node_id;

            foreach(NavBE child in list) {
                if((child.ParentId == parentId) & (child.Id != homepageId)) {
                    Title childTitle = Title.FromDbPath((NS)child.NameSpace, child.Title, child.DisplayName);

                    // skip children if they are siblings of the top User: or Template: page
                    if(((page.Title.IsUser) || (page.Title.IsSpecial) || (page.Title.IsTemplate)) && (node_id == homepageId) && !childTitle.IsParentOf(page.Title)) {
                        continue;
                    }
                    var sChild = new ExpandableNavPage(child);
                    sChild.Parent = node;
                    node.Children.Add(sChild);

                    ///recursive.
                    BuildExpandableTree(list, sChild, page);
                }
            }
            if(node_id == page.ID) {
                node.IsSelected = true;
            }
            if(node_id == homepageId) {
                node.IsHomepage = true;
            }
        }

        private static XDoc CreateExpandableTreeDoc(ref XDoc result, List<ExpandableNavPage> tree) {
            foreach(ExpandableNavPage branch in tree) {
                CreateExpandableTreeChildrenDoc(ref result, branch);
            }
            return result;
        }

        private static void CreateExpandableTreeChildrenDoc(ref XDoc result, ExpandableNavPage branch) {
            uint nodeID = branch.NavPage.Id;

            // fix page_parent_id: it's stored as zero for home and children of home
            Title nodeTitle = Title.FromDbPath((NS)branch.NavPage.NameSpace, branch.NavPage.Title, branch.NavPage.DisplayName);
            string name = nodeTitle.AsUserFriendlyName();
            List<string> css = new List<string>();
            result.Start("ul");
            if(branch.Parent != null && branch.Parent.Children[branch.Parent.Children.Count - 1] == branch) {
                css.Add("lastNode");
            } else if(branch.LastParent) {
                css.Add("lastNode");
            }
            if(branch.IsHomepage) {
                css.Add("homepage");
            }
            result.Attr("class", String.Join(" ", css.ToArray()));
            css.Clear();
            result.Start("li");
            result.Attr("id", "n" + nodeID);
            bool isChild = false;
            css.Add("node");
            if(branch.IsAncestor) {
                css.Add("ancestor");
            }
            if(branch.IsSelected) {
                css.Add("selected");
            }
            if(branch.IsHomepage) {
                css.Add("homeNode");
            } else {
                if(branch.Children.Count > 0 || branch.NavPage.ChildCount > 0) {
                    css.Add(branch.IsAncestor || branch.IsSelected ? "parentOpen" : "parentClosed");
                } else {
                    css.Add("child");
                    isChild = true;
                }
            }
            result.Attr("class", String.Join(" ", css.ToArray()));

            ///Icon Spacer
            result.Start("div");
            result.Attr("class", "icon");
            if(!isChild) {
                result.Attr("onclick", "DekiExpandableNav.Toggle(this,event);");
            }

            ///Link.
            result.Start("a").Value(name);
            const string stopBubbelingScript = @"if (!event) var event = window.event; event.cancelBubble = true; if (event.stopPropagation) event.stopPropagation();";
            result.Attr("onclick", stopBubbelingScript);
            if(css.Contains("selected")) {
                result.Attr("class", "selected");
            } else if(css.Contains("ancestor")) {
                result.Attr("class", "ancestor");
            } else if(branch.IsHomepage) {
                result.Attr("class", "homepage");
            }
            result.Attr("title", name);
            result.Attr("href", Utils.AsPublicUiUri(nodeTitle));
            result.End();
            result.End();
            if(branch.Children.Count > 0 && branch.NavPage.Id != DekiContext.Current.Instance.HomePageId) {
                foreach(ExpandableNavPage cBranch in branch.Children) {
                    CreateExpandableTreeChildrenDoc(ref result, cBranch);
                }
            }
            result.End();
            result.End();
        }
    }
}
