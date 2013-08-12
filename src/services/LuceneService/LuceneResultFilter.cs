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
using MindTouch.Dream;
using MindTouch.Tasking;

namespace MindTouch.LuceneService {
    using Yield = IEnumerator<IYield>;

    public class LuceneResultFilter {

        //--- Class Methods ---
        public static Result<IList<LuceneResult>> Filter(Plug authPlug, IEnumerable<LuceneResult> items, int offset, int limit, Result<IList<LuceneResult>> result) {
            var builder = new LuceneResultFilter(authPlug, 10000, 100);
            return Coroutine.Invoke(builder.Filter, items, offset, limit, result);
        }

        //--- Fields ---
        private readonly Plug _authPlug;
        private readonly int _maxAuthItems;
        private readonly FilterCache _filterCache = new FilterCache();
        private readonly int _minAuthItems;
        private List<LuceneResult> _resultSet;

        //--- Constructors ---
        public LuceneResultFilter(Plug authPlug, int maxAuthItems, int minAuthItems) {
            if(authPlug == null) {
                throw new ArgumentNullException("authPlug");
            }
            _authPlug = authPlug;
            _maxAuthItems = maxAuthItems;
            _minAuthItems = minAuthItems;
        }

        //--- Methods ---

        /// <summary>
        /// Note: In general, the static <see cref="Filter(MindTouch.Dream.Plug,System.Collections.Generic.IEnumerable{MindTouch.LuceneService.LuceneResult},int,int,MindTouch.Tasking.Result{System.Collections.Generic.IList{MindTouch.LuceneService.LuceneResult}})"/> should be used instead of the instance method.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public Yield Filter(IEnumerable<LuceneResult> items, int offset, int limit, Result<IList<LuceneResult>> result) {
            _resultSet = new List<LuceneResult>();
            var candidates = new List<LuceneResult>();
            var maxResults = offset.SafeAdd(limit);

            // we have to iterate and check items that are inside our offset, since filtering might remove them, affecting the set size
            foreach(var item in items) {
                candidates.Add(item);

                // if the candidates list is at greater than _minAuthItems and our result set plus candidates is greater than our
                // max desired set size plus _minAuthItems
                // or we have at least _maxAuthItems candidates,
                // filter and merge the candidates into the result set
                if((candidates.Count >= _minAuthItems && (_resultSet.Count + candidates.Count) >= maxResults.SafeAdd(_minAuthItems))
                   || candidates.Count >= _maxAuthItems
                ) {
                    yield return Coroutine.Invoke(FilterAndMergeCandidates, candidates, new Result());
                    candidates.Clear();
                    if(_resultSet.Count >= maxResults) {
                        break;
                    }
                }
            }
            if(candidates.Any()) {
                yield return Coroutine.Invoke(FilterAndMergeCandidates, candidates, new Result());
            }
            if(offset > 0 || limit != int.MaxValue) {
                result.Return(_resultSet.Skip(offset).Take(limit).ToList());
            } else {
                result.Return(_resultSet);
            }
        }

        private Yield FilterAndMergeCandidates(List<LuceneResult> candidates, Result result) {
            var checkIds = _filterCache.NeedsFilterCheck(candidates.Select(x => x.PageId)).ToCommaDelimitedString();
            if(checkIds != null) {
                IEnumerable<ulong> filteredIds = null;
                yield return _authPlug
                    .Post(DreamMessage.Ok(MimeType.TEXT, checkIds), new Result<DreamMessage>())
                    .Set(m => filteredIds = m.ToText().CommaDelimitedToULong());
                _filterCache.MarkAsFiltered(filteredIds);
            }
            _resultSet.AddRange(candidates.Where(x => !_filterCache.IsFiltered(x.PageId)));
            result.Return();
            yield break;
        }
    }
}
