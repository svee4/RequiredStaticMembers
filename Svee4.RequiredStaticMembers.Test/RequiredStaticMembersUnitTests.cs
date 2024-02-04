using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Svee4.RequiredStaticMembers.Test;

[TestClass]
public class RequiredStaticMembersUnitTest
{
    private const string AbstractAttributeCompleteFullName = $"{Utilities.BaseNamespace}.{AbstractAttributeGenerator.AttributeClassname}";

    [TestMethod]
    public async Task RSMAnalyzer_DoesNotRaiseError_Basic()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static void Test() => throw new NotImplementedException();
}

class Concrete : IBase
{
    public static void Test() {}
}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        Assert.AreEqual(result.Length, 0);
    }

    [TestMethod]
    public async Task RSMAnalyzer_DoesNotRaiseError_SecondInheritance()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static void Test() => throw new NotImplementedException();
}

interface IDerived : IBase
{

}

class Concrete : IDerived
{
    public static void Test() {}
}
""";

        var result = await GetDiagnosticsDefaultImpl(test);
        Assert.AreEqual(result.Length, 0);
    }

    [TestMethod]
    public async Task RSMAnalyzer_DoesNotRaiseError_FirstInheritance()
    {
        const string test = $$"""
using System;

return;

interface IBase
{

}

interface IDerived : IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static void Test() => throw new NotImplementedException();
}

class Concrete : IDerived
{
    public static void Test() {}
}
""";

        var result = await GetDiagnosticsDefaultImpl(test);
        Assert.AreEqual(result.Length, 0);
    }

    [TestMethod]
    public async Task RSMAnalyzer_RaisesError_Basic()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static void Test() => throw new NotImplementedException();
}

class Concrete : IBase
{

}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        Assert.AreEqual(result.Length, 1);
        Assert.AreEqual(result[0].Id, RequiredStaticMembersAnalyzer.DiagnosticId);
        Assert.AreEqual(result[0].GetMessage(), RequiredStaticMembersAnalyzer.GetFormattedMessage("Concrete", "Test", "IBase"));

    }

    [TestMethod]
    public async Task RSMAnalyzer_RaisesError_SecondInheritance()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static void Test() => throw new NotImplementedException();
}

interface IDerived : IBase {}

class Concrete : IDerived
{

}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        Assert.AreEqual(result.Length, 1);
        Assert.AreEqual(result[0].Id, RequiredStaticMembersAnalyzer.DiagnosticId);
        Assert.AreEqual(result[0].GetMessage(), RequiredStaticMembersAnalyzer.GetFormattedMessage("Concrete", "Test", "IBase"));
    }

    [TestMethod]
    public async Task RSMAnalyzer_RaisesError_FirstInheritance()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
}

interface IDerived : IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static void Test() => throw new NotImplementedException();
}

    class Concrete : IDerived
{

}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        Assert.AreEqual(result.Length, 1);
        Assert.AreEqual(result[0].Id, RequiredStaticMembersAnalyzer.DiagnosticId);
        Assert.AreEqual(result[0].GetMessage(), RequiredStaticMembersAnalyzer.GetFormattedMessage("Concrete", "Test", "IDerived"));
    }

    /// <summary>
    /// Runs generator and analyzer on source code and returns generated diagnostics
    /// </summary>
    /// <param name="sourceText">C# code to parse</param>
    /// <returns></returns>
    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsDefaultImpl(string sourceText)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);

        var compilation = CSharpCompilation.Create(
            "Test",
            [syntaxTree],
            Basic.Reference.Assemblies.Net80.References.All
        );

        CSharpGeneratorDriver.Create(new AbstractAttributeGenerator())
                        .RunGeneratorsAndUpdateCompilation(compilation, out var compilationResult, out _);

        var compilationResultWithAnalyzers = compilationResult.WithAnalyzers([new RequiredStaticMembersAnalyzer()]);

        return await compilationResultWithAnalyzers.GetAllDiagnosticsAsync();
    }


}
