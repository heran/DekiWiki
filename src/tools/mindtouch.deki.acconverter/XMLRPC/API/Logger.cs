namespace Nwc.XmlRpc
{
  using System;
  using System.Diagnostics;

  ///<sumary>
  ///This is a logging singleton that allows for plugin log delegates.
  ///</summary>
  public class Logger
  {
    public delegate void LoggerDelegate(String message, EventLogEntryType type);
    static public LoggerDelegate Delegate = null;

    static public void WriteEntry(String message, EventLogEntryType type)
      {
	if (Delegate != null)
	  Delegate(message, type);
      }
  }
}
