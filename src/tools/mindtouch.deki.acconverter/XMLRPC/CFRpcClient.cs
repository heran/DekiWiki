using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nwc.XmlRpc;
using System.Net;
using System.Configuration;
using System.Collections;
using MindTouch.Tools.ConfluenceConverter.XMLRPC.Types;


namespace MindTouch.Tools.ConfluenceConverter.XMLRPC
{
    public interface WikiType
    {
        Hashtable GetAsHashtable();
        void PopulateFromHashtable(Hashtable hash);
    }

    public class CFRpcClient
    {
        private const int MAX_CALL_ATTEMPTS = 2;
        private string authToken = "";

        string _url;
        string _username;
        string _password;

        public CFRpcClient(string url, string username, string password)
        {
            _url = url;
            _username = username;
            _password = password;
        }


       

        #region Private Methods
        //These methods should be private, but are marked as internal to allow the CFRpcClientTests to access them.
        internal XmlRpcResponse CallConfluenceMethod(string methodName, params object[] parameters)
        {
            return CallConfluenceMethod(1, methodName, parameters);
        }

        internal XmlRpcResponse CallConfluenceMethod(int attemptNumber, string methodName, params object[] parameters)
        {          

            string sAuthToken = GetAuthToken();

            XmlRpcResponse response = null;

            
                XmlRpcRequest request = new XmlRpcRequest();

                //Set the method for the request.
                request.MethodName = methodName;

                //Add the Parameters to the request
                request.Params.Clear();
                request.Params.Add(sAuthToken);
                foreach (object parameter in parameters)
                {
                    request.Params.Add(parameter);
                }

                //Attempt to send the request
                try
                {
                    response = request.Send2(_url, request.ToString());

                    //Check for general errors only. Specific errors will be handled
                    //by the calling method.
                    if (response.Value != null && response.IsFault)
                    {
                        //TODO : sort out error reporting
                    }

                }
                catch (WebException ex)
                {
                    //TODO : sort out error reporting
                }
            

            return response;
        }


        internal XmlRpcResponse CallConfluenceMethodWithoutLogin(string methodName,  params object[] parameters)
        {           
            XmlRpcResponse response = null;
            XmlRpcRequest request = new XmlRpcRequest();

            //Set the method for the request.
            request.MethodName = methodName;

            //Add the Parameters to the request
            request.Params.Clear();
            foreach (object parameter in parameters)
            {
                request.Params.Add(parameter);
            }

            //Attempt to send the request
            try
            {
                response = request.Send2(_url, request.ToString());

                //Check for general errors only. Specific errors will be handled
                //by the calling method.
                if (response.Value != null && response.IsFault)
                {
                   //TODO : sort out error reporting
                }

            }
            catch (WebException ex)
            {
                //TODO : sort out error reporting
            }


            return response;
        }



        internal XmlRpcResponse SendLoginRequest()
        {          

            //We cannot use CallConfluenceMethod, since it calls this method.

            XmlRpcResponse response = null;

            //Build the request manually
            XmlRpcRequest request = new XmlRpcRequest();

            //Set the method for the request.
            request.MethodName = "confluence1.login";

            //Add the Parameters to the request
            request.Params.Clear();
            request.Params.Add(_username);
            request.Params.Add(_password);

            //Send the request
            try
            {
                //String login(String username, String password) - login a user. Returns a String authentication token to be passed as authentication to all other remote calls. It's not bulletproof auth, but it will do for now. Must be called before any other method in a 'remote conversation'. From 1.3 onwards, you can supply an empty string as the token to be treated as being the anonymous user.
                response = request.Send2(_url, request.ToString());

                if (!response.IsFault && response.Value != null)
                {
                    //Login succeeded.
                }
                else
                {
                    //TODO : sort out error reporting
                }
            }
            catch (WebException ex)
            {
                //TODO : sort out error reporting
            }

            return response;

        }
        #endregion


        internal List<T> CallVectorCFTypeConfluenceMethod<T>(string methodname, string errorCode, params object[] parameters) where T : WikiType, new()
        {
            return CallVectorCFTypeConfluenceMethod<T>("confluence1.", methodname, errorCode, parameters);
        }

        // This method is a copy of the above method, but takes in a prefix command in case we are using an extended RPC plugin
        internal List<T> CallVectorCFTypeConfluenceMethod<T>(string prefix, string methodname, string errorCode, params object[] parameters) where T : WikiType, new()
        {
         
            XmlRpcResponse response = CallConfluenceMethod(prefix + methodname, parameters);

            List<T> cfObjects = new List<T>();

            
                //Determine if the call succeeded
                if (!response.IsFault && response.Value != null)
                {
                    ArrayList labelArrayList = (ArrayList)response.Value;

                    foreach (Object cftypeObject in labelArrayList)
                    {
                        T cfobject = new T();
                        cfobject.PopulateFromHashtable((Hashtable)cftypeObject);
                        cfObjects.Add(cfobject);
                    }
                }
                else
                {
                    //TODO : sort out error reporting
                }
           

            return cfObjects;
        }


        // This method is a copy of the above method, but takes in a prefix command in case we are using an extended RPC plugin
        internal List<T> CallVectorCFTypeConfluenceMethodWithoutLogin<T>(string prefix, string methodname, string errorCode, params object[] parameters) where T : WikiType, new()
        {
            
            XmlRpcResponse response = CallConfluenceMethodWithoutLogin(prefix + methodname, parameters);

            List<T> cfObjects = new List<T>();

           
                //Determine if the call succeeded
                if (!response.IsFault && response.Value != null)
                {
                    ArrayList labelArrayList = (ArrayList)response.Value;

                    foreach (Object cftypeObject in labelArrayList)
                    {
                        T cfobject = new T();
                        cfobject.PopulateFromHashtable((Hashtable)cftypeObject);
                        cfObjects.Add(cfobject);
                    }
                }
                else
                {
                    //TODO : sort out error reporting
                }
           

            return cfObjects;
        }


        // This method is a copy of the above method, but takes in a prefix command in case we are using an extended RPC plugin
        internal string CallVectorCFTypeConfluenceMethodWithoutLogin(string prefix, string methodname, string errorCode, params object[] parameters) 
        {

            XmlRpcResponse response = CallConfluenceMethodWithoutLogin(prefix + methodname, parameters);           


            //Determine if the call succeeded
            if (!response.IsFault && response.Value != null)
            {
               return (string)response.Value;
                
            }
            else
            {
                return "";
            }            
        }


        public static List<T> ConvertToList<T>(ArrayList list)
        {
            List<T> ret = new List<T>();
            foreach (T item in list)
            {
                ret.Add(item);
            }
            return ret;
        }

        /// <summary>
        /// This method is used to call a confluence RPC method, and return a list of primitively typed objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodname"></param>
        /// <param name="errorCode"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        internal List<string> CallStringVectorConfluenceMethod(string methodname, string errorCode, params object[] parameters)
        {
           
            //Vector getMostPopularLabels(String token, int maxCount)
            XmlRpcResponse response = CallConfluenceMethod("confluence1." + methodname, parameters);

            List<string> list = new List<string>();

           
                //Determine if the call succeeded
                if (!response.IsFault && response.Value != null)
                {
                    list = ConvertToList<string>((ArrayList)response.Value);
                }
                else
                {
                    //TODO : sort out error reporting
                }
           

            return list;
        }


        internal T CallCFTypeConfluenceMethod<T>(string methodname, string errorCode, params object[] parameters) where T : WikiType, new()
        {
           

            //Vector getMostPopularLabels(String token, int maxCount)
            XmlRpcResponse response = CallConfluenceMethod("confluence1." + methodname, parameters);

            T cfObject = new T();

            //Determine if the call succeeded
            if (!response.IsFault && response.Value != null)
            {
                cfObject.PopulateFromHashtable((Hashtable) response.Value);
            }
            else
            {
                //TODO : sort out error reporting
            }

            return cfObject;
        }

        /// <summary>
        /// Calls the given confluence method, and casts the result into type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodname"></param>
        /// <param name="errorCode"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        internal bool CallBoolConfluenceMethod(string methodname, string errorCode, params object[] parameters)
        {
           
            XmlRpcResponse response = CallConfluenceMethod("confluence1." + methodname, parameters);

            bool result = false;

                //Determine if the call succeeded
                if (!response.IsFault && response.Value != null)
                {
                    result = (bool)response.Value;
                }
                else
                {
                    //TODO : sort out error reporting
                }
           

            return result;
        }


        internal void CallVoidConfluenceMethod(string methodname, string errorCode, params object[] parameters)
        {            
            XmlRpcResponse response = CallConfluenceMethod("confluence1." + methodname, parameters);

            
                //Determine if the call succeeded
                if (!response.IsFault && response.Value != null)
                {
                    //No return value, so nothing to do.
                }
                else
                {
                    //TODO : sort out error reporting
                }
            
        }
        
        /// <summary>
        /// This function is provided for testing purposes only, and invalidates the stored authorisation token.
        /// </summary>
        internal void InvalidateAuthToken()
        {
            authToken = "invalidtoken";
        }

        internal string GetAuthToken()
        {
            return GetAuthToken(true);
        }

        internal string GetNewAuthToken()
        {
            return GetAuthToken(false);
        }

        internal string GetAuthToken(bool useCachedToken)
        {
            //This method returns an authentication token. It will cache any retrieved token, and only return a fresh one
            //if the cache is invalid, or if useCachedToken is set to false

            if (useCachedToken && !authToken.Equals(string.Empty))
            {
                //it is ok to return a cached token and the cached authToken is valid
                return authToken;
            }

            //We cannot use the authorisation cache, so attempt to get a new authToken
            
            //Firstly Reset the authToken
            authToken = "";

            //Get the new authToken
            XmlRpcResponse response = SendLoginRequest();
           
           // Succeeded in obtaining a token
           authToken = (string)response.Value;
           

            return authToken;
        }


        /// <summary>
        /// Takes an XmlRpcResponse object from a call to a Boolean method in the Confluence API and
        /// returns true if the method returned true.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        internal bool OperationSuccessful(XmlRpcResponse response)
        {
            return !response.IsFault && response.Value != null && (Boolean)response.Value;
        }

    }
}
