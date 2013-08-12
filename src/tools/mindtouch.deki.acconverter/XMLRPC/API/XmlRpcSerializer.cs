namespace Nwc.XmlRpc
{
  using System;
  using System.Collections;
  using System.IO;
  using System.Xml;
  using System.Diagnostics;

  class XmlRpcSerializer : XmlRpcXmlTokens
  {
    static public void SerializeObject(XmlTextWriter output, Object obj)
      {
	if (obj == null)
	  return;

	if (obj is byte[])
	  {
	    byte[] ba = (byte[])obj;
	    output.WriteStartElement(BASE64);
	    output.WriteBase64(ba,0,ba.Length);
	    output.WriteEndElement();
	  }
	else if (obj is String)
	  {
	    output.WriteElementString(STRING,obj.ToString());
	  }
	else if (obj is Int32)
	  {
	    output.WriteElementString(INT,obj.ToString());
	  }
	else if (obj is DateTime)
	  {
	    output.WriteElementString(DATETIME,((DateTime)obj).ToString(ISO_DATETIME));
	  }
	else if (obj is Double)
	  {
	    output.WriteElementString(DOUBLE,obj.ToString());
	  }
	else if (obj is Boolean)
	  {
	    output.WriteElementString(BOOLEAN, ((((Boolean)obj) == true)?"1":"0"));
	  }
	else if (obj is ArrayList)
	  {
	    output.WriteStartElement(ARRAY);
	    output.WriteStartElement(DATA);
	    if (((ArrayList)obj).Count > 0)
	      {
		foreach (Object member in ((ArrayList)obj))
		  {
		    output.WriteStartElement(VALUE);
		    SerializeObject(output,member);
		    output.WriteEndElement();
		  }
	      }
	    output.WriteEndElement();
	    output.WriteEndElement();
	  }
	else if (obj is Hashtable)
	  {
	    Hashtable h = (Hashtable)obj;
	    output.WriteStartElement(STRUCT);	    
	    foreach (String key in h.Keys)
	      {
		output.WriteStartElement(MEMBER);
		output.WriteElementString(NAME,key);
		output.WriteStartElement(VALUE);
		SerializeObject(output,h[key]);
		output.WriteEndElement();
		output.WriteEndElement();
	      }
	    output.WriteEndElement();
	  }

      }
  }
}
