namespace Nwc.XmlRpc
{
  using System;
  using System.Collections;
  using System.IO;
  using System.Xml;
  using System.Diagnostics;
  using System.Globalization;

  class XmlRpcDeserializer : XmlRpcXmlTokens
  {
    private static DateTimeFormatInfo _dateFormat = new DateTimeFormatInfo();
    
    protected String _text;
    protected Object _value;
    protected Object _name;

    private Object _container;
    private Stack _containerStack;

    public XmlRpcDeserializer()
      {
	_container = null;
	_containerStack = new Stack();
	_dateFormat.FullDateTimePattern = ISO_DATETIME;
      }

    public void ParseNode(XmlTextReader reader)
      {
	switch (reader.NodeType)
	  {
	  case XmlNodeType.Element:
	    Logger.WriteEntry("Element " + reader.Name, EventLogEntryType.Information);
	    switch (reader.Name)
	      {
	      case VALUE:
		_value = null;
		_text = null;
		break;
	      case STRUCT:
		if (_container != null)
		  _containerStack.Push(_container);
		_container = new Hashtable();
		break;
	      case ARRAY:
		if (_container != null)
		  _containerStack.Push(_container);
		_container = new ArrayList();
		break;
	      }
	    break;
	  case XmlNodeType.EndElement:
	    Logger.WriteEntry("End Element " + reader.Name, EventLogEntryType.Information);
	    switch (reader.Name)
	      {
	      case BASE64:
		_value = Convert.FromBase64String(_text);
		break;
	      case BOOLEAN:
		int val = Int16.Parse(_text);
		if (val == 0)
		  _value = false;
		else if (val == 1)
		  _value = true;
		break;
	      case STRING:
		_value = _text;
		break;
	      case DOUBLE:
		_value = Double.Parse(_text);
		break;
	      case INT:
	      case ALT_INT:
		_value = Int32.Parse(_text);
		break;
	      case DATETIME:
		_value = DateTime.ParseExact(_text, "F", _dateFormat);
		break;
	      case NAME:
		_name = _text;
		break;
	      case VALUE:
		if (_value == null)
		  _value = _text; // some kits don't use <string> tag, they just do <value>

		if ((_container != null) && (_container is ArrayList)) // in an array?  If so add value to it.
		  ((ArrayList)_container).Add(_value);
		break;
	      case MEMBER:
		if ((_container != null) && (_container is Hashtable)) // in an struct?  If so add value to it.
		((Hashtable)_container).Add(_name, _value);
		break;
	      case ARRAY:
	      case STRUCT:
		_value = _container;
		_container = (_containerStack.Count == 0)? null : _containerStack.Pop();
		break;
	      }
	    break;
	  case XmlNodeType.Text:
	    Logger.WriteEntry("Text " + reader.Value, EventLogEntryType.Information);
	    _text = reader.Value;
	    break;
	  default:
	    break;
	  }	

	Logger.WriteEntry("Text now: " + _text, EventLogEntryType.Information);
	Logger.WriteEntry("Value now: " + id(_value), EventLogEntryType.Information);
	Logger.WriteEntry("Container now: " + id(_container), EventLogEntryType.Information);
      }

    private String id(Object x)
      {
	if (x == null)
	  return "null";

	return x.GetType().Name + "[" + x.GetHashCode() + "]";
      }
  }
}


