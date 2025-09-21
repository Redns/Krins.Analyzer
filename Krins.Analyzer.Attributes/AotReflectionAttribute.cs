using System;

namespace Krins.Analyzer.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AotReflectionAttribute : Attribute { }
}
