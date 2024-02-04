using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Svee4.RequiredStaticMembers;

[Generator]
public class AbstractAttributeGenerator : IIncrementalGenerator
{

    public const string AttributeName = "AbstractAttribute";
    public const string AttributeClassname = $"{AttributeName}Attribute";


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context2 =>
        {
            context2.AddSource($"{Utilities.BaseNamespace}.{AttributeName}.g.cs",
                $$"""
                using System;
                 
                namespace {{Utilities.BaseNamespace}}
                {
                    /// <summary>
                    /// Apply this attribute to an interface member to track its implementation status in derived types.<br/>
                    /// <see href='https://github.com/svee4/RequiredStaticMembers'>Github</see>
                    /// </summary>
                    [AttributeUsage(AttributeTargets.Method)]
                    public sealed class {{AttributeClassname}} : Attribute {}
                }
                """);
        });
    }
}
