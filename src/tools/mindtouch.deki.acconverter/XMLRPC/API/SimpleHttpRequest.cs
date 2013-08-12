namespace Nwc.XmlRpc
{
  using System;
  using System.IO;
  using System.Net.Sockets;
  using System.Collections;
  using System.Diagnostics;

  public class SimpleHttpRequest
  {
    private String _httpMethod = null;
    private String _protocol;
    private String _filePathFile = null;
    private String _filePathDir = null;
    private String __filePath;
    private TcpClient _client;
    private StreamReader _input;
    private StreamWriter _output;
    private Hashtable _headers;

    public SimpleHttpRequest(TcpClient client)
      {
	_client = client;
	_output = new StreamWriter(client.GetStream());
	_input = new StreamReader(client.GetStream());
	GetRequestMethod();
	GetRequestHeaders();
      }

    public StreamWriter Output
      {
	get { return _output; }
      }

    public StreamReader Input
      {
	get { return _input; }
      }

    public TcpClient Client
      {
	get { return _client; }
      }

    private String _filePath
    {
      get { return __filePath; }
      set 
	{
	  __filePath = value;
	  _filePathDir = null;
	  _filePathFile = null;
	}
    }

    public String HttpMethod 
      {
	get { return _httpMethod; }
      }

    public String Protocol
      {
	get { return _protocol; }
      }

    public String FilePath
      {
	get { return _filePath; }
      }

    public String FilePathFile
      {
	get
	  {
	    if (_filePathFile != null)
	      return _filePathFile;

	    int i = FilePath.LastIndexOf("/");

	    if (i == -1)
	      return "";
	    
	    i++;
	    _filePathFile = FilePath.Substring(i, FilePath.Length - i);
	    return _filePathFile;
	  }
      }

    public String FilePathDir
      {
	get
	  {
	    if (_filePathDir != null)
	      return _filePathDir;

	    int i = FilePath.LastIndexOf("/");

	    if (i == -1)
	      return "";
	    
	    i++;
	    _filePathDir = FilePath.Substring(0, i);
	    return _filePathDir;
	  }
      }

    private void GetRequestMethod()
      {
	string req = _input.ReadLine();
	if (req == null)
	  throw new ApplicationException("Void request.");

	if (0 == String.Compare("GET ", req.Substring (0, 4), true))
	  _httpMethod = "GET";
	else if (0 == String.Compare("POST ", req.Substring (0, 5), true))
	  _httpMethod = "POST";
	else
	  throw new InvalidOperationException("Unrecognized method in query: " + req);

	req = req.TrimEnd ();
	int idx = req.IndexOf(' ') + 1;
	if (idx >= req.Length)
	  throw new ApplicationException ("What do you want?");

	string page_protocol = req.Substring(idx);
	int idx2 = page_protocol.IndexOf(' ');
	if (idx2 == -1)
	  idx2 = page_protocol.Length;
		
	_filePath = page_protocol.Substring(0, idx2).Trim();
	_protocol = page_protocol.Substring(idx2).Trim();
      }


    private void GetRequestHeaders()
	{
	    String line;
	    int idx;

	    _headers = new Hashtable();

	    while ((line = _input.ReadLine ()) != "") 
		{
		    if (line == null)
			{
			    break;
			}

		    idx = line.IndexOf (':');
		    if (idx == -1 || idx == line.Length - 1)
			{
			  Logger.WriteEntry("Malformed header line: " + line, EventLogEntryType.Information);
			  continue;
			}

		    String key = line.Substring (0, idx);
		    String value = line.Substring (idx + 1);

		    try 
			{
			    _headers.Add(key, value);
			} 
		    catch (Exception) 
			{
			  Logger.WriteEntry("Duplicate header key in line: " + line, EventLogEntryType.Information);
			}
		}
	}
    
    override public String ToString()
      {
	return HttpMethod + " " + FilePath + " " + Protocol;
      }

    public void Close()
      {
	_output.Flush();
	_output.Close();
	_input.Close();
	_client.Close();
      }
  }
}
