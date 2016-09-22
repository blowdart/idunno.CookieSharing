using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(idunno.CookieSharing.FullFramework.Startup))]
namespace idunno.CookieSharing.FullFramework
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
