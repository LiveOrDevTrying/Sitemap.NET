using System;

namespace SiteMaps.NET.Attributes
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public class NoSitemap : Attribute
    {
    }
}
