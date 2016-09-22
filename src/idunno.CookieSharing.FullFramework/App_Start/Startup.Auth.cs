using Microsoft.AspNetCore.DataProtection;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Interop;
using Owin;
using System;
using System.IO;

namespace idunno.CookieSharing.FullFramework
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            // We need to configure data protection to use a specific key directory
            // we can share between applications.
            //
            // We also need a common protector purpose, as different purposes are
            // automatically isolated from one another.
            //
            // Finally we need to wire up a common ticket formatter.

            // Normally you'd just have a hard coded, or configuration based path,
            // but for this demo we're going to share a directory in the solution directory,
            // so we have to do some jiggery-pokery to figure it out.
            //
            // Let's get ugly to get the current runtime directory. OWIN doesn't give us a
            // way to figure this out, so we're resorting to AppDomain.
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string keyRingPath = Path.GetFullPath(Path.Combine(baseDirectory, "..", "idunno.KeyRing"));

            // Now we create a data protector, with a fixed purpose and sub-purpose used in key derivation.
            var protectionProvider = DataProtectionProvider.Create(new DirectoryInfo(keyRingPath));
            var dataProtector = protectionProvider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", authenticationScheme, "v2");
            // And finally create a new auth ticket formatter using the data protector.
            var ticketFormat = new AspNetTicketDataFormat(new DataProtectorShim(dataProtector));

            // Now configure the cookie options to have the same cookie name, and use
            // the common format.
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Cookie",
                AuthenticationMode = Microsoft.Owin.Security.AuthenticationMode.Active,
                LoginPath = new PathString("/Account/Login"),
                CookieName = ".AspNet.SharedCookie",
                TicketDataFormat = ticketFormat
            });
        }
    }
}