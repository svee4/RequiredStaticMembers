using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Svee4.RequiredStaticMembers;

public static class Utilities
{
    public const string DiagnosticPrefix = "RSM";
    public const string BaseNamespace = "Svee4.RequiredStaticMembers";
    public const string AnalyzerCategory = "RequiredStaticMembers";
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsRequiredAttribute(AttributeData attribute) =>
        // i dont know if theres a better way
        attribute.AttributeClass?.Name
            is SourceGenerator.AttributeClassname
            or $"{BaseNamespace}.{SourceGenerator.AttributeClassname}";

}
