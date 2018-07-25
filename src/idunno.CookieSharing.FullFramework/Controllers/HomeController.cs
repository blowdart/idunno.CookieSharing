using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace idunno.CookieSharing.FullFramework.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }


        [Authorize]
        public ActionResult About()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                const string Issuer = "urn:net-desktop";
                var identity = new ClaimsIdentity(
                    new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, model.Email, ClaimValueTypes.String, Issuer)
                    },
                    "Cookie",
                    ClaimTypes.Email,
                    ClaimTypes.Role
                    );
                var principal = new ClaimsPrincipal(identity);

                var ctx = HttpContext.GetOwinContext();
                var props = new AuthenticationProperties()
                {
                    AllowRefresh = true,
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(20)
                };
                ctx.Authentication.SignIn(props, identity);

                return RedirectToAction("Index");
            }
            else
            {
                return View("Index", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            var ctx = Request.GetOwinContext();
            ctx.Authentication.SignOut("Cookie");
            return RedirectToAction("Index");
        }
    }
}