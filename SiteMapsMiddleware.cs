using Microsoft.AspNetCore.Authorization;
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
        private readonly bool _isSSL;
        private readonly SiteMapNode[] _siteMapNodes;
        private readonly SiteMapNodeDetail[] _detailNodes;
        private readonly string _basePath;

        public SiteMapsMiddleware(RequestDelegate next, bool parseControllers, bool isSSL, SiteMapNode[] siteMapNodes, SiteMapNodeDetail[] detailNodes, string basePath)
        {
            _next = next;
            _parseControllers = parseControllers;
            _isSSL = isSSL;
            _siteMapNodes = siteMapNodes;
            _detailNodes = detailNodes;
            _basePath = basePath;
        }

        public virtual async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Value.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase))
            {
                var stream = context.Response.Body;
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/xml";

                var baseUrl = string.IsNullOrWhiteSpace(_basePath)
                    ? string.Format("{0}://{1}{2}", _isSSL ? "https" : "http", context.Request.Host, context.Request.PathBase)
                    : string.Format("{0}://{1}", _isSSL ? "https" : "http", _basePath);
                XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                var root = new XElement(xmlns + "urlset");
                var urlElement = new XElement(xmlns + "url",
                    new XElement(xmlns + "loc", Uri.EscapeUriString($"{baseUrl}")), new XElement(xmlns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz")));
                root.Add(urlElement);

                if (_siteMapNodes.Length > 0)
                {
                    foreach (var siteMapNode in _siteMapNodes)
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
                        .Where(type => typeof(Controller).IsAssignableFrom(type) || type.Name.EndsWith("controller"))
                        .ToList();

                    foreach (var controller in controllers)
                    {
                        var attribute = Attribute.GetCustomAttribute(controller, typeof(NoSiteMap));
                        var isAuthRequired = Attribute.GetCustomAttribute(controller, typeof(AuthorizeAttribute)) != null;

                        if (attribute == null)
                        {
                            var controllerRoute = (RouteAttribute)Attribute.GetCustomAttribute(controller, typeof(RouteAttribute));

                            var routeName = string.Empty;

                            if (controllerRoute != null)
                            {
                                if (!string.IsNullOrWhiteSpace(controllerRoute.Name))
                                {
                                    routeName = $"{controllerRoute.Name}/";
                                }
                            }
                            else
                            {
                                routeName = $"{controller.Name.ToLower().Replace("controller", "")}/";
                            }

                            var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                .Where(method => typeof(Task<IActionResult>).IsAssignableFrom(method.ReturnType) ||
                                    typeof(IActionResult).IsAssignableFrom(method.ReturnType) ||
                                    typeof(Task<PartialViewResult>).IsAssignableFrom(method.ReturnType) ||
                                    typeof(PartialViewResult).IsAssignableFrom(method.ReturnType));

                            foreach (var method in methods)
                            {
                                // What happens when we have an Area?
                                attribute = Attribute.GetCustomAttribute(method, typeof(NoSiteMap));
                                var authorizeAttribute = Attribute.GetCustomAttribute(method, typeof(AuthorizeAttribute));
                                var allowAnonymouseAttribute = Attribute.GetCustomAttribute(method, typeof(AllowAnonymousAttribute));
                                var httpPostAttribute = Attribute.GetCustomAttribute(method, typeof(HttpPostAttribute));
                                var httpPutAttribute = Attribute.GetCustomAttribute(method, typeof(HttpPutAttribute));
                                var httpDeleteAttribute = Attribute.GetCustomAttribute(method, typeof(HttpDeleteAttribute));

                                if (authorizeAttribute == null &&
                                    (!isAuthRequired || allowAnonymouseAttribute != null) &&
                                    httpPostAttribute == null &&
                                    httpPutAttribute == null &&
                                    httpDeleteAttribute == null)
                                {
                                    var methodRoute = (RouteAttribute)Attribute.GetCustomAttribute(method, typeof(RouteAttribute));

                                    var methodRouteName = string.Empty;

                                    if (methodRoute != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(methodRoute.Template))
                                        {
                                            methodRouteName = $"{methodRoute.Template}";
                                        }
                                    }
                                    else
                                    {
                                        methodRouteName = method.Name.ToLower();
                                    }

                                    if (attribute == null)
                                    {
                                        var containsRecord = root.Elements()
                                            .Nodes()
                                            .Select(s => s.ToString())
                                            .Contains(Uri.EscapeUriString($"{baseUrl}/{routeName}{methodRouteName}"));

                                        if (!containsRecord)
                                        {
                                            var priority = (Priority)Attribute.GetCustomAttribute(method, typeof(Priority));

                                            var priorityValue = 1.0f;

                                            if (priority != null)
                                            {
                                                priorityValue = priority.Value;
                                            }

                                            urlElement = new XElement(xmlns + "url",
                                                new XElement(xmlns + "loc", Uri.EscapeUriString($"{baseUrl}/{routeName}{methodRouteName}")),
                                                    new XElement(xmlns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz")),
                                                    new XElement(xmlns + "priority", priorityValue));
                                            root.Add(urlElement);
                                        }
                                    }

                                    var details = GetDetailRecordNodes(controller.Name, method.Name);

                                    if (details.Length > 0)
                                    {
                                        foreach (var detail in details)
                                        {
                                            urlElement = new XElement(
                                                xmlns + "url",
                                                new XElement(xmlns + "loc", Uri.EscapeUriString($"{baseUrl}/{routeName}{methodRouteName}/{detail.Route}")),
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

        private SiteMapNodeDetail[] GetDetailRecordNodes(string controllerName, string methodName)
        {
            return _detailNodes
                .Where(s =>
                    s.Controller.ToLower().Trim() == controllerName.ToLower().Trim() &&
                    s.Method.ToLower().Trim() == methodName.ToLower().Trim())
                .ToArray();
        }
    }
}