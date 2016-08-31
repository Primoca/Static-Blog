using Microsoft.WindowsAzure;
using Static_Blog.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Xml;

namespace Static_Blog.Controllers
{
    public class BlogController : Controller
    {
		[CustomAuthorize]
        public ActionResult Index()
        {
			ViewBag.FullPostCount = 1;
			return View(Post.Get(null, null).Take(5).ToArray());
        }

		[CustomAuthorize]
		public ActionResult Posts(int? year, int? month, string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				ViewBag.TopH1 = (month.HasValue ? new DateTime(2000, month.Value, 1).ToString("MMMM") : "") + " " + (year.HasValue ? year.ToString() : "");
				return View("Index", Post.Get(year, month).Take(10).ToArray());
			}
			return View(Post.Get(year + "/" + month.Value.ToString("00") + "/" + id));
		}

		[Authorize]
		public ActionResult Edit(int? year, int? month, string id)
		{
			if (!year.HasValue)
			{
				return View(new Post { Date = DateTime.Now, Author = "Alex Thompson" });
			}
			return View(Post.Get(year + "/" + month.Value.ToString("00") + "/" + id));
		}

		[HttpPost]
		[ValidateInput(false)]
		[Authorize]
		public ActionResult Edit(Post post, string year, string month, string id)
		{
			var urlPostId = year + "/" + month + "/" + id;
			Post.Save(post);
			if (post.FullId != urlPostId)
			{
				return Redirect("/edit/" + post.FullId);
			}
			return View(post);
		}

        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public async Task<ActionResult> Deploy()
        {
            var c = new Crawler();
            await c.Crawl(new Uri(Request.Url.GetLeftPart(UriPartial.Authority)), CloudConfigurationManager.GetSetting("BasicPassword"), "");
            return new ContentResult { Content = string.Join("<br>", c.CrawledUrls) + string.Join("<br>", c.log.entires) };
        }

        [CustomAuthorize]
		public ActionResult Tags(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return View(Post.GetTags());
			}
			ViewBag.TopH1 = "Tags / " + id;
			return View("Index", Post.GetTag(id));
		}

		[CustomAuthorize]
		public ActionResult Rss()
		{
			return View(Post.Get(null, null).Take(10));
		}

		[CustomAuthorize]
		public ActionResult Css()
		{
			var response = BundleTable.Bundles.First().GenerateBundleResponse(new BundleContext(Request.RequestContext.HttpContext, BundleTable.Bundles, "~/Content/css"));
			return new ContentResult { Content = response.Content, ContentType = response.ContentType };
		}

		[CustomAuthorize]
		public ActionResult Assets(int? year, string id)
		{
			if (!string.IsNullOrEmpty(id))
			{
				return new FileStreamResult(Post.GetAsset(year + "/" + id), MimeMapping.GetMimeMapping(id));
			}
			return View(Post.GetAssets());
		}

		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public ActionResult Assets(HttpPostedFileBase file)
		{
			Post.SaveAsset(file.FileName, file.InputStream);
			return View(Post.GetAssets());
		}
    }
}
