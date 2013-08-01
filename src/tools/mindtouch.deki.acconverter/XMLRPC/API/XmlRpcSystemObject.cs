namespace Nwc.XmlRpc
{
  using System;
  using System.Collections;
  using System.Reflection;
  using System.Diagnostics;

  [XmlRpcExposed]
public class XmlRpcSystemObject
  {
    private XmlRpcServer _server;

    public XmlRpcSystemObject(XmlRpcServer server)
      {
	_server = server;
	server.Add("system",this);
      }

    [XmlRpcExposed]
      public ArrayList listMethods()
      {
	ArrayList methods = new ArrayList();
	Boolean considerExposure;

	foreach (DictionaryEntry handlerEntry in _server)
	  {
	    considerExposure = XmlRpcExposedAttribute.IsExposed(handlerEntry.Value.GetType());

	    foreach (MemberInfo mi in handlerEntry.Value.GetType().GetMembers())
	      {
 		if (mi.MemberType != MemberTypes.Method)
 		  continue;

		if(!((MethodInfo)mi).IsPublic)
		  continue;

		if (considerExposure && !XmlRpcExposedAttribute.IsExposed(mi))
		  continue;

		methods.Add(handlerEntry.Key + "." + mi.Name);
	      }
	  }

	return methods;
      }

    [XmlRpcExposed]
      public ArrayList methodSignature(String name)
      {
	ArrayList signatures = new ArrayList();
	int index = name.IndexOf('.');

	if (index < 0)
	  return signatures;

	String oName = name.Substring(0,index);
	Object obj = _server[oName];

	if (obj == null)
	  return signatures;

	MemberInfo[] mi = obj.GetType().GetMember(name.Substring(index + 1));
	
	if (mi == null || mi.Length != 1) // for now we want a single signature
	  return signatures;

	MethodInfo method;

	try
	  {
	    method = (MethodInfo)mi[0];
	  }
	catch (Exception e)
	  {
	    Logger.WriteEntry("Attempted methodSignature call on " + mi[0] + " caused: " + e,
			      EventLogEntryType.Information);
	    return signatures;
	  }

	if (!method.IsPublic)
	  return signatures;

	ArrayList signature = new ArrayList();
	signature.Add(method.ReturnType.Name);

	foreach (ParameterInfo param in method.GetParameters())
	  {
	    signature.Add(param.ParameterType.Name);
	  }


	signatures.Add(signature);

	return signatures;
      }

    [XmlRpcExposed]
      public String methodHelp(String name)
      {
	return "methodHelp not yet implemented.";
      }
  }
}
