namespace Nwc.XmlRpc
{
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.IO;
  using System.Net;
  using System.Net.Sockets;
  using System.Text;
  using System.Threading;
  using System.Xml;

  /// <summary>A simple HTTP server.</summary>
public class XmlRpcServer : IEnumerable
  {
    private TcpListener _myListener ;
    private int _port;
    private Hashtable _handlers;
    private XmlRpcSystemObject _system;
		
    //The constructor which make the TcpListener start listening on the
    //given port. It also calls a Thread on the method StartListen(). 
    public XmlRpcServer(int port)
      {
	_port = port;
	_handlers = new Hashtable();
	_system = new XmlRpcSystemObject(this);
      }

    public void Start()
      {
	try
	  {
	    //start listing on the given port
	    _myListener = new TcpListener(_port) ;
	    _myListener.Start();
	    //start the thread which calls the method 'StartListen'
	    Thread th = new Thread(new ThreadStart(StartListen));
	    th.Start();
	  }
	catch(Exception e)
	  {
	    Logger.WriteEntry("An Exception Occurred while Listening :" +e.ToString(), EventLogEntryType.Error);
	  }
      }

    public IEnumerator GetEnumerator()
      {
	return _handlers.GetEnumerator();
      }

    public Object this [String name]
      {
	get { return _handlers[name]; }
      }

    /// <summary>
    /// This function send the Header Information to the client (Browser)
    /// </summary>
    /// <param name="sHttpVersion">HTTP Version</param>
    /// <param name="sMIMEHeader">Mime Type</param>
    /// <param name="iTotBytes">Total Bytes to be sent in the body</param>
    /// <param name="sStatusCode"></param>
    /// <param name="output">Socket reference</param>
    public void SendHeader(string sHttpVersion, string sMIMEHeader, long iTotBytes, string sStatusCode, TextWriter output)
      {
	String sBuffer = "";
			
	// if Mime type is not provided set default to text/html
	if (sMIMEHeader.Length == 0 )
	  {
	    sMIMEHeader = "text/html";  // Default Mime Type is text/html
	  }

	sBuffer = sBuffer + sHttpVersion + sStatusCode + "\r\n";
	sBuffer = sBuffer + "Connection: close\r\n";
	if (iTotBytes > 0)
	  sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n";
	sBuffer = sBuffer + "Server: XmlRpcServer \r\n";
	sBuffer = sBuffer + "Content-Type: " + sMIMEHeader + "\r\n";

	sBuffer = sBuffer += "\r\n";

	output.Write(sBuffer);
      }

    ///<summary>
    ///This method Accepts new connections and dispatches them when appropriate.
    ///</summary>
    public void StartListen()
      {
	while(true)
	  {
	    //Accept a new connection
	    TcpClient client = _myListener.AcceptTcpClient();
	    SimpleHttpRequest httpReq = new SimpleHttpRequest(client);

	    if (httpReq.HttpMethod == "POST")
	      {
		try
		  {
		    HttpPost(httpReq);
		  }
		catch (Exception e)
		  {
		    Logger.WriteEntry("Failed on post: " + e, EventLogEntryType.Error);
		  }
	      }
	    else
	      {
		Logger.WriteEntry("Only POST methods are supported: " + httpReq.HttpMethod + " ignored", EventLogEntryType.FailureAudit);
	      }

	    httpReq.Close();
	  }
      }

    public void HttpPost(SimpleHttpRequest req)
      {
	XmlRpcRequest rpc = XmlRpcRequestDeserializer.Parse(req.Input);

	XmlRpcResponse resp = new XmlRpcResponse();
	Object target = _handlers[rpc.MethodNameObject];
	
	if (target == null)
	  {
	    resp.SetFault(-1, "Object " + rpc.MethodNameObject + " not registered.");
	  }
	else
	  {
	    try
	      {
		resp.Value = rpc.Invoke(target);
	      }
	    catch (XmlRpcException e)
	      {
		resp.SetFault(e.Code, e.Message);
	      }
	    catch (Exception e2)
	      {
		resp.SetFault(-1, e2.Message);
	      }
	  }

	Logger.WriteEntry(resp.ToString(), EventLogEntryType.Information);

	SendHeader(req.Protocol, "text/xml", 0, " 200 OK", req.Output);
	req.Output.Flush();
	XmlTextWriter xml = new XmlTextWriter(req.Output);
	XmlRpcResponseSerializer.Serialize(xml, resp);
	xml.Flush();
	req.Output.Flush();
      }

    ///<summary>
    ///Add an XML-RPC handler object by name.
    ///</summary>
    public void Add(String name, Object obj)
      {
	_handlers.Add(name,obj);
      }
  }
}
