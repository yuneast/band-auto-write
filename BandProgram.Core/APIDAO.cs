using System;
using System.IO;
using System.Net;

namespace BandProgram
{
	internal class APIDAO
	{
		private Util util = Util.getInstance();

		public APIDAO()
		{
		}

		public string getAuthorization(string clientId, string clientSecret)
		{
			string str = string.Concat(clientId, ":", clientSecret);
			return this.util.Base64Encoding(str, null);
		}

		public string getRequestResult(string requestUrl)
		{
			string str;
			string empty = string.Empty;
			try
			{
				WebResponse response = WebRequest.Create(requestUrl).GetResponse();
				Stream responseStream = response.GetResponseStream();
				StreamReader streamReader = new StreamReader(responseStream);
				empty = streamReader.ReadToEnd();
				streamReader.Close();
				responseStream.Close();
				response.Close();
				str = empty;
			}
			catch (Exception exception)
			{
				str = null;
			}
			return str;
		}

		public string getToken(string authorization_code, string authorization)
		{
			string str;
			string empty = string.Empty;
			try
			{
				WebRequest webRequest = WebRequest.Create(string.Concat("https://auth.band.us/oauth2/token?grant_type=authorization_code&code=", authorization_code));
				webRequest.Headers.Add("Authorization", authorization);
				WebResponse response = webRequest.GetResponse();
				Stream responseStream = response.GetResponseStream();
				StreamReader streamReader = new StreamReader(responseStream);
				empty = streamReader.ReadToEnd();
				streamReader.Close();
				responseStream.Close();
				response.Close();
				str = empty;
			}
			catch (Exception exception)
			{
				str = null;
			}
			return str;
		}
	}
}