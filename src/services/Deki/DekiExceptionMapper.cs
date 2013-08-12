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
using System.Reflection;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Util;
using MindTouch.Dream;

namespace MindTouch.Deki {

    // ReSharper disable SuggestBaseTypeForParameter
    public static class DekiExceptionMapper {

        //--- Class Fields ---
        private static readonly Dictionary<Type, MethodInfo> _handlers = new Dictionary<Type, MethodInfo>();

        //--- Class Methods ---

        // TODO: if we need more arguments, need to make the dispatcher smarter to pass in the desired arguments, so that the other handler
        // signatures don't have to change
        public static DreamMessage Map(Exception exception, DekiResources resources) {
            var exceptionType = exception.GetType();
            MethodInfo handler;
            lock(_handlers) {
                if(!_handlers.TryGetValue(exceptionType, out handler)) {
                    handler = (from method in typeof(DekiExceptionMapper).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                               let parameters = method.GetParameters()
                               where parameters.Length == 2 && parameters[0].ParameterType.IsAssignableFrom(exceptionType)
                               let depth = GetInheritanceChain(parameters[0].ParameterType, 0)
                               orderby depth descending
                               select method).FirstOrDefault();
                    if(handler == null) {
                        return null;
                    }
                    _handlers[exceptionType] = handler;
                }
            }
            return (DreamMessage)handler.Invoke(null, new object[] { exception, resources });
        }

        private static int GetInheritanceChain(Type type, int depth) {
            return type.BaseType == null ? depth : GetInheritanceChain(type.BaseType, ++depth);
        }

        //--- generic ResourcedMindTouchException handlers ---
        private static DreamMessage Map(MindTouchInvalidCallException e, DekiResources resources) {
            return DreamMessage.BadRequest(resources.Localize(e.Resource));
        }

        private static DreamMessage Map(MindTouchFatalCallException e, DekiResources resources) {
            return DreamMessage.InternalError(resources.Localize(e.Resource));
        }

        private static DreamMessage Map(MindTouchConflictException e, DekiResources resources) {
            return DreamMessage.Conflict(resources.Localize(e.Resource));
        }

        private static DreamMessage Map(MindTouchNotFoundException e, DekiResources resources) {
            return DreamMessage.NotFound(resources.Localize(e.Resource));
        }

        private static DreamMessage Map(MindTouchForbiddenException e, DekiResources resources) {
            return DreamMessage.Forbidden(resources.Localize(e.Resource));
        }

        private static DreamMessage Map(MindTouchAccessDeniedException e, DekiResources resources) {
            return DreamMessage.AccessDenied(e.AuthRealm, resources.Localize(e.Resource));
        }

        private static DreamMessage Map(MindTouchNotImplementedException e, DekiResources resources) {
            return DreamMessage.NotImplemented(resources.Localize(e.Resource));
        }

        private static DreamMessage Map(ExternalServiceResponseException e, DekiResources resources) {

            // Note (arnec): the attached resource is just silently dropped here... doesn't seem right.
            // also e.Resource in this case can be null, so beware.
            return e.ExternalServiceResponse;
        }

        //--- DekiDataException handlers ---
        private static DreamMessage Map(ResourceExpectedHeadException e, DekiResources resources) {
            return DreamMessage.Conflict(resources.Localize(DekiResources.RESOURCE_EXPECTED_HEAD_REVISION(e.HeadRevision, e.Revision)));
        }

        private static DreamMessage Map(ResourceRevisionOutOfRangeException e, DekiResources resources) {
            return DreamMessage.Conflict(resources.Localize(DekiResources.RESOURCE_REVISION_OUT_OF_RANGE(e.Resource)));
        }

        private static DreamMessage Map(ResourceConcurrencyException e, DekiResources resources) {
            return DreamMessage.Conflict(resources.Localize(DekiResources.PROPERTY_CONCURRENCY_ERROR(e.ResourceId)));
        }

        private static DreamMessage Map(PageConcurrencyException e, DekiResources resources) {
            return DreamMessage.Conflict(resources.Localize(DekiResources.PAGE_CONCURRENCY_ERROR(e.PageId)));
        }

        private static DreamMessage Map(CommentConcurrencyException e, DekiResources resources) {
            return DreamMessage.Conflict(resources.Localize(DekiResources.COMMENT_CONCURRENCY_ERROR(e.PageId)));
        }

        private static DreamMessage Map(OldIdNotFoundException e, DekiResources resources) {
            return DreamMessage.InternalError(resources.Localize(DekiResources.UNABLE_TO_FIND_OLD_PAGE_FOR_ID(e.OldId, e.TimeStamp)));
        }

        private static DreamMessage Map(PageIdNotFoundException e, DekiResources resources) {
            return DreamMessage.InternalError(resources.Localize(DekiResources.UNABLE_TO_RETRIEVE_PAGE_FOR_ID(e.PageId)));
        }

        private static DreamMessage Map(HomePageNotFoundException e, DekiResources resources) {
            return DreamMessage.InternalError(resources.Localize(DekiResources.UNABLE_TO_FIND_HOME_PAGE()));
        }

        private static DreamMessage Map(TooManyResultsException e, DekiResources resources) {
            return DreamMessage.Forbidden(resources.Localize(DekiResources.SITE_TOO_BIG_TO_GENERATE_SITEMAP()));
        }

        //--- Misc exception handlers ---
        private static DreamMessage Map(MindTouchInvalidOperationException e, DekiResources resources) {
            return DreamMessage.BadRequest(e.Message);
        }
        private static DreamMessage Map(DekiLicenseException e, DekiResources resources) {
            return DreamMessage.BadRequest(e.Message);
        }

        private static DreamMessage Map(AttachmentPreviewBadImageFatalException e, DekiResources resources) {
            return new DreamMessage(DreamStatus.InternalError, null, e.PreviewMimeType, e.PreviewImage);
        }
    }
    // ReSharper restore SuggestBaseTypeForParameter
}