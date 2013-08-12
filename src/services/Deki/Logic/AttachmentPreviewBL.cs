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
using System.IO;
using System.Diagnostics;
using log4net;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Tasking;

namespace MindTouch.Deki.Logic {
    public static class AttachmentPreviewBL {

        //--- Constants ---
        private const string BADPREVIEWFILENAME = "badpreview.png";
        private const string HEADERSTAT_CONVERT = "imconvert-ms";
        private const string HEADERSTAT_IDENTIFY = "imidentify-ms";

        //--- Class Fields ---
        private static byte[] _badPreviewImageContent;
        private static readonly ILog _log = DekiLogManager.CreateLog();

        //--- Class Methods ---
        public static StreamInfo RetrievePreview(ResourceBE attachment, uint height, uint width, RatioType ratio, SizeType size, FormatType format) {
            string filename;
            return RetrievePreview(attachment, height, width, ratio, size, format, out filename);
        }

        public static StreamInfo RetrievePreview(ResourceBE attachment, uint height, uint width, RatioType ratio, SizeType size, FormatType format, out string filename) {
            if (!AttachmentBL.Instance.IsAllowedForImageMagickPreview(attachment)) {
                throw new AttachmentPreviewFailedWithMimeTypeNotImplementedException(attachment.MimeType);
            }
            if(format != FormatType.UNDEFINED && size != SizeType.UNDEFINED && size != SizeType.ORIGINAL && size != SizeType.CUSTOM) {
                throw new AttachmentPreviewFormatConversionWithSizeNotImplementedException();
            }

            // check that attachment has a width and height defined
            AttachmentBL.Instance.IdentifyUnknownImages(new ResourceBE[] { attachment });

            #region check validity of size, width, and height parameters

            // NOTE (steveb): following table describes the possible state-transitions; note that the resulting graph must acyclic to avoid infinite loops.
            //      BESTFIT
            //          -> THUMB
            //          -> WEBVIEW
            //          -> ORIGINAL
            //      UNDEFINED
            //          -> CUSTOM
            //          -> ORIGINAL
            //      THUMB
            //          -> ORIGINAL
            //      WEBVIEW
            //          -> ORIGINAL
            //      CUSTOM
            //          -> ORIGINAL
            //      ORIGINAL
        again:
            switch(size) {
            case SizeType.BESTFIT:
                if(width == 0 && height == 0) {

                    // no dimensions specified, use original
                    size = SizeType.ORIGINAL;
                    goto again;
                }
                if(width <= DekiContext.Current.Instance.ImageThumbPixels && height <= DekiContext.Current.Instance.ImageThumbPixels) {

                    // thumbnail is big enough
                    size = SizeType.THUMB;
                    goto again;
                } else if(width <= DekiContext.Current.Instance.ImageWebviewPixels && height <= DekiContext.Current.Instance.ImageWebviewPixels) {

                    // webview is big enough
                    size = SizeType.WEBVIEW;
                    goto again;
                } else {

                    // use original
                    size = SizeType.ORIGINAL;
                    goto again;
                }
            case SizeType.CUSTOM:
                if(height == 0 && width == 0) {

                    // no dimensions specified, use original
                    size = SizeType.ORIGINAL;
                    goto again;
                }
                if((attachment.MetaXml.ImageWidth <= width && attachment.MetaXml.ImageHeight <= height) && (attachment.MetaXml.ImageWidth >= 0 && attachment.MetaXml.ImageHeight >= 0)) {

                    // requested dimensions are larger than original, use original (we don't scale up!)
                    size = SizeType.ORIGINAL;
                    goto again;
                }
                break;
            case SizeType.ORIGINAL:
                width = 0;
                height = 0;
                break;
            case SizeType.UNDEFINED:
                if(height != 0 || width != 0) {
                    size = SizeType.CUSTOM;
                    goto again;
                } else {
                    size = SizeType.ORIGINAL;
                    goto again;
                }
            case SizeType.THUMB:
                width = DekiContext.Current.Instance.ImageThumbPixels;
                height = DekiContext.Current.Instance.ImageThumbPixels;
                if((attachment.MetaXml.ImageWidth <= width && attachment.MetaXml.ImageHeight <= height) && (attachment.MetaXml.ImageWidth >= 0 && attachment.MetaXml.ImageHeight >= 0)) {
                    size = SizeType.ORIGINAL;
                    goto again;
                }
                break;
            case SizeType.WEBVIEW:
                width = DekiContext.Current.Instance.ImageWebviewPixels;
                height = DekiContext.Current.Instance.ImageWebviewPixels;
                if((attachment.MetaXml.ImageWidth <= width && attachment.MetaXml.ImageHeight <= height) && (attachment.MetaXml.ImageWidth >= 0 && attachment.MetaXml.ImageHeight >= 0)) {
                    size = SizeType.ORIGINAL;
                    goto again;
                }
                break;
            }
            #endregion


            // Asking to convert to the same format as original
            if(format != FormatType.UNDEFINED && format == ResolvePreviewFormat(attachment.MimeType)) {
                format = FormatType.UNDEFINED;
            }

            //Determine if the result is capable of being cached
            bool cachable = (
                format == FormatType.UNDEFINED       //No format conversion
                && ratio != RatioType.VARIABLE       //must be a fixed aspect ratio or not provided
                && (size == SizeType.THUMB || size == SizeType.WEBVIEW)  //must be one of the known cached thumb sizes.
            );

            // load image
            StreamInfo result = null;
            try {
                if(size == SizeType.ORIGINAL && format == FormatType.UNDEFINED) {
                    result = DekiContext.Current.Instance.Storage.GetFile(attachment, SizeType.ORIGINAL, false);
                } else {
                    bool cached = false;

                    //The type of image is based on uploaded file's mimetype
                    if(format == FormatType.UNDEFINED) {
                        format = ResolvePreviewFormat(attachment.MimeType);
                    }

                    // check if image can be taken from the cache
                    if(cachable) {
                        result = GetCachedThumb(attachment, size);
                        cached = (result != null);
                    }

                    // load image if we haven't yet
                    if(result == null) {
                        result = DekiContext.Current.Instance.Storage.GetFile(attachment, SizeType.ORIGINAL, false);
                        if(result != null) {
                            result = BuildThumb(attachment, result, format, ratio, width, height);
                        }
                    }

                    // store result if possible and needed
                    if(cachable && !cached && (result != null)) {
                        SaveCachedThumb(attachment, size, result);
                        result = GetCachedThumb(attachment, size);
                    }
                }
                if(result == null) {
                    ThrowOnBadPreviewImage();
                }

                // full filename for response content-disposition header
                switch(size) {
                case SizeType.CUSTOM:
                    filename = string.Format("{0}_({1}x{2}){3}", attachment.Name, width == 0 ? "" : width.ToString(), height == 0 ? "" : height.ToString(), attachment.FilenameExtension == string.Empty ? string.Empty : "." + attachment.FilenameExtension);
                    break;
                case SizeType.ORIGINAL:
                    filename = attachment.Name;
                    break;
                case SizeType.BESTFIT:
                case SizeType.THUMB:
                case SizeType.UNDEFINED:
                case SizeType.WEBVIEW:
                default:
                    filename = string.Format("{0}_({1}){2}", attachment.Name, size.ToString().ToLowerInvariant(), attachment.FilenameExtension == string.Empty ? string.Empty : "." + attachment.FilenameExtension);
                    break;
                }
                if(format != FormatType.UNDEFINED) {
                    filename = Path.ChangeExtension(filename, format.ToString().ToLowerInvariant());
                }
                return result;
            } catch {
                if(result != null) {
                    result.Close();
                }
                throw;
            }
        }

        public static void PreSaveAllPreviews(ResourceBE attachment) {
            if (AttachmentBL.Instance.IsAllowedForImageMagickPreview(attachment)) {

                //The type of preview based on uploaded mimetype
                FormatType previewFormat = ResolvePreviewFormat(attachment.MimeType);

                // generate thumbnail
                StreamInfo file = DekiContext.Current.Instance.Storage.GetFile(attachment, SizeType.ORIGINAL, false);
                if(file != null) {
                    SaveCachedThumb(attachment, SizeType.THUMB, BuildThumb(attachment, file, previewFormat, RatioType.UNDEFINED, DekiContext.Current.Instance.ImageThumbPixels, DekiContext.Current.Instance.ImageThumbPixels));
                }

                // generate webview
                file = DekiContext.Current.Instance.Storage.GetFile(attachment, SizeType.ORIGINAL, false);
                if(file != null) {
                    SaveCachedThumb(attachment, SizeType.WEBVIEW, BuildThumb(attachment, file, previewFormat, RatioType.UNDEFINED, DekiContext.Current.Instance.ImageWebviewPixels, DekiContext.Current.Instance.ImageWebviewPixels));
                }
            }
        }

        private static StreamInfo GetCachedThumb(ResourceBE attachment, SizeType size) {
            StreamInfo result = null;
            if(size == SizeType.THUMB || size == SizeType.WEBVIEW) {
                result = DekiContext.Current.Instance.Storage.GetFile(attachment, size, false);
            }                 
            return result;
        }

        private static void SaveCachedThumb(ResourceBE attachment, SizeType size, StreamInfo file) {
            if(file != null) {
                DekiContext.Current.Instance.Storage.PutFile(attachment, size, file);
            }
        }

        private static StreamInfo BuildThumb(ResourceBE attachment, StreamInfo file, FormatType format, RatioType ratio, uint width, uint height) {
            if (!AttachmentBL.Instance.IsAllowedForImageMagickPreview(attachment)) {
                return file;
            }             
            return BuildThumb(file, format, ratio, width, height);
        }

        public static StreamInfo BuildThumb(StreamInfo file, FormatType format, RatioType ratio, uint width, uint height) {
            using(file) {
                
                //The mimetype of the thumb is based on the formattype. The 
                MimeType mime = ResolvePreviewMime(ref format);                

                //Some basic DoS protection.
                if(!IsPreviewSizeAllowed(width, height)) {
                    throw new Exceptions.ImagePreviewOversizedInvalidArgumentException();
                }
                string thumbnailArgs = string.Empty;
                if(width > 0 || height > 0) {
                    thumbnailArgs = string.Format("-colorspace RGB -thumbnail {0}{1}{2}{3}", width == 0 ? "" : width.ToString(), height > 0 ? "x" : "", height == 0 ? string.Empty : height.ToString(), ratio == RatioType.FIXED || ratio == RatioType.UNDEFINED ? "" : "!");
                }

                // NOTE (steveb): the '-[0]' option means that we only want to convert the first frame if there are multiple frames
                string args = string.Format("{0} -[0] {1}-", thumbnailArgs, (format == FormatType.UNDEFINED) ? string.Empty : format.ToString() + ":");

                Stopwatch sw = Stopwatch.StartNew();

                // run ImageMagick application
                Tuplet<int, Stream, Stream> exitValues = Async.ExecuteProcess(DekiContext.Current.Deki.ImageMagickConvertPath, args, file.Stream, new Result<Tuplet<int, Stream, Stream>>(TimeSpan.FromMilliseconds(DekiContext.Current.Deki.ImageMagickTimeout))).Wait();

                // record stats about this imagemagick execution
                sw.Stop();
                AddToStats(HEADERSTAT_CONVERT, sw.ElapsedMilliseconds);
                int status = exitValues.Item1;
                Stream outputStream = exitValues.Item2;
                Stream errorStream = exitValues.Item3;

                if(outputStream.Length == 0) {
                    using(StreamReader error = new StreamReader(errorStream)) {
                        _log.WarnMethodCall("Imagemagick convert failed", args, status, error.ReadToEnd());
                    }
                    return null;
                }
                _log.InfoFormat("Imagemagick convert finished in {0}ms. Args:{1}", sw.ElapsedMilliseconds, args);
                return new StreamInfo(outputStream, outputStream.Length, mime);
            }
        }

        /// <summary>
        /// Given the mimetype of an original file, return the mimetype of the preview images
        /// </summary>
        /// <param name="mime"></param>
        /// <returns></returns>
        public static MimeType ResolvePreviewMime(MimeType mime) {

            FormatType f = ResolvePreviewFormat(mime);
            MimeType m = ResolvePreviewMime(ref f);
            return m;
        }

        /// <summary>
        /// Given a formattype, return the associated mimetype. Used for user-defined conversions
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        private static MimeType ResolvePreviewMime(ref FormatType format) {
            MimeType mime;
            switch (format) {
            case FormatType.BMP:
                mime = MimeType.BMP;
                break;
            case FormatType.GIF:
                mime = MimeType.GIF;
                break;
            case FormatType.JPG:
                mime = MimeType.JPEG;
                break;
            case FormatType.PNG:
                mime = MimeType.PNG;
                break;
            default:
                mime = MimeType.JPEG;
                format = FormatType.JPG;
                break;
            }

            return mime;
        }

        /// <summary>
        /// Given the mimetype of an original file, return the formattype for the preview images to be used by imagemagick
        /// </summary>
        /// <param name="mime"></param>
        /// <returns></returns>
        private static FormatType ResolvePreviewFormat(MimeType mime) {
            if(mime.Match(MimeType.JPEG) || mime.Match(new MimeType("image/x-jpeg"))) {
                return FormatType.JPG;
            }

            if(mime.Match(MimeType.PNG) || mime.Match(new MimeType("image/x-png"))) {
                return FormatType.PNG;
            }

            if(mime.Match(MimeType.BMP) || mime.Match(new MimeType("image/x-bmp"))) {
                return FormatType.BMP;
            }

            if(mime.Match(MimeType.GIF) || mime.Match(new MimeType("image/x-gif"))) {
                return FormatType.GIF;
            }

            return FormatType.JPG;
        }

        /// <summary>
        /// Retrieves image dimensions.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>True if image too large to be identified or identifying succeeded. False if identification attempted but failed</returns>
        public static bool RetrieveImageDimensions(StreamInfo file, out int width, out int height, out int frames) {
            width = 0;
            height = 0;
            frames = 0;
            if(file == null) {
                return false;
            }
            using(file) {

                // prevent manipulation of large images
                if (DekiContext.Current.Instance.MaxImageSize < (ulong) file.Length) {
                    return true;
                }
                Stopwatch sw = Stopwatch.StartNew();

                // execute imagemagick-identify
                Tuplet<int, Stream, Stream> exitValues = Async.ExecuteProcess(DekiContext.Current.Deki.ImageMagickIdentifyPath, "-format \"%wx%h \" -", file.Stream, new Result<Tuplet<int,Stream,Stream>>(TimeSpan.FromMilliseconds(DekiContext.Current.Deki.ImageMagickTimeout))).Wait();

                // record stats about this imagemagick execution
                sw.Stop();
                AddToStats(HEADERSTAT_IDENTIFY, sw.ElapsedMilliseconds);

                int status = exitValues.Item1;
                Stream outputStream = exitValues.Item2;
                Stream errorStream = exitValues.Item3;

                // parse output
                string output = new StreamReader(outputStream).ReadToEnd();
                string[] dimensions = output.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                foreach(string dimension in dimensions) {
                    int tmpWidth;
                    int tmpHeight;
                    string[] parts = dimension.Split(new char[] { 'x' }, 2);
                    if(parts.Length == 2) {
                        int.TryParse(parts[0], out tmpWidth);
                        int.TryParse(parts[1], out tmpHeight);
                        if((tmpWidth > 0) && (tmpHeight > 0)) {
                            ++frames;
                            width = Math.Max(width, tmpWidth);
                            height = Math.Max(height, tmpHeight);
                        }
                    }
                }
                _log.InfoFormat("Imagemagick identify finished in {0}ms", sw.ElapsedMilliseconds);
            }
            return frames > 0;
        }

        private static void ThrowOnBadPreviewImage() {
            string filepath = Utils.PathCombine(DekiContext.Current.Deki.ResourcesPath, "images", BADPREVIEWFILENAME);
            try {

                lock (BADPREVIEWFILENAME) {
                    if (_badPreviewImageContent == null || _badPreviewImageContent.Length == 0) {
                        _badPreviewImageContent = System.IO.File.ReadAllBytes(filepath);
                    }
                }
            }
            catch (Exception) { }

            if (_badPreviewImageContent != null && _badPreviewImageContent.Length > 0) {
                throw new AttachmentPreviewBadImageFatalException(MimeType.FromFileExtension(BADPREVIEWFILENAME), _badPreviewImageContent);
            }
            throw new AttachmentPreviewNoImageFatalException();
        }

        private static bool IsPreviewSizeAllowed(uint width, uint height) {
            uint maxWidth = Math.Max(1024, Math.Max(DekiContext.Current.Instance.ImageThumbPixels, DekiContext.Current.Instance.ImageWebviewPixels));
            uint maxHeight = Math.Max(768, Math.Max(DekiContext.Current.Instance.ImageThumbPixels, DekiContext.Current.Instance.ImageWebviewPixels));

            if(height * width > maxWidth * maxHeight) {
                return false;
            }
            return true;
        }

        private static void AddToStats(string statName, long value) {
            string current;
            if(DekiContext.Current.Stats.TryGetValue(statName, out current)) {
                long currentLong;
                if(long.TryParse(current, out currentLong)) {
                    currentLong += value;
                    current = currentLong.ToString();
                }
            }
            if(string.IsNullOrEmpty(current)) {
                current = value.ToString();
            }
            DekiContext.Current.Stats[statName] = current;
        }
    }
}
