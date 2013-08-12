namespace Nwc.XmlRpc
{
  using System;
  using System.Collections;
  using System.Xml;
 
  class XmlRpcResponseSerializer : XmlRpcSerializer
  {
    static public void Serialize(XmlTextWriter output, XmlRpcResponse response)
      {
	output.WriteStartDocument();
	output.WriteStartElement(METHOD_RESPONSE);

	if (response.IsFault)
	  output.WriteStartElement(FAULT);
	else
	  {
	    output.WriteStartElement(PARAMS);
	    output.WriteStartElement(PARAM);
	  }

	output.WriteStartElement(VALUE);

	SerializeObject(output,response.Value);

	output.WriteEndElement();

	output.WriteEndElement();
	if (!response.IsFault)
	  output.WriteEndElement();
	output.WriteEndElement();
      }
  }
}
