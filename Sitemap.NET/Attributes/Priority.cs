using System;

namespace SiteMaps.NET.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class Priority : Attribute
    {
        public float Value { get; set; } = 1.0f;
    }
}
