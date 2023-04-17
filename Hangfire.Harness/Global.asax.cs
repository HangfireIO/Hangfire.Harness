using System.Configuration;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Serilog;
using Serilog.Exceptions;

namespace Hangfire.Harness
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls12;

            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("App", ConfigurationManager.AppSettings["SeqAppName"])
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithExceptionDetails()
                .WriteTo.Seq("https://logs.hangfire.io", apiKey: ConfigurationManager.AppSettings["SeqApiKey"])
                .MinimumLevel.Verbose()
                .CreateLogger();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_End()
        {
            Log.CloseAndFlush();
        }
    }
}
