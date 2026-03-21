using System;

namespace Il2CppDumper;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
internal class VersionAttribute : Attribute
{
    public double Min { get; set; } = 0;
    public double Max { get; set; } = 99;
}
