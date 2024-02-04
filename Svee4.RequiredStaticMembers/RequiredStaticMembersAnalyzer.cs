using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        var symbol = (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node);
        if (symbol is null) return;

        // collect methods that should be implemented
        Dictionary<string, ISymbol> abstractMembers = [];
        foreach (ISymbol member in symbol.AllInterfaces.SelectMany(inter => inter.GetMembers()))
        {
            if (member is IMethodSymbol or IPropertySymbol 
                && member.IsStatic
                && member.GetAttributes().Any(IsAbstractAttribute))
            {
                abstractMembers.Add(member.Name, member);
            }
        }
        

        // remove methods that are implemented
        foreach (ISymbol method in symbol.GetMembers().Where(member => member is IMethodSymbol))
        {
            // static members cannot be explicitly implemented. if the name matches, it must be an implementation
            abstractMembers.Remove(method.Name);
        }

        // all methods that haven't been removed are not implemented
        foreach (var method in abstractMembers.Values)
        {
            var diagnostic = Diagnostic.Create(
                descriptor: Rule, 
                location: context.Node.GetLocation(), 
                /* type */      symbol.Name, 
                /* member */    method.Name, 
                /* interface */ method.ContainingSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }

    }

    private static bool IsAbstractAttribute(AttributeData attribute) =>
        attribute.AttributeClass!.Name 
            is AbstractAttributeGenerator.AttributeClassname 
            or $"{Utilities.BaseNamespace}.{AbstractAttributeGenerator.AttributeClassname}";
}
