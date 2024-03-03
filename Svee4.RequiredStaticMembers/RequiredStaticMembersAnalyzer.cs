using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Svee4.RequiredStaticMembers;


[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequiredStaticMembersAnalyzer : DiagnosticAnalyzer
{
    
    public const string DiagnosticId = $"{Utilities.DiagnosticPrefix}001";

    private const string MessageFormat = "Type '{0}' does not implement required static virtual member '{1}' from interface '{2}'";

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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    
#pragma warning disable CA1062 // Validate arguments of public methods
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeType, ImmutableArray.Create(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration));
    }
#pragma warning restore CA1062 // Validate arguments of public methods

    
    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        var typeSymbol = (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node);
        Debug.Assert(typeSymbol is not null);
        
        foreach (INamedTypeSymbol @interface in typeSymbol.AllInterfaces)
        {
            foreach (ISymbol member in @interface.GetMembers())
            {
                if (member is not IMethodSymbol or IPropertySymbol) continue;
                if (!member.GetAttributes().Any(IsAbstractAttribute)) continue;

                var implementation = typeSymbol.FindImplementationForInterfaceMember(member);
                
                // if the type does not have an implementation, FindImplementationForInterfaceMember will return the interface's default implementation
                Debug.Assert(implementation is not null);

                // so, we need to check that the implementation is not that of the interface's
                if (!SymbolEqualityComparer.Default.Equals(typeSymbol, implementation.ContainingType))
                {
                    // location could be the interface node location but i dont know how to get that
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: Rule,
                        location: ((TypeDeclarationSyntax)context.Node).Identifier.GetLocation(),
                        /* type      */ typeSymbol.Name,
                        /* member    */ member.Name,
                        /* interface */ member.ContainingSymbol.Name));
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAbstractAttribute(AttributeData attribute) =>
        // i dont know if theres a better way
        attribute.AttributeClass?.Name 
            is SourceGenerator.AttributeClassname 
            or $"{Utilities.BaseNamespace}.{SourceGenerator.AttributeClassname}";
}
