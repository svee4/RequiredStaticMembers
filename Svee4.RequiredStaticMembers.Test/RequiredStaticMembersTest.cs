using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Svee4.RequiredStaticMembers.Test;

[TestClass]
public class RequiredStaticMembersTest
{
    // TODO: remove this redundant property and just AbstractAttributeGenerator.AttributeClassCompleteName directly
    private const string AbstractAttributeCompleteFullName = AbstractAttributeGenerator.AttributeClassCompleteName;

    [TestMethod]
    public async Task BasicUsage()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static virtual void Test() => throw new NotImplementedException();
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
    public async Task BasicUsage_SecondInheritance()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static virtual void Test() => throw new NotImplementedException();
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
    public async Task BasicUsage_FirstInheritance()
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
    static virtual void Test() => throw new NotImplementedException();
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
    public async Task BasicError()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static virtual void Test() => throw new NotImplementedException();
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
    public async Task BasicError_SecondInheritance()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static virtual void Test() => throw new NotImplementedException();
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
    public async Task BasicError_FirstInheritance()
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
    static virtual void Test() => throw new NotImplementedException();
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
    
    [TestMethod]
    public async Task BasicError_ParameterCountMismatch()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static virtual void Test1(int a) => throw new NotImplementedException();
    [{{AbstractAttributeCompleteFullName}}]
    static virtual void Test2(int a, int b) => throw new NotImplementedException();

}

class Concrete : IBase
{
    public static void Test1(int a, int b) {}
    public static void Test2(int a) {}
}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        Assert.AreEqual(result.Length, 2);
    }
    
    
    [TestMethod]
    public async Task BasicError_ParameterTypeMismatch()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static virtual void Test(string a) => throw new NotImplementedException();
}

class Concrete : IBase
{
    public static void Test(int a) {}
}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        Assert.AreEqual(result.Length, 1);
    }
    
       [TestMethod]
    public async Task BasicError_ReturnTypeMismatch()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{AbstractAttributeCompleteFullName}}]
    static virtual string Test() => throw new NotImplementedException();
}

class Concrete : IBase
{
    public static int Test() => 3;
}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        Assert.AreEqual(result.Length, 1);
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
