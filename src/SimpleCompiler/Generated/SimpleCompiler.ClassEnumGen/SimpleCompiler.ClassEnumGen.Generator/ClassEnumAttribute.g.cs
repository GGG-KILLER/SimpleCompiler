using System;

#nullable enable

namespace ClassEnumGen;

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
public sealed class ClassEnumAttribute : Attribute
{
    public string? KindEnumName { get; set; }
}