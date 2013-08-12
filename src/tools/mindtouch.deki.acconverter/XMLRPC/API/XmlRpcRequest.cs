namespace Nwc.XmlRpc
{
  using System;
  using System.Collections;
  using System.IO;
  using System.Xml;
  using System.Net;
  using System.Text;
  using System.Reflection;
  using System.Configuration;
  
  public class XmlRpcRequest
  {
    public String MethodName = null;
    public ArrayList Params = null;
    private Encoding _encoding = new ASCIIEncoding();

    public XmlRpcRequest()
      {
	Params = new ArrayList();
      }

    public String MethodNameObject
      {
	get {
	  int index = MethodName.IndexOf(".");

	  if (index == -1)
	    return MethodName;

	  return MethodName.Substring(0,index);
	}
      }

    public String MethodNameMethod
      {
	get {
	  int index = MethodName.IndexOf(".");

	  if (index == -1)
	    return MethodName;

	  return MethodName.Substring(index + 1, MethodName.Length - index - 1);
	}
      }

    public XmlRpcResponse Send(String url)
      {
	HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
	request.Method = "POST";
	request.ContentType = "test/xml";
	WebProxy myProxy = new WebProxy();
	try
	{
		if (ConfigurationManager.AppSettings["ProxyAddress"].ToString() != "")
		{
			string proxyAddress = ConfigurationManager.AppSettings["ProxyAddress"].ToString();
			if (proxyAddress.Length > 0)
			{
				Uri newUri = new Uri(proxyAddress);
				// Associate the newUri object to 'myProxy' object so that new myProxy settings can be set.
				myProxy.Address = newUri;
				// Create a NetworkCredential object and associate it with the Proxy property of request object.
				myProxy.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["ProxyUser"].ToString(), ConfigurationManager.AppSettings["ProxyPass"].ToString());
				request.Proxy = myProxy;
			}
		}

	}
	catch (Exception)
	{
		
	}
    //request.AllowWriteStreamBuffering = true;
    //request.ReadWriteTimeout = 1000;
    //request.Timeout = 1000;   
    //request.ContentLength = strContent.Length;
	
	Stream stream = request.GetRequestStream();
	XmlTextWriter xml = new XmlTextWriter(stream, _encoding);
	XmlRpcRequestSerializer.Serialize(xml, this);
	xml.Flush();
	xml.Close();

	HttpWebResponse response = (HttpWebResponse)request.GetResponse();
	StreamReader input = new StreamReader(response.GetResponseStream());

	XmlRpcResponse resp = XmlRpcResponseDeserializer.Parse(input);
	input.Close();
	response.Close();
	return resp;
      }

      public XmlRpcResponse Send2(String url, String strContent)
      {
          HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
          string postData = strContent;
          ASCIIEncoding encoding = new ASCIIEncoding();
          byte[] byte1 = encoding.GetBytes(postData);
          // Set the content type of the data being posted.
          request.ContentType = "text/xml";
          request.Method = "POST";
          // Set the content length of the string being posted.
      //    request.ContentLength = postData.Length;
		  WebProxy myProxy = new WebProxy();
		  try
		  {
			  if (ConfigurationManager.AppSettings["ProxyAddress"].ToString() != "")
			  {
				  string proxyAddress = ConfigurationManager.AppSettings["ProxyAddress"].ToString();
				  if (proxyAddress.Length > 0)
				  {
					  Uri newUri = new Uri(proxyAddress);
					  // Associate the newUri object to 'myProxy' object so that new myProxy settings can be set.
					  myProxy.Address = newUri;
					  // Create a NetworkCredential object and associate it with the Proxy property of request object.
					  myProxy.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["ProxyUser"].ToString(), ConfigurationManager.AppSettings["ProxyPass"].ToString());
					  request.Proxy = myProxy;
				  }
			  }

		  }
          catch (Exception)
		  {

		  }
          Stream newStream = request.GetRequestStream();
          XmlTextWriter xml = new XmlTextWriter(newStream, _encoding);
          XmlRpcRequestSerializer.Serialize(xml, this);
          xml.Flush();
          xml.Close();        

          HttpWebResponse response = (HttpWebResponse)request.GetResponse();
          StreamReader input = new StreamReader(response.GetResponseStream());

          XmlRpcResponse resp = XmlRpcResponseDeserializer.Parse(input);
          input.Close();
          response.Close();

          return resp;
      }



    public Object Invoke(Object target)
      {
	Type type = target.GetType();
	MethodInfo method = type.GetMethod(MethodNameMethod);

	if (method == null)
	  throw new XmlRpcException(-2,"Method " + MethodNameMethod + " not found.");

	if (XmlRpcExposedAttribute.IsExposed(target.GetType()) && 
	    !XmlRpcExposedAttribute.IsExposed(method))
	  throw new XmlRpcException(-3, "Method " + MethodNameMethod + " is not exposed.");

	Object[] args = new Object[Params.Count];

	for (int i = 0; i < Params.Count; i++)
	  args[i] = Params[i];

	return method.Invoke(target, args);
      }

    override public String ToString()
      {
	StringWriter strBuf = new StringWriter();
	XmlTextWriter xml = new XmlTextWriter(strBuf);
	xml.Formatting = Formatting.Indented;
	xml.Indentation = 4;
	XmlRpcRequestSerializer.Serialize(xml,this);
	xml.Flush();
	xml.Close();
	return strBuf.ToString();
      }
  }
}
