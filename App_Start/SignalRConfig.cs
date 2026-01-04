using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;

[assembly: OwinStartup(typeof(ZKTecoApi.App_Start.Startup))]

namespace ZKTecoApi.App_Start
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // CORS'u etkinleştir
            app.UseCors(CorsOptions.AllowAll);

            // SignalR konfigürasyonu
            var hubConfiguration = new HubConfiguration
            {
                EnableDetailedErrors = true,
                EnableJavaScriptProxies = true
            };

            // SignalR endpoint'ini map et
            app.MapSignalR("/signalr", hubConfiguration);
        }
    }
}
