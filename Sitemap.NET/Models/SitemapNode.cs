using SiteMap.NET.Enums;
using System;

namespace SiteMap.NET.Models
{
    public struct SitemapNode
    {
        public SiteMapFrequency? Frequency { get; set; }
        public DateTime? LastModified { get; set; }
        public double? Priority { get; set; }
        public string Url { get; set; }
    }
}
