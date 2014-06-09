using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.WebPages.OAuth;
using System.Net;
using System.IO;
using System.Web;
using System.Configuration;
using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.Messaging;
using System.Collections.Specialized;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure;

namespace Static_Blog
{
	public static class AuthConfig
	{
		public static GoogleOAuth2Client GoogleClient;

		public static void RegisterAuth()
		{
			GoogleClient = new GoogleOAuth2Client(CloudConfigurationManager.GetSetting("GoogleClientId"), CloudConfigurationManager.GetSetting("GoogleClientSecret"));
		}
	}

	public class GoogleOAuth2Client : OAuth2Client
	{
		string _clientId;
		string _secret;

		public GoogleOAuth2Client(string clientId, string secret)
			: base("google")
		{
			_clientId = clientId;
			_secret = secret;
		}
		protected override Uri GetServiceLoginUrl(Uri returnUrl)
		{
			UriBuilder uriBuilder = new UriBuilder("https://accounts.google.com/o/oauth2/auth");
			uriBuilder.AppendQueryArgument("client_id", _clientId);
			uriBuilder.AppendQueryArgument("redirect_uri", returnUrl.AbsoluteUri);
			uriBuilder.AppendQueryArgument("scope", "openid email");
			uriBuilder.AppendQueryArgument("response_type", "code");
			uriBuilder.AppendQueryArgument("openid.realm", returnUrl.Scheme + "://" + returnUrl.Host.ToLower() + (returnUrl.Port > 443 ? (":" + returnUrl.Port) : ""));
			return uriBuilder.Uri;
		}

		protected override IDictionary<string, string> GetUserData(string accessToken)
		{
			WebRequest webRequest = WebRequest.Create("https://www.googleapis.com/plus/v1/people/me/openIdConnect?access_token=" + accessToken);
			using (WebResponse response = webRequest.GetResponse())
			{
				using (Stream responseStream = response.GetResponseStream())
				{
					using (var sr = new StreamReader(responseStream))
					{
						var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());
						d.Add("id", d["sub"]);
						return d;
					}
				}
			}
		}

		protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
		{
			UriBuilder uriBuilder = new UriBuilder("http://placeholder");
			uriBuilder.AppendQueryArgument("client_id", _clientId);
			uriBuilder.AppendQueryArgument("redirect_uri", returnUrl.AbsoluteUri);
			uriBuilder.AppendQueryArgument("client_secret", _secret);
			uriBuilder.AppendQueryArgument("code", authorizationCode);
			uriBuilder.AppendQueryArgument("grant_type", "authorization_code");
			var query = uriBuilder.Query.Replace("?", "");

			WebRequest webRequest = WebRequest.Create("https://accounts.google.com/o/oauth2/token");
			webRequest.ContentType = "application/x-www-form-urlencoded";
			webRequest.ContentLength = (long)query.Length;
			webRequest.Method = "POST";
			using (Stream requestStream = webRequest.GetRequestStream())
			{
				StreamWriter streamWriter = new StreamWriter(requestStream);
				streamWriter.Write(query);
				streamWriter.Flush();
			}
			HttpWebResponse httpWebResponse = (HttpWebResponse)webRequest.GetResponse();
			if (httpWebResponse.StatusCode == HttpStatusCode.OK)
			{
				using (Stream responseStream = httpWebResponse.GetResponseStream())
				{
					using (var sr = new StreamReader(responseStream))
					{
						return JsonConvert.DeserializeObject<JObject>(sr.ReadToEnd())["access_token"].ToString();
					}
				}
			}
			return null;
		}
	}
}
