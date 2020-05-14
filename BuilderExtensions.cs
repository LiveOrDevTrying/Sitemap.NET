using Microsoft.AspNetCore.Builder;
using SiteMaps.NET.Models;

namespace SiteMaps.NET
{
    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseSitemap(this IApplicationBuilder app,
            bool parseControllers = true, bool isSSL = true, SiteMapNode[] siteMapNodes = null, SiteMapNodeDetail[] detailNodes = null, string basePath = "")
        {
            if (siteMapNodes == null)
            {
                siteMapNodes = new SiteMapNode[0];
            }

            return app.UseMiddleware<SiteMapsMiddleware>(parseControllers, isSSL, siteMapNodes, detailNodes, basePath);
        }

        public static IApplicationBuilder UseRobots(this IApplicationBuilder app, RobotRule[] robotRules = null, bool isSSL = true)
        {
            if (robotRules == null)
            {
                robotRules = new RobotRule[0];
            }

            return app.UseMiddleware<RobotsMiddleware>(robotRules, isSSL);
        }
    }
}
