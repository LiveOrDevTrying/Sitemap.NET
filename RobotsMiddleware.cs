using Microsoft.AspNetCore.Http;
using SiteMaps.NET.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SiteMaps.NET
{
    public class RobotsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RobotRule[] _robotRules;
        private readonly bool _useSSL;

        public RobotsMiddleware(RequestDelegate next, bool useSSL)
        {
            _next = next;
            _useSSL = useSSL;
        }

        public RobotsMiddleware(RequestDelegate next, RobotRule[] robotRules, bool useSSL)
        {
            _next = next;
            _robotRules = robotRules;
            _useSSL = useSSL;
        }

        public virtual async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Value.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase))
            {
                var stream = context.Response.Body;
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html";

                var baseUrl = string.Format("{0}://{1}{2}", _useSSL ? "https" : "http", context.Request.Host, context.Request.PathBase);

                var sb = new StringBuilder();

                if (_robotRules != null)
                {
                    foreach (var rule in _robotRules)
                    {
                        foreach (var item in rule.UserAgents)
                        {
                            sb.AppendLine($"User-Agent: {item}");
                        }

                        foreach (var item in rule.Disallow)
                        {
                            sb.AppendLine($"Disallow: {item}");
                        }

                        sb.AppendLine();
                    }
                }
                else
                {
                    // Make default Robots
                    sb.AppendLine($"User-Agent: *");
                    sb.AppendLine($"Allow: /");
                }

                sb.AppendLine(string.Empty);
                sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

                using (var memoryStream = new MemoryStream())
                {
                    var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                    memoryStream.Write(bytes, 0, bytes.Length);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await memoryStream.CopyToAsync(stream, bytes.Length);
                    return;
                }
            }

            await _next.Invoke(context);
        }
    }
}