using System.Web;
using System.Web.Optimization;

namespace Static_Blog
{
	public class BundleConfig
	{
		// For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
		public static void RegisterBundles(BundleCollection bundles)
		{
			bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/site.css"));
		}
		
		//By default the bundle version is put in the query string which doesn't work for generating static files.
		//So we rewrite the url with the version hash in a file name and handle the new path on the controller.
		public static string RewriteStyleBundleUrl(string url)
		{
			return url.Replace("/Content", "").Replace("?v=", "/") + ".css";
		}
	}
}