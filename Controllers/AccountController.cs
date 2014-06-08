using Microsoft.Web.WebPages.OAuth;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
			OAuthWebSecurity.RequestAuthentication(provider, Url.Action("AuthenticationCallback", "Account", new { ReturnUrl = returnUrl }));
		}

		public ActionResult AuthenticationCallback(string returnUrl)
		{
			var result = OAuthWebSecurity.VerifyAuthentication(Url.Action("AuthenticationCallback", "Account", new { ReturnUrl = returnUrl }));
			if (result.IsSuccessful)
			{
				string email = "";
				switch (result.Provider)
				{
					case "google":
						email = result.ExtraData["email"];
						break;
				}
				if (email == ConfigurationManager.AppSettings["adminEmail"])
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
