namespace Nwc.XmlRpc
{
  using System;
  using System.Collections;
  using System.Xml;
 
  class XmlRpcRequestSerializer : XmlRpcSerializer
  {
    static public void Serialize(XmlTextWriter output, XmlRpcRequest req)
      {
	output.WriteStartDocument();
	output.WriteStartElement(METHOD_CALL);
	output.WriteElementString(METHOD_NAME,req.MethodName);
	output.WriteStartElement(PARAMS);
	foreach (Object param in req.Params)
	  {
	    output.WriteStartElement(PARAM);
	    output.WriteStartElement(VALUE);
	    SerializeObject(output, param);
	    output.WriteEndElement();
	    output.WriteEndElement();
	  }

	output.WriteEndElement();
	output.WriteEndElement();
      }
  }
}
