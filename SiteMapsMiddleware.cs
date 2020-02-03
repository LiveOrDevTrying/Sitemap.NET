using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SiteMaps.NET.Attributes;
using SiteMaps.NET.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SiteMaps.NET
{
    public class SiteMapsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly bool _parseControllers;

        public SiteMapsMiddleware(RequestDelegate next, bool parseControllers)
        {
            _next = next;
            _parseControllers = parseControllers;
        }

        public virtual async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Value.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase))
            {
                var stream = context.Response.Body;
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/xml";

                var baseUrl = string.Format("{0}://{1}{2}", context.Request.Scheme, context.Request.Host, context.Request.PathBase);

                XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                var root = new XElement(xmlns + "urlset");
                var urlElement = new XElement(xmlns + "url",
                    new XElement(xmlns + "loc", Uri.EscapeUriString($"{baseUrl}")), new XElement(xmlns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz")));
                root.Add(urlElement);

                var siteMapNodes = await GetSiteMapNodes();

                if (siteMapNodes.Length > 0)
                {
                    foreach (var siteMapNode in siteMapNodes)
                    {
                        if (!root.Elements()
                                .Nodes()
                                .Select(s => s.ToString())
                                .Contains(Uri.EscapeUriString(siteMapNode.Url)))
                        {
                            urlElement = new XElement(
                                xmlns + "url",
                                new XElement(xmlns + "loc", Uri.EscapeUriString(siteMapNode.Url)),
                                siteMapNode.LastModified == null ? null : new XElement(
                                    xmlns + "lastmod",
                                    siteMapNode.LastModified.Value.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz")),
                                siteMapNode.Frequency == null ? null : new XElement(
                                    xmlns + "changefreq",
                                    siteMapNode.Frequency.Value.ToString().ToLowerInvariant()),
                                siteMapNode.Priority == null ? null : new XElement(
                                    xmlns + "priority",
                                    siteMapNode.Priority.Value.ToString("F1", CultureInfo.InvariantCulture)));
                            root.Add(urlElement);
                        }
                    }
                }

                if (_parseControllers)
                {
                    var controllers = Assembly.GetEntryAssembly().GetTypes()
                        .Where(type => typeof(ControllerBase).IsAssignableFrom(type) || type.Name.EndsWith("controller"))
                        .ToList();

                    foreach (var controller in controllers)
                    {
                        var attribute = Attribute.GetCustomAttribute(controller, typeof(NoSiteMap));

                        if (attribute == null)
                        {
                            var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                .Where(method => typeof(Task<IActionResult>).IsAssignableFrom(method.ReturnType) ||
                                    typeof(IActionResult).IsAssignableFrom(method.ReturnType));

                            foreach (var method in methods)
                            {
                                // What happens when we have an Area?

                                attribute = Attribute.GetCustomAttribute(method, typeof(NoSiteMap));

                                var containsRecord = root.Elements()
                                    .Nodes()
                                    .Select(s => s.ToString())
                                    .Contains(Uri.EscapeUriString($"{baseUrl}/{controller.Name.ToLower().Replace("controller", "")}/{method.Name.ToLower()}"));

                                if (!containsRecord &&
                                    attribute == null)
                                {
                                    urlElement = new XElement(xmlns + "url",
                                        new XElement(xmlns + "loc", Uri.EscapeUriString($"{baseUrl}/{controller.Name.ToLower().Replace("controller", "")}/{method.Name.ToLower()}")),
                                            new XElement(xmlns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz")));
                                    root.Add(urlElement);
                                }

                                var details = await GetDetailRecordNodes(controller.Name, method.Name);

                                if (details.Length > 0)
                                {
                                    foreach (var detail in details)
                                    {
                                        urlElement = new XElement(
                                            xmlns + "url",
                                            new XElement(xmlns + "loc", Uri.EscapeUriString(detail.Url)),
                                            detail.LastModified == null ? null : new XElement(
                                                xmlns + "lastmod",
                                                detail.LastModified.Value.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz")),
                                            detail.Frequency == null ? null : new XElement(
                                                xmlns + "changefreq",
                                                detail.Frequency.Value.ToString().ToLowerInvariant()),
                                            detail.Priority == null ? null : new XElement(
                                                xmlns + "priority",
                                                detail.Priority.Value.ToString("F1", CultureInfo.InvariantCulture)));
                                        root.Add(urlElement);
                                    }
                                }
                            }
                        }
                    }
                }

                var document = new XDocument(root);

                using (var memoryStream = new MemoryStream())
                {
                    var bytes = Encoding.UTF8.GetBytes(document.ToString());
                    memoryStream.Write(bytes, 0, bytes.Length);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await memoryStream.CopyToAsync(stream, bytes.Length);
                    return;
                }
            }

            await _next.Invoke(context);
        }

        protected virtual Task<SiteMapNode[]> GetSiteMapNodes()
        {
            return Task.FromResult(new SiteMapNode[0]);
        }

        protected virtual Task<SiteMapNode[]> GetDetailRecordNodes(string controllerName, string methodName)
        {
            return Task.FromResult(new SiteMapNode[0]);
        }
    }
}