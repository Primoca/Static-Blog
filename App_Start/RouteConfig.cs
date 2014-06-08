using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Static_Blog
{
	public class RouteConfig
	{
		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute(
				name: "Account",
				url: "account/{action}/{id}",
				defaults: new { controller = "Account", action = "Login", id = UrlParameter.Optional }
			);

			routes.MapRoute(
				name: "Posts",
				url: "posts/{year}/{month}/{id}",
				defaults: new { controller = "Blog", action = "Posts", year = UrlParameter.Optional, month = UrlParameter.Optional, id = UrlParameter.Optional }
			);

			routes.MapRoute(
				name: "Edit",
				url: "edit/{year}/{month}/{id}",
				defaults: new { controller = "Blog", action = "Edit", id = UrlParameter.Optional }
			);

			routes.MapRoute(
				name: "Asset",
				url: "assets/{year}/{id}",
				defaults: new { controller = "Blog", action = "Assets", id = UrlParameter.Optional }
			);
			
			routes.MapRoute(
				name: "Blog",
				url: "{action}/{id}",
				defaults: new { controller = "Blog", action = "Index", id = UrlParameter.Optional }
			);
		}
	}
}