using System;

namespace Krins.Analyzer.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AotReflectionAttribute : Attribute { }

    public struct PropertyInfo
    {
        public string Name { get; set; }

        public Type Type { get; set; }
    }
}
