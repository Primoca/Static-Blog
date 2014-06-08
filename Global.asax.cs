using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Static_Blog
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();

			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
			AuthConfig.RegisterAuth();
		}

		protected void Application_BeginRequest()
		{
			
		}
	}

	public class CustomAuthorizeAttribute : System.Web.Mvc.AuthorizeAttribute
	{
		public override void OnAuthorization(AuthorizationContext filterContext)
		{
			var header = filterContext.RequestContext.HttpContext.Request.Headers["Authorization"];
			if (!string.IsNullOrEmpty(header) && header == "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["BasicPassword"] + ":")))
			{
				filterContext.HttpContext.User = new CustomPrincipal { Identity = new CustomIdentity { IsAuthenticated = true, Name = "Basic" } };
			}
			base.OnAuthorization(filterContext);
		}
		protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
		{
			filterContext.HttpContext.Response.Headers.Add("WWW-Authenticate", "Basic");
			base.HandleUnauthorizedRequest(filterContext);
		}
	}

	public class CustomPrincipal : IPrincipal
	{
		public IIdentity Identity { get; set; }

		public bool IsInRole(string role)
		{
			return false;
		}
	}

	public class CustomIdentity : IIdentity
	{
		public string AuthenticationType
		{
			get { return "Basic"; }
		}

		public bool IsAuthenticated { get; set; }

		public string Name { get; set; }
	}
}