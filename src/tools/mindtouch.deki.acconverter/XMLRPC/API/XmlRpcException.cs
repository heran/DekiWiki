namespace Nwc.XmlRpc
{
  using System;

  public class XmlRpcException : Exception
    {
      private int _code;
  
      public XmlRpcException(int code, String message) : base(message)
	{
	  _code = code;
	}

      public int Code
	{
	  get { return _code; }
	}

      override public String ToString()
	{
	  return "Code: " + _code + " Message: " + ((Exception)this).ToString();
	}
    }
}
