using System.IO;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace idunno.CookieSharing.Core
{
    public class Startup
    {
        IHostingEnvironment _hostingEnvironment;

        public Startup(IHostingEnvironment hostingEnvironment)
        {
            // We need the hosting environment to figure out where to store
            // the keys for this demo. You wouldn't do this in production code,
            // you'd add a configuration point or a hard coded directory.

            _hostingEnvironment = hostingEnvironment;
        }

        public void ConfigureServices(
            IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
            string contentRoot = env.ContentRootPath;
            string keyRingPath = Path.GetFullPath(Path.Combine(contentRoot, "..", "idunno.KeyRing"));

            // Now we create a data protector, with a fixed purpose and sub-purpose used in key derivation.
            var protectionProvider = DataProtectionProvider.Create(new DirectoryInfo(keyRingPath));
            var dataProtector = protectionProvider.CreateProtector(
                "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
                "Cookie",
                "v2");
            // And finally create a new auth ticket formatter using the data protector.
            var ticketFormat = new TicketDataFormat(dataProtector);

            // Now configure the cookie options to have the same cookie name, and use
            // the common format.
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationScheme = "Cookie",
                LoginPath = new PathString("/Account/Login/"),
                AccessDeniedPath = new PathString("/Account/Forbidden/"),
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                CookieName = ".AspNet.SharedCookie",
                TicketDataFormat = ticketFormat
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                     name: "default",
                     template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
