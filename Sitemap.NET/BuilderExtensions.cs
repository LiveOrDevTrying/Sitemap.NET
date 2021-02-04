using Microsoft.AspNetCore.Builder;
using SiteMap.NET.Models;

namespace SiteMap.NET
{
    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseSitemap(this IApplicationBuilder app,
            bool parseControllers = true, bool isSSL = true, SitemapNode[] siteMapNodes = null, SitemapNodeDetail[] detailNodes = null, string basePath = "")
        {
            if (siteMapNodes == null)
            {
                siteMapNodes = new SitemapNode[0];
            }

            if (detailNodes == null)
            {
                detailNodes = new SitemapNodeDetail[0];
            }

            return app.UseMiddleware<SitemapMiddleware>(parseControllers, isSSL, siteMapNodes, detailNodes, basePath);
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
