/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
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
using MindTouch.Xml;

namespace MindTouch.Deki.Profiler {
    internal static class DekiPageProfiler {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        internal static void Log(TimeSpan elapsed, PageBE basePage, DekiContext context, IDekiDataSession session) {
            _log.WarnFormat("slow page render for: {0}\n{1}", basePage.Title.AsPrefixedDbPath().IfNullOrEmpty("_homepage_"), CreateProfilerDoc(elapsed, basePage, context, session).ToPrettyString());
        }

        internal static XDoc CreateProfilerDoc(TimeSpan elapsed, PageBE basePage, DekiContext context, IDekiDataSession session) {
            var profiler = new XDoc("profiler")
                .Attr("elapsed", elapsed.TotalSeconds.ToString("###,0.00##"))
                .Attr("id", context.Instance.Id)
                .Attr("path", basePage.Title.AsPrefixedDbPath());
            var history = DekiXmlParser.GetParseState().ProfilerHistory;

            // show profiling for rendered pages
            profiler.Start("rendered-content");
            foreach(var pageHistory in history) {
                profiler.Start("page")
                    .Attr("wikiid", pageHistory.PageId)
                    .Attr("path", pageHistory.PagePath)
                    .Attr("elapsed", pageHistory.Elapsed.TotalSeconds.ToString("###,0.00##"))
                    .Attr("mode", pageHistory.Mode.ToString().ToLowerInvariant());
                foreach(var functionHistory in
                    from function in pageHistory.Functions
                    group function by function.Location
                        into functionByLocation
                        select new {
                            Function = functionByLocation.First(),
                            Count = functionByLocation.Count(),
                            TotalSeconds = functionByLocation.Sum(f => f.Elapsed.TotalSeconds)
                        }
                ) {
                    profiler.Start("function")
                        .Attr("name", functionHistory.Function.FunctionName)
                        .Attr("elapsed", functionHistory.TotalSeconds.ToString("###,0.00##"))
                        .Attr("count", functionHistory.Count)
                        .Attr("location", functionHistory.Function.Location)
                    .End();
                }
                profiler.End();
            }
            profiler.End();

            // show summary for function profiling
            profiler.Start("functions-summary");
            foreach(var functionHistory in
                from pageHistory in history
                from function in pageHistory.Functions
                group function by function.FunctionName
                    into functionByName
                    let totalSeconds = functionByName.Sum(f => f.Elapsed.TotalSeconds)
                    orderby totalSeconds descending
                    select new {
                        Function = functionByName.First(),
                        Count = functionByName.Count(),
                        TotalSeconds = totalSeconds,
                        MaxSeconds = functionByName.Max(f => f.Elapsed.TotalSeconds)
                    }
            ) {
                profiler.Start("function")
                    .Attr("name", functionHistory.Function.FunctionName)
                    .Attr("elapsed", functionHistory.TotalSeconds.ToString("###,0.00##"))
                    .Attr("average", (functionHistory.TotalSeconds / functionHistory.Count).ToString("###,0.00##"))
                    .Attr("max", functionHistory.MaxSeconds.ToString("###,0.00##"))
                    .Attr("count", functionHistory.Count)
                .End();
            }
            profiler.End();

            // show summary for page profiling
            profiler.Start("pages-summary");
            foreach(var pageHistory in
                from page in history
                group page by page.PageId.ToString() + page.Mode
                    into pageByIdAndMode
                    let totalSeconds = pageByIdAndMode.Sum(p => p.Elapsed.TotalSeconds)
                    orderby totalSeconds descending
                    select new {
                        Page = pageByIdAndMode.First(),
                        Count = pageByIdAndMode.Count(),
                        TotalSeconds = totalSeconds,
                        MaxSeconds = pageByIdAndMode.Max(p => p.Elapsed.TotalSeconds)
                    }
            ) {
                profiler.Start("page")
                    .Attr("id", pageHistory.Page.PageId)
                    .Attr("path", pageHistory.Page.PagePath)
                    .Attr("elapsed", pageHistory.TotalSeconds.ToString("###,0.00##"))
                    .Attr("average", (pageHistory.TotalSeconds / pageHistory.Count).ToString("###,0.00##"))
                    .Attr("max", pageHistory.MaxSeconds.ToString("###,0.00##"))
                    .Attr("count", pageHistory.Count)
                    .Attr("mode", pageHistory.Page.Mode.ToString().ToLowerInvariant())
                .End();
            }
            profiler.End();

            // show summary for query operations
            var dbprofiler = GetSessionProfiler(session);
            if(dbprofiler != null) {
                var dbhistory = dbprofiler.History;
                profiler.Start("db-summary")
                    .Attr("elapsed", dbhistory.Sum(p => p.Elapsed.TotalSeconds).ToString("###,0.00##"))
                    .Attr("count", dbhistory.Count);
                foreach(var dbsummary in
                    from dbentry in dbhistory
                    group dbentry by dbentry.Function
                        into dbentryByName
                        let totalSeconds = dbentryByName.Sum(f => f.Elapsed.TotalSeconds)
                        orderby totalSeconds descending
                        select new {
                            Query = dbentryByName.First(),
                            Count = dbentryByName.Count(),
                            TotalSeconds = totalSeconds,
                            MaxSeconds = dbentryByName.Max(f => f.Elapsed.TotalSeconds)
                        }
                ) {
                    profiler.Start("query")
                        .Attr("name", dbsummary.Query.Function)
                        .Attr("elapsed", dbsummary.TotalSeconds.ToString("###,0.00##"))
                        .Attr("average", (dbsummary.TotalSeconds / dbsummary.Count).ToString("###,0.00##"))
                        .Attr("max", dbsummary.MaxSeconds.ToString("###,0.00##"))
                        .Attr("count", dbsummary.Count)
                    .End();
                }
                profiler.End();
            }

            // add data stats
            Dictionary<string, string> stats;
            var sessionStats = session as IDekiDataStats;
            if(sessionStats != null) {
                profiler.Start("data-stats");
                stats = sessionStats.GetStats();
                if(stats != null) {
                    foreach(var pair in stats) {
                        profiler.Start("entry").Attr("name", pair.Key).Attr("value", pair.Value).End();
                    }
                }
                profiler.End();
            }

            // add misc. statistics
            stats = DekiContext.Current.Stats;
            if(stats.Count > 0) {
                profiler.Start("misc-stats");
                foreach(var pair in stats) {
                    profiler.Start("entry").Attr("name", pair.Key).Attr("value", pair.Value).End();
                }
                profiler.End();
            }
            return profiler;
        }

        private static DekiDataSessionProfiler GetSessionProfiler(IDekiDataSession session) {
            while(!(session is DekiDataSessionProfiler)) {
                session = session.Next;
                if(session == null) {
                    return null;
                }
            }
            return (DekiDataSessionProfiler)session;
        }
    }
}
