namespace Nwc.XmlRpc
{
  using System;
  using System.Collections;
  using System.IO;
  using System.Xml;

  class XmlRpcRequestDeserializer : XmlRpcDeserializer
  {
    static private XmlRpcRequestDeserializer _singleton;
    static public XmlRpcRequestDeserializer Singleton
      {
	get
	  {
	    if (_singleton == null)
	      _singleton = new XmlRpcRequestDeserializer();

	    return _singleton;
	  }
      }

    public static XmlRpcRequest Parse(StreamReader xmlData)
      {
	XmlTextReader reader = new XmlTextReader(xmlData);
	XmlRpcRequest request = new XmlRpcRequest();
	bool done = false;


	while (!done && reader.Read())
	  {
	    Singleton.ParseNode(reader); // Parent parse...
            switch (reader.NodeType)
	      {
	      case XmlNodeType.EndElement:
		switch (reader.Name)
		  {
		  case METHOD_NAME:
		    request.MethodName = Singleton._text;
		    break;
		  case METHOD_CALL:
		    done = true;
		    break;
		  case PARAM:
		    request.Params.Add(Singleton._value);
		    Singleton._text = null;
		    break;
		  }
		break;
	      default:
		Singleton.ParseNode(reader);
		break;
	      }	
	  }
	return request;
      }
  }
}
