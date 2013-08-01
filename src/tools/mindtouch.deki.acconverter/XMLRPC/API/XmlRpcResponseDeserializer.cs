namespace Nwc.XmlRpc
{
  using System;
  using System.Collections;
  using System.IO;
  using System.Xml;

  class XmlRpcResponseDeserializer : XmlRpcDeserializer
  {
    static private XmlRpcResponseDeserializer _singleton;
    static public XmlRpcResponseDeserializer Singleton
      {
	get
	  {
	    if (_singleton == null)
	      _singleton = new XmlRpcResponseDeserializer();

	    return _singleton;
	  }
      }

    public static XmlRpcResponse Parse(StreamReader xmlData)
      {
	XmlTextReader reader = new XmlTextReader(xmlData);
	XmlRpcResponse response = new XmlRpcResponse();
	bool done = false;

	while (!done && reader.Read())
	  {
	    Singleton.ParseNode(reader); // Parent parse...
            switch (reader.NodeType)
	      {
	      case XmlNodeType.EndElement:
		switch (reader.Name)
		  {
		  case FAULT:
		    response.Value = Singleton._value;
		    response.IsFault = true;
		    break;
		  case PARAM:
		    response.Value = Singleton._value;
		    Singleton._value = null;
		    Singleton._text = null;
		    break;
		  }
		break;
	      default:
		break;
	      }	
	  }
	return response;
      }
  }
}
