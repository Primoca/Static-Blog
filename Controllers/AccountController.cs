using Microsoft.Web.WebPages.OAuth;
using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Static_Blog.Controllers
{
	public class AccountController : Controller
	{
		public ActionResult Login(string returnUrl)
		{
			ViewBag.ReturnUrl = returnUrl;
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public void Login(string provider, string returnUrl)
		{
			AuthConfig.GoogleClient.RequestAuthentication(HttpContext, new Uri(Request.Url.GetLeftPart(UriPartial.Authority) + Url.Action("AuthenticationCallback", "Account")));
		}

		public ActionResult AuthenticationCallback(string returnUrl)
		{
			var result = AuthConfig.GoogleClient.VerifyAuthentication(HttpContext, new Uri(Request.Url.GetLeftPart(UriPartial.Authority) + Url.Action("AuthenticationCallback", "Account")));
			if (result.IsSuccessful)
			{
				string email = "";
				switch (result.Provider)
				{
					case "google":
						email = result.ExtraData["email"];
						break;
				}
				if (email == CloudConfigurationManager.GetSetting("adminEmail"))
				{
					FormsAuthentication.SetAuthCookie(email, false);

					if (Url.IsLocalUrl(returnUrl))
					{
						return Redirect(returnUrl);
					}
					else
					{
						return RedirectToAction("Posts", "blog");
					}
				}
			}
			throw new Exception("Login Error - " + (result.Error != null ? result.Error.Message : ""));
		}

		public ActionResult Logout()
		{
			FormsAuthentication.SignOut();
			return RedirectToAction("Login");
		}
	}
}
