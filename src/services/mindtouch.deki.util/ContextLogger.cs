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
using System.Globalization;
using log4net;
using log4net.Core;

namespace MindTouch.Deki.Util {

    public class ContextLogger : ILog {

        //--- Fields ---
        private readonly ILog _rootLogger;
        private readonly string _context;
        private readonly Type _type;

        //--- Constructors ---
        public ContextLogger(ILog rootLogger, string context) {
            _type = GetType();
            _rootLogger = rootLogger;
            _context = context;
        }

        //--- Properties ---
        public bool IsDebugEnabled { get { return _rootLogger.IsDebugEnabled; } }
        public bool IsInfoEnabled { get { return _rootLogger.IsInfoEnabled; } }
        public bool IsWarnEnabled { get { return _rootLogger.IsWarnEnabled; } }
        public bool IsErrorEnabled { get { return _rootLogger.IsFatalEnabled; } }
        public bool IsFatalEnabled { get { return _rootLogger.IsFatalEnabled; } }
        public ILogger Logger { get { return _rootLogger.Logger; } }
        public string Context { get { return _context; } }

        //--- Methods ---
        public void Debug(object message) {
            if(!IsDebugEnabled) {
                return;
            }
            Logger.Log(_type, Level.Debug, Format(message), null);
        }

        public void Debug(object message, Exception exception) {
            if(!IsDebugEnabled) {
                return;
            }
            Logger.Log(_type, Level.Debug, Format(message), exception);
        }

        public void DebugFormat(string format, params object[] args) {
            if(!IsDebugEnabled) {
                return;
            }
            Logger.Log(_type, Level.Debug, Format(string.Format(CultureInfo.InvariantCulture, format, args)), null);
        }

        public void DebugFormat(string format, object arg0) {
            if(!IsDebugEnabled) {
                return;
            }
            Logger.Log(_type, Level.Debug, Format(string.Format(CultureInfo.InvariantCulture, format, arg0)), null);
        }

        public void DebugFormat(string format, object arg0, object arg1) {
            if(!IsDebugEnabled) {
                return;
            }
            Logger.Log(_type, Level.Debug, Format(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1)), null);
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2) {
            if(!IsDebugEnabled) {
                return;
            }
            Logger.Log(_type, Level.Debug, Format(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2)), null);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args) {
            if(!IsDebugEnabled) {
                return;
            }
            Logger.Log(_type, Level.Debug, Format(string.Format(provider, format, args)), null);
        }

        public void Info(object message) {
            if(!IsInfoEnabled) {
                return;
            }
            Logger.Log(_type, Level.Info, Format(message), null);
        }

        public void Info(object message, Exception exception) {
            if(!IsInfoEnabled) {
                return;
            }
            Logger.Log(_type, Level.Info, Format(message), exception);
        }

        public void InfoFormat(string format, params object[] args) {
            if(!IsInfoEnabled) {
                return;
            }
            Logger.Log(_type, Level.Info, Format(string.Format(CultureInfo.InvariantCulture, format, args)), null);
        }

        public void InfoFormat(string format, object arg0) {
            if(!IsInfoEnabled) {
                return;
            }
            Logger.Log(_type, Level.Info, Format(string.Format(CultureInfo.InvariantCulture, format, arg0)), null);
        }

        public void InfoFormat(string format, object arg0, object arg1) {
            if(!IsInfoEnabled) {
                return;
            }
            Logger.Log(_type, Level.Info, Format(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1)), null);
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2) {
            if(!IsInfoEnabled) {
                return;
            }
            Logger.Log(_type, Level.Info, Format(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2)), null);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args) {
            if(!IsInfoEnabled) {
                return;
            }
            Logger.Log(_type, Level.Info, Format(string.Format(provider, format, args)), null);
        }

        public void Warn(object message) {
            if(!IsWarnEnabled) {
                return;
            }
            Logger.Log(_type, Level.Warn, Format(message), null);
        }

        public void Warn(object message, Exception exception) {
            if(!IsWarnEnabled) {
                return;
            }
            Logger.Log(_type, Level.Warn, Format(message), exception);
        }

        public void WarnFormat(string format, params object[] args) {
            if(!IsWarnEnabled) {
                return;
            }
            Logger.Log(_type, Level.Warn, Format(string.Format(CultureInfo.InvariantCulture, format, args)), null);
        }

        public void WarnFormat(string format, object arg0) {
            if(!IsWarnEnabled) {
                return;
            }
            Logger.Log(_type, Level.Warn, Format(string.Format(CultureInfo.InvariantCulture, format, arg0)), null);
        }

        public void WarnFormat(string format, object arg0, object arg1) {
            if(!IsWarnEnabled) {
                return;
            }
            Logger.Log(_type, Level.Warn, Format(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1)), null);
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2) {
            if(!IsWarnEnabled) {
                return;
            }
            Logger.Log(_type, Level.Warn, Format(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2)), null);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args) {
            if(!IsWarnEnabled) {
                return;
            }
            Logger.Log(_type, Level.Warn, Format(string.Format(provider, format, args)), null);
        }

        public void Error(object message) {
            Logger.Log(_type, Level.Error, Format(message), null);
        }

        public void Error(object message, Exception exception) {
            Logger.Log(_type, Level.Error, Format(message), exception);
        }

        public void ErrorFormat(string format, params object[] args) {
            Logger.Log(_type, Level.Error, Format(string.Format(CultureInfo.InvariantCulture, format, args)), null);
        }

        public void ErrorFormat(string format, object arg0) {
            Logger.Log(_type, Level.Error, Format(string.Format(CultureInfo.InvariantCulture, format, arg0)), null);
        }

        public void ErrorFormat(string format, object arg0, object arg1) {
            Logger.Log(_type, Level.Error, Format(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1)), null);
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2) {
            Logger.Log(_type, Level.Error, Format(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2)), null);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args) {
            Logger.Log(_type, Level.Error, Format(string.Format(provider, format, args)), null);
        }

        public void Fatal(object message) {
            Logger.Log(_type, Level.Fatal, Format(message), null);
        }

        public void Fatal(object message, Exception exception) {
            Logger.Log(_type, Level.Fatal, Format(message), exception);
        }

        public void FatalFormat(string format, params object[] args) {
            Logger.Log(_type, Level.Fatal, Format(string.Format(CultureInfo.InvariantCulture, format, args)), null);
        }

        public void FatalFormat(string format, object arg0) {
            Logger.Log(_type, Level.Fatal, Format(string.Format(CultureInfo.InvariantCulture, format, arg0)), null);
        }

        public void FatalFormat(string format, object arg0, object arg1) {
            Logger.Log(_type, Level.Fatal, Format(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1)), null);
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2) {
            Logger.Log(_type, Level.Fatal, Format(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2)), null);
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args) {
            Logger.Log(_type, Level.Fatal, Format(string.Format(provider, format, args)), null);
        }

        private string Format(object message) {
            return _context + message;
        }
    }
}