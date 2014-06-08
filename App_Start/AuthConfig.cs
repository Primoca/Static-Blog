using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.WebPages.OAuth;
using System.Net;
using System.IO;
using System.Web;
using System.Configuration;

namespace Static_Blog
{
	public static class AuthConfig
	{
		public static void RegisterAuth()
		{
			OAuthWebSecurity.RegisterGoogleClient();
		}
	}
}
