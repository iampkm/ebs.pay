using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CQSS.Pay.Web.Startup))]
namespace CQSS.Pay.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
