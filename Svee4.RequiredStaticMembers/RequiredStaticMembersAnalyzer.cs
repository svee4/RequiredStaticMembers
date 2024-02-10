using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Svee4.RequiredStaticMembers;


[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequiredStaticMembersAnalyzer : DiagnosticAnalyzer
{
    
    public const string DiagnosticId = $"{Utilities.DiagnosticPrefix}001";

    private const string MessageFormat = "Type '{0}' does not implement required static member '{1}' from interface '{2}'";

    /// <summary>
    /// Formats a message from this analyzer with the given parameters
    /// </summary>
    /// <param name="classname"></param>
    /// <param name="membername"></param>
    /// <param name="interfacename"></param>
    /// <returns></returns>
    public static string GetFormattedMessage(string classname, string membername, string interfacename) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, MessageFormat, classname, membername, interfacename);

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Required static interface member is not implemented",
        messageFormat: MessageFormat,
        category: Utilities.AnalyzerCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: null
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    
#pragma warning disable CA1062 // Validate arguments of public methods
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeContainingType, ImmutableArray.Create(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration));
    }
#pragma warning restore CA1062 // Validate arguments of public methods

    
    private static void AnalyzeContainingType(SyntaxNodeAnalysisContext context)
    {
        var typeSymbol = (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node);
        Debug.Assert(typeSymbol is not null, $"{nameof(typeSymbol)} is not null");
        
        var symbolEqualityComparer = SymbolEqualityComparer.Default;
        
        foreach (INamedTypeSymbol inter in typeSymbol.AllInterfaces)
        {
            foreach (ISymbol member in inter.GetMembers())
            {
                // do cheap checks first
                if (member is not IMethodSymbol or IPropertySymbol) continue;
                if (!member.GetAttributes().Any(IsAbstractAttribute)) continue;

                var implementation = typeSymbol.FindImplementationForInterfaceMember(member);
                
                // if the type does not have an implementation, FindImplementationForInterfaceMember will return the interface's default implementation
                Debug.Assert(implementation is not null);

                // so, we need to check that the implementation is not that of the interface's
                if (!symbolEqualityComparer.Equals(typeSymbol, implementation.ContainingType))
                {
                    ReportDiagnostic(context, typeSymbol, member);
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReportDiagnostic(
        SyntaxNodeAnalysisContext context,
        ITypeSymbol typeSymbol,
        ISymbol memberSymbol
    ) =>
        context.ReportDiagnostic(Diagnostic.Create(
            descriptor: Rule,
            location: context.Node.GetLocation(),
            /* type */ typeSymbol.Name,
            /* member */ memberSymbol.Name,
            /* interface */ memberSymbol.ContainingSymbol.Name));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAbstractAttribute(AttributeData attribute) =>
        attribute.AttributeClass?.Name 
            is AbstractAttributeGenerator.AttributeClassname 
            or $"{Utilities.BaseNamespace}.{AbstractAttributeGenerator.AttributeClassname}";
}
