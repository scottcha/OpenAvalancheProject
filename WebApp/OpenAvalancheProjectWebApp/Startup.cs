using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(OpenAvalancheProjectWebApp.Startup))]
namespace OpenAvalancheProjectWebApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
