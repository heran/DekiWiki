namespace Nwc.XmlRpc
{
  using System;
  using System.Reflection;

  /// <summery>
  /// Simple tagging attribute to indicate participation is XML-RPC exposure.
  /// </summary>
  /// If present at the class level it indicates that this class does explicitly 
  /// expose methods. If present at the method level it denotes that the method
  /// is exposed.
  [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method, 
        AllowMultiple=false,
        Inherited=true
    )]
  public class XmlRpcExposedAttribute : Attribute
  {
    public static Boolean IsExposed(MemberInfo mi)
      {
	foreach (Attribute attr in mi.GetCustomAttributes(true))
	  {
	    if (attr is XmlRpcExposedAttribute)
	      return true;
	  }
	return false;
      }
  }
}
