using SiteMaps.NET.Enums;
using System;

namespace SiteMaps.NET.Models
{
    public struct SiteMapNode
    {
        public SiteMapFrequency? Frequency { get; set; }
        public DateTime? LastModified { get; set; }
        public double? Priority { get; set; }
        public string Url { get; set; }
    }
}
