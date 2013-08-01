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

using System.Collections.Generic;
using System.IO;

using MindTouch.Deki.Import;
using MindTouch.Dream;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Import Service", "Copyright (c) 2006-2010 MindTouch Inc.",
        SID = new string[] { "sid://mindtouch.com/2009/07/package" }
    )]
    public class PackageService : DreamService {
        private Plug _dekiApi;

        //--- Features ---
        [DreamFeature("POST:import", "Provide a Package for import")]
        [DreamFeatureParam("uri", "string?", "Uri to retrieve package from (to be used instead of including package in the request body)")]
        [DreamFeatureParam("reltopath", "string?", "Path to prepend for relative Uri's in package (default: / )")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        public Yield PostImport(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string uri = context.GetParam("uri", null);
            string reltopath = context.GetParam("reltopatch", "/");
            DreamMessage packageMessage = request;
            if(!string.IsNullOrEmpty(uri)) {
                Result<DreamMessage> packageResult;
                yield return packageResult = Plug.New(uri).InvokeEx("GET", DreamMessage.Ok(), new Result<DreamMessage>());
                packageMessage = packageResult.Value;
                if(!packageMessage.IsSuccessful) {
                    throw new DreamAbortException(DreamMessage.BadRequest(string.Format("Unable to retrieve package from Uri '{0}': {1}", uri, packageMessage.Status)));
                }
            }
            string tempFile = Path.GetTempFileName();
            Stream tempStream = File.Create(tempFile);
            Result<long> copyResult;

            // TODO (steveb): use WithCleanup() to dispose of resources in case of failure
            yield return copyResult = packageMessage.ToStream().CopyTo(tempStream, packageMessage.ContentLength, new Result<long>()).Catch();
            tempStream.Dispose();
            if(copyResult.HasException) {
                response.Throw(copyResult.Exception);
                yield break;
            }
            ArchivePackageReader archivePackageReader = new ArchivePackageReader(File.OpenRead(tempFile));
            Result<ImportManager> importerResult;
            Plug authorizedDekiApi = _dekiApi.WithHeaders(request.Headers);

            // TODO (steveb): use WithCleanup() to dispose of resources in case of failure
            yield return importerResult = ImportManager.CreateAsync(authorizedDekiApi, reltopath, archivePackageReader, new Result<ImportManager>()).Catch();
            if(importerResult.HasException) {
                archivePackageReader.Dispose();
                File.Delete(tempFile);
                response.Throw(importerResult.Exception);
                yield break;
            }
            ImportManager importManager = importerResult.Value;
            Result importResult;
            yield return importResult = importManager.ImportAsync(new Result()).Catch();
            archivePackageReader.Dispose();
            File.Delete(tempFile);
            if(importResult.HasException) {
                response.Throw(importResult.Exception);
                yield break;
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _dekiApi = Plug.New(config["uri.deki"].AsUri);
            result.Return();
        }

    }
}
