
using System;
using System.Xml;
using System.Data;
using System.Collections;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using System.Reflection;
using System.IO;
using System.Net;
using Microsoft.CSharp;
using System.Net.Http;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Data
{
	/// <summary>
	/// WebServiceWsdl handles generation and caching of Assemblies containing WSDL proxies
	///   It also will invoke proxies with the proper arguments.  These arguments must be 
	///   provided as a WebServiceParameter.
	/// </summary>
	public class WebServiceWsdl
	{
		// Cache the compiled assemblies
		const string _Namespace = "fyireporting.ws";
		static Hashtable _cache = Hashtable.Synchronized(new Hashtable());	
		string _url;					// url for this assembly
		Assembly _WsdlAssembly;			// Assembly ready for invokation

		static internal WebServiceWsdl GetWebServiceWsdl(string url)
		{
			WebServiceWsdl w = _cache[url] as WebServiceWsdl;
			if (w != null)
				return w;

			return new WebServiceWsdl(url);
		}

		static public void ClearCache()
		{
			_cache.Clear();
		}

		public MethodInfo GetMethodInfo(string service, string operation)
		{
			// Create an instance of the service object proxy   
			object o = _WsdlAssembly.CreateInstance(_Namespace + "." + service, false);
			if (o == null)
				throw new Exception(string.Format("Unable to create instance of service '{0}'.", service));

			// Get information about the method
			MethodInfo mi = o.GetType().GetMethod(operation);
			if (mi == null)
				throw new Exception(string.Format("Unable to find operation '{0}' in service '{1}'.", operation, service));

			return mi;
		}

		// Invoke the operation for the requested service
		public object Invoke(string service, string operation, DataParameterCollection dpc, int timeout)
		{
			// Create an instance of the service object proxy
			object o = _WsdlAssembly.CreateInstance(_Namespace + "." + service, false);
			if (o == null)
				throw new Exception(string.Format("Unable to create instance of service '{0}'.", service));

			// Get information about the method
			MethodInfo mi = o.GetType().GetMethod(operation);
			if (mi == null)
				throw new Exception(string.Format("Unable to find operation '{0}' in service '{1}'.", operation, service));

			// Go thru the parameters building up an object array with the proper parameters
			ParameterInfo[] pis = mi.GetParameters();
			object[] args = new object[pis.Length];
			int ai=0;
			foreach (ParameterInfo pi in pis)
			{
				BaseDataParameter dp = dpc[pi.Name] as BaseDataParameter;
				if (dp == null)		// retry with '@' in front!
					dp = dpc["@"+pi.Name] as BaseDataParameter;
				if (dp == null || dp.Value == null)
					args[ai] = null;
				else if (pi.ParameterType == dp.Value.GetType())
					args[ai] = dp.Value;
				else	// we need to do conversion
					args[ai] = Convert.ChangeType(dp.Value, pi.ParameterType);
				ai++;
			}
            throw new NotImplementedException("Some classes are not available on .NET STANDARD");
		}

		// constructor
		private WebServiceWsdl(string url)
		{
			_url = url;						
			_WsdlAssembly = GetAssembly();
			_cache.Add(url, this);
		}

		private Assembly GetAssembly()
		{
            throw new NotImplementedException("Some classes are missing on .NET STANDARD");
		}

        
        async Task<Stream> GetStream()
        {
            string fname = _url;
            Stream strm = null;

            if (fname.StartsWith("http:") || fname.StartsWith("file:") || fname.StartsWith("https:"))
            {
                using (HttpClient client = new HttpClient())
                {
                    client.AddMajorsilenceReportingUserAgent();
                    HttpResponseMessage response = await client.GetAsync(fname);
                    if (response.IsSuccessStatusCode)
                    {
                        strm = await response.Content.ReadAsStreamAsync();
                    }
                    else
                    {
                        throw new Exception($"Failed to get response from {fname}. Status code: {response.StatusCode}");
                    }
                }
            }
            else
            {
                strm = new FileStream(fname, System.IO.FileMode.Open, FileAccess.Read);
            }

            return strm;
        }

	}
}
