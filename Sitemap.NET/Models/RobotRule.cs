using System;
using System.Collections.Generic;
using System.Text;

namespace SiteMap.NET.Models
{
    public struct RobotRule
    {
        public string[] UserAgents { get; set; }
        public string[] Disallow { get; set; }
    }
}
