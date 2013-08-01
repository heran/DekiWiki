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
using System.Text;

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {

    public class TagBL {
        private readonly IDekiDataSession _session;
        private readonly CultureInfo _culture;
        private readonly UserBE _user;
        private readonly IDekiChangeSink _eventSink;
        private readonly DekiContext _dekiContext;

        public TagBL() {
            _dekiContext = DekiContext.Current;
            _session = DbUtils.CurrentSession;
            _user = _dekiContext.User;
            _eventSink = _dekiContext.Instance.EventSink;
            _culture = DreamContext.Current.Culture;
        }

        public IList<TagBE> GetTagsForPage(PageBE page) {

            // retrieve tags associated with a specified page id
            var tags = _session.Tags_GetByPageId(page.ID);

            // populate the defined to and related pages information
            var ids = new List<uint>();
            foreach(var tag in tags) {
                if(TagType.DEFINE == tag.Type) {
                    tag.DefinedTo = page;
                }
                ids.Add(tag.Id);
            }

            var relatedPageMap = ids.Count > 0 ? _session.Tags_GetRelatedPageIds(ids) : new Dictionary<uint, IEnumerable<ulong>>();
            var pageMap = _session.Pages_GetByIds(relatedPageMap.SelectMany(x => x.Value).Distinct()).ToDictionary(x => x.ID);
            foreach(var tag in tags) {
                if((TagType.DEFINE != tag.Type) && (TagType.TEXT != tag.Type)) {
                    continue;
                }
                IEnumerable<ulong> relatedPageIds;
                relatedPageMap.TryGetValue(tag.Id, out relatedPageIds);
                if(null == relatedPageIds) {
                    continue;
                }
                var filtered = PermissionsBL.FilterDisallowed(_user, relatedPageIds, false, Permissions.BROWSE);
                var relatedPages = (from pageId in filtered
                                    where pageMap.ContainsKey(pageId)
                                    select pageMap[pageId]).ToArray();
                if(TagType.DEFINE == tag.Type) {
                    tag.RelatedPages = relatedPages;
                } else if((TagType.TEXT == tag.Type) && (0 < relatedPages.Length)) {
                    tag.DefinedTo = relatedPages[0];
                }
            }
            return tags;
        }

        public IList<TagBE> GetTags(string partialName, TagType type, DateTime from, DateTime to) {
            return FilterDisallowed(_session.Tags_GetByQuery(partialName, type, from, to));
        }

        public TagBE GetTagFromUrl(DreamContext context) {
            return GetTagFromPathSegment(context.GetParam("tagid"));
        }

        public TagBE GetTagFromPathSegment(string tagPathSegment) {
            TagBE tag = null;
            uint id = 0;

            string tagId = tagPathSegment;
            if(uint.TryParse(tagId, out id))
                tag = _session.Tags_GetById(id);
            else {
                if(tagId.StartsWith("=")) {
                    string prefixedName = tagId.Substring(1);
                    prefixedName = XUri.Decode(prefixedName);
                    TagBE tmpTag = ParseTag(prefixedName);
                    tag = _session.Tags_GetByNameAndType(tmpTag.Name, tmpTag.Type);
                }
            }
            if(tag != null) {
                IList<TagBE> tags = new List<TagBE>();
                tags.Add(tag);
                tags = FilterDisallowed(tags);
                if(tags.Count == 0)
                    tag = null;
            }
            if(tag == null) {
                throw new TagNotFoundException();
            }

            return tag;
        }

        public TagBE ParseTag(string prefixedName) {
            TagBE tag = null;

            // retrieve the tag type
            prefixedName = prefixedName.Trim();
            if(0 < prefixedName.Length) {
                tag = new TagBE();
                if(prefixedName.StartsWithInvariantIgnoreCase(TagPrefix.DEFINE)) {
                    tag.Type = TagType.DEFINE;
                } else if(prefixedName.StartsWithInvariantIgnoreCase(TagPrefix.DATE)) {
                    tag.Type = TagType.DATE;
                } else if(prefixedName.StartsWithInvariantIgnoreCase(TagPrefix.USER)) {
                    tag.Type = TagType.USER;
                }

                // remove the prefix from the tag
                int prefixLength = tag.Prefix.Length;
                if(prefixLength < prefixedName.Length) {
                    tag.Name = prefixedName.Substring(prefixLength);
                } else {
                    throw new TagInvalidArgumentException(prefixedName); 
                }

                // the tag is a date and not valid, change its type to text
                if(TagType.DATE == tag.Type) {
                    DateTime date;
                    if(!DateTime.TryParseExact(tag.Name, "yyyy-MM-dd", _culture, DateTimeStyles.None, out date)) {
                        tag.Type = TagType.TEXT;
                        tag.Name = prefixedName;
                    }
                }
            }

            return tag;
        }

        public void PutTagsFromXml(PageBE pageToUpdate, XDoc tagsDoc) {
            List<TagBE> newTags = ReadTagsListXml(tagsDoc);
            PutTags(pageToUpdate, newTags);
        }

        public void PutTags(PageBE page, string[] tags) {
            List<TagBE> newTags = new List<TagBE>();
            foreach(string tagName in tags) {
                TagBE tag = ParseTag(tagName);
                if(null != tag) {
                    newTags.Add(tag);
                }
            }
            if(newTags.Count == 0) {
                return;
            }
            PutTags(page, newTags);
        }

        private void PutTags(PageBE page, List<TagBE> newTags) {
            List<TagBE> existingTags = GetTagsForPage(page).ToList();

            // retrieve a diff of the existing and new tags
            List<TagBE> added;
            List<uint> removed;
            var diffSummary = CompareTagSets(existingTags, newTags, out added, out removed);

            // add and delete tags as determined from the diff
            added = InsertTags(page.ID, added);
            _session.TagMapping_Delete(page.ID, removed);
            if((0 < added.Count) || (0 < removed.Count)) {
                RecentChangeBL.AddTagsRecentChange(_dekiContext.Now, page, _user, diffSummary);
            }

            PageBL.Touch(page, _dekiContext.Now);
            _eventSink.PageTagsUpdate(_dekiContext.Now, page, _user);
        }

        public List<TagBE> InsertTags(ulong pageId, IList<TagBE> tags) {
            var result = new List<TagBE>();

            foreach(TagBE tag in tags) {
                uint tagId = _session.Tags_Insert(tag);

                // If the tag already exists, retrieve it
                if(tagId == 0) {
                    var matchingTag = _session.Tags_GetByNameAndType(tag.Name, tag.Type);

                    // Rename if name only differs by capitalization and/or accent marks (MySql collation ignores both)
                    if(!StringUtil.EqualsInvariant(matchingTag.Name, tag.Name)) {
                        matchingTag.Name = tag.Name;
                        UpdateTag(matchingTag);
                    }

                    // if the tag is a define tag, check that it's still a valid one
                    if(tag.Type == TagType.DEFINE && _session.Tags_ValidateDefineTagMapping(matchingTag)) {

                        // if the define tag is valid, insert this tag as text
                        tag.Type = TagType.TEXT;
                        result.AddRange(InsertTags(pageId, new[] { tag }));
                        continue;
                    }
                    tagId = matchingTag.Id;
                }
                _session.TagMapping_Insert(pageId, tagId);
                result.Add(tag);
            }
            return result;
        }

        private void UpdateTag(TagBE tag) {
            _session.Tags_Update(tag);
            
            // update tagged pages
            var taggedPages = GetTaggedPages(tag);
            foreach(var page in taggedPages) {
                PageBL.Touch(page, _dekiContext.Now);
                _eventSink.PageTagsUpdate(_dekiContext.Now, page, _user);
            }
        }

        private IList<TagBE> AssignDefinePages(IList<TagBE> tags) {
            foreach(TagBE tag in tags) {
                if(tag.Type == TagType.DEFINE) {
                    ulong pageId = _session.Tags_GetPageIds(tag.Id).FirstOrDefault();
                    if(0 != pageId) {
                        tag.DefinedTo = PageBL.GetPageById(pageId);
                    }
                }
            }
            return tags;
        }

        private IList<PageBE> GetTaggedPages(TagBE tag) {
            IList<ulong> pageIds = _session.Tags_GetPageIds(tag.Id);
            IList<PageBE> pages = PageBL.GetPagesByIdsPreserveOrder(pageIds);
            return PermissionsBL.FilterDisallowed(_user, pages, false, Permissions.BROWSE);
        }

        private IList<TagBE> FilterDisallowed(IList<TagBE> tags) {
            List<TagBE> tagsToRemove = new List<TagBE>();
            tags = AssignDefinePages(tags);
            foreach(TagBE tag in tags) {
                if(tag.Type == TagType.DEFINE) {
                    if(tag.DefinedTo == null || PermissionsBL.FilterDisallowed(_user, new PageBE[] { tag.DefinedTo }, false, Permissions.BROWSE).Length != 1) {
                        tagsToRemove.Add(tag);
                    }
                    tag.OccuranceCount = 1;
                } else {
                    // filter the tags based on permissions
                    IList<ulong> pageIds = _session.Tags_GetPageIds(tag.Id);
                    IList<PageBE> pages = PageBL.GetPagesByIdsPreserveOrder(pageIds);
                    if(pages == null) {
                        // this should never happen
                        tagsToRemove.Add(tag);
                    } else {
                        // apply permissions
                        int count = PermissionsBL.FilterDisallowed(_user, pages, false, Permissions.BROWSE).Length;
                        if(count == 0)
                            tagsToRemove.Add(tag);
                        tag.OccuranceCount = count;
                    }
                }
            }
            foreach(TagBE tag in tagsToRemove) {
                tags.Remove(tag);
            }
            return tags;
        }

        private DekiResourceBuilder CompareTagSets(List<TagBE> current, List<TagBE> proposed, out List<TagBE> added, out List<uint> removed) {

            // perform a subtraction of tags (in both directions) to determine which tags were added and removed from one set to another
            added = new List<TagBE>();
            removed = new List<uint>();
            TagComparer tagComparer = new TagComparer();
            current.Sort(tagComparer);
            proposed.Sort(tagComparer);

            // determine which pages have been added
            StringBuilder addedSummary = new StringBuilder();
            List<TagBE> addedList = new List<TagBE>();
            foreach(TagBE proposedTag in proposed) {
                if((current.BinarySearch(proposedTag, tagComparer) < 0)) {

                    // only add the tag if is not already being added 
                    bool includeTag = (added.BinarySearch(proposedTag, tagComparer) < 0);
                    if(includeTag && (TagType.TEXT == proposedTag.Type)) {
                        TagBE proposedTagDefine = new TagBE();
                        proposedTagDefine.Type = TagType.DEFINE;
                        proposedTagDefine.Name = proposedTag.Name;
                        if(0 <= proposed.BinarySearch(proposedTagDefine, tagComparer)) {
                            includeTag = false;
                        }
                    }
                    if(includeTag) {
                        added.Add(proposedTag);
                        if(1 < added.Count) {
                            addedSummary.Append(", ");
                        }
                        addedSummary.Append(proposedTag.PrefixedName);
                    }
                }
            }

            // determine which pages have been removed
            StringBuilder removedSummary = new StringBuilder();
            foreach(TagBE currentTag in current) {
                if(proposed.BinarySearch(currentTag, tagComparer) < 0) {
                    removed.Add(currentTag.Id);
                    if(1 < removed.Count) {
                        removedSummary.Append(", ");
                    }
                    removedSummary.Append(currentTag.PrefixedName);
                }
            }

            // create a diff summary string
            var diffSummary = new DekiResourceBuilder();
            if(0 < addedSummary.Length) {
                diffSummary.Append(DekiResources.TAG_ADDED(addedSummary.ToString()));
            }
            if(0 < removedSummary.Length) {
                if(!diffSummary.IsEmpty) {
                    diffSummary.Append(" ");
                }
                diffSummary.Append(DekiResources.TAG_REMOVED(removedSummary.ToString()));
            }
            return diffSummary;
        }

        private class TagComparer : IComparer<TagBE> {
            public int Compare(TagBE tag1, TagBE tag2) {

                // compares one tag to another
                int result = (int)tag1.Type - (int)tag2.Type;
                if(0 == result) {
                    result = tag1.Name.CompareInvariant(tag2.Name);
                }
                return result;
            }
        }

        #region XML Helpers

        private List<TagBE> ReadTagsListXml(XDoc tagsDoc) {
            List<TagBE> result = new List<TagBE>();
            foreach(XDoc tagDoc in tagsDoc["tag/@value"]) {
                TagBE tag = ParseTag(tagDoc.Contents);
                if(null != tag) {
                    result.Add(tag);
                }
            }
            return result;
        }

        public XDoc GetTagXml(TagBE tag, bool showPages, string pageFilterLanguage) {
            string uriText = null, titleText = null;
            switch(tag.Type) {
            case TagType.DEFINE:
                uriText = Utils.AsPublicUiUri(tag.DefinedTo.Title);
                titleText = tag.Name;
                break;
            case TagType.DATE:
                uriText = _dekiContext.UiUri.Uri.AtPath("Special:Events").With("from", tag.Name).ToString();
                DateTime date = DateTime.ParseExact(tag.Name, "yyyy-MM-dd", _culture, DateTimeStyles.None);
                titleText = date.ToString("D", _culture);
                break;
            case TagType.TEXT:
                if(null != tag.DefinedTo) {
                    uriText = Utils.AsPublicUiUri(tag.DefinedTo.Title);
                } else {
                    uriText = _dekiContext.UiUri.AtPath("Special:Tags").With("tag", tag.Name).ToString();
                }
                titleText = tag.Name;
                break;
            }
            XDoc result = new XDoc("tag");
            result.Attr("value", tag.PrefixedName);
            result.Attr("id", tag.Id);
            result.Attr("href", _dekiContext.ApiUri.At("site", "tags", tag.Id.ToString()).ToString());
            if(tag.OccuranceCount > 0)
                result.Attr("count", tag.OccuranceCount);

            result.Elem("type", tag.Type.ToString().ToLowerInvariant());
            result.Elem("uri", uriText);
            result.Elem("title", titleText);
            if(null != tag.RelatedPages) {
                result.Add(PageBL.GetPageListXml(tag.RelatedPages, "related"));
            }

            if(showPages) {
                IList<PageBE> pages = GetTaggedPages(tag);
                if(pageFilterLanguage != null) {

                    // filter pages by language
                    pages = (from page in pages where page.Language == pageFilterLanguage select page).ToList();
                }
                result.Add(PageBL.GetPageListXml(pages, "pages"));
            }
            return result;
        }

        public XDoc GetTagListXml(IEnumerable<TagBE> listItems, string listName, XUri href, bool showPages) {
            XDoc ret = null;
            if(!string.IsNullOrEmpty(listName))
                ret = new XDoc(listName);
            else
                ret = new XDoc("list");

            int count = 0;
            if(listItems != null)
                count = new List<TagBE>(listItems).Count;

            ret.Attr("count", count);

            if(href != null) {
                ret.Attr("href", href);
            }

            if(listItems != null) {
                foreach(TagBE tag in listItems)
                    ret.Add(GetTagXml(tag, showPages, null));
            }
            return ret;
        }
        #endregion
    }
}
