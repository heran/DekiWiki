namespace Nwc.XmlRpc
{
  using System;
  using System.Collections;
  using System.IO;
  using System.Xml;

  public class XmlRpcResponse
  {
    protected Object _value;
    public bool IsFault;

    public XmlRpcResponse()
      {
	Value = null;
	IsFault = false;
      }

    public XmlRpcResponse(int code, String message) : this()
      {
	SetFault(code,message);
      }

    public Object Value
      {
	get { return _value; }
	set {
	  IsFault = false;
	  _value = value;
	}
      }

    public int FaultCode
      {
	get {
	  if (!IsFault)
	    return 0;
	  else
	    return (int)((Hashtable)_value)["faultCode"];
	}
      }

    public String FaultString
      {
	get {
	  if (!IsFault)
	    return "";
	  else
	    return (String)((Hashtable)_value)["faultString"];
	}
      }

    public void SetFault(int code, String message)
      {
	Hashtable fault = new Hashtable();
	fault.Add("faultCode", code);
	fault.Add("faultString", message);
	Value = fault;
	IsFault = true;
      }

    override public String ToString()
      {
	StringWriter strBuf = new StringWriter();
	XmlTextWriter xml = new XmlTextWriter(strBuf);
	xml.Formatting = Formatting.Indented;
	xml.Indentation = 4;
	XmlRpcResponseSerializer.Serialize(xml,this);
	xml.Flush();
	xml.Close();
	return strBuf.ToString();
      }
  }
}
