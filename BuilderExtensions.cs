using Microsoft.AspNetCore.Builder;
using SiteMaps.NET.Models;

namespace SiteMaps.NET
{
    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseSitemap(this IApplicationBuilder app,
            bool parseControllers, bool useSSL)
        {
            return app.UseMiddleware<SiteMapsMiddleware>(parseControllers, useSSL);
        }

        public static IApplicationBuilder UseRobots(this IApplicationBuilder app, bool useSSL)
        {
            return app.UseMiddleware<RobotsMiddleware>(useSSL);
        }

        public static IApplicationBuilder UseRobots(this IApplicationBuilder app, bool useSSL, RobotRule[] robotRules)
        {
            return app.UseMiddleware<RobotsMiddleware>(robotRules, useSSL);
        }
    }
}
