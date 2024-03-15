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
public class RequiredAttributeTargetAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = $"{Utilities.DiagnosticPrefix}002";

    private const string MessageFormat =
        $"Member '{{0}}' is not a valid target for attribute '{SourceGenerator.AttributeClassname}'; Member must be a `static virtual` interface member";

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
        context.RegisterSymbolAction(AnalyzeRequiredMember, ImmutableArray.Create(SymbolKind.Property, SymbolKind.Method));
    }
#pragma warning restore CA1062 // Validate arguments of public methods

    private static void Report(SymbolAnalysisContext context, Location location, string memberName) =>
        context.ReportDiagnostic(Diagnostic.Create(
            descriptor: Rule,
            location: location,
            memberName));

    // checks if target of Required attribute is valid
    // this could probably be rewritten to be a lot more efficient by registering on the attribute or something
    // but i dont know how to do that
    private static void AnalyzeRequiredMember(SymbolAnalysisContext context)
    {
        var token = context.CancellationToken;
        token.ThrowIfCancellationRequested();
        
        var symbol = context.Symbol;

        if (!symbol.GetAttributes().Any(Utilities.IsRequiredAttribute)) return;
        
        if (symbol is not
            {
                ContainingType.TypeKind: TypeKind.Interface,
                IsStatic: true, 
                IsVirtual: true
            })
        {
            var location = symbol.Locations.First();
            Report(context, location, symbol.Name);
        }
    }
}