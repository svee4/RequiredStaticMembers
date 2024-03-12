﻿using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Svee4.RequiredStaticMembers.Test;

[TestClass]
public class RequiredStaticMembersTest
{

    [TestMethod]
    public async Task BasicUsage()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{SourceGenerator.AttributeClassCompleteName}}]
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
    [{{SourceGenerator.AttributeClassCompleteName}}]
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
    [{{SourceGenerator.AttributeClassCompleteName}}]
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
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual void Test() => throw new NotImplementedException();
}

class Concrete : IBase
{

}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        AssertDiagnosticIdsMatch(result, RequiredStaticMembersAnalyzer.DiagnosticId);
    }

    [TestMethod]
    public async Task BasicError_SecondInheritance()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual void Test() => throw new NotImplementedException();
}

interface IDerived : IBase {}

class Concrete : IDerived
{

}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        AssertDiagnosticIdsMatch(result, RequiredStaticMembersAnalyzer.DiagnosticId);
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
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual void Test() => throw new NotImplementedException();
}

class Concrete : IDerived
{

}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        AssertDiagnosticIdsMatch(result, RequiredStaticMembersAnalyzer.DiagnosticId);
    }
    
    [TestMethod]
    public async Task BasicError_ParameterCountMismatch()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual void Test1(int a) => throw new NotImplementedException();
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual void Test2(int a, int b) => throw new NotImplementedException();

}

class Concrete : IBase
{
    public static void Test1(int a, int b) {}
    public static void Test2(int a) {}
}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        AssertDiagnosticIdsMatch(result, RequiredStaticMembersAnalyzer.DiagnosticId, RequiredStaticMembersAnalyzer.DiagnosticId);
    }
    
    
    [TestMethod]
    public async Task BasicError_ParameterTypeMismatch()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual void Test(string a) => throw new NotImplementedException();
}

class Concrete : IBase
{
    public static void Test(int a) {}
}
""";

        var result = await GetDiagnosticsDefaultImpl(test);

        AssertDiagnosticIdsMatch(result, RequiredStaticMembersAnalyzer.DiagnosticId);
    }
    
    [TestMethod]
    public async Task BasicError_ReturnTypeMismatch()
    {
        const string test = $$"""
using System;

return;

interface IBase
{
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual string Test() => throw new NotImplementedException();
}

class Concrete : IBase
{
    public static int Test() => 3;
}
""";

        ImmutableArray<Diagnostic> result = await GetDiagnosticsDefaultImpl(test);
        AssertDiagnosticIdsMatch(result, RequiredStaticMembersAnalyzer.DiagnosticId);
    }
    
    [TestMethod]
    public async Task BasicUsage_OneGeneric()
    {
                const string test = $$"""
using System;

return;

interface IBase<T>
{
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual T Test() => throw new NotImplementedException();
}

class Concrete : IBase<int>
{
    public static int Test() => 3;
}
""";

        ImmutableArray<Diagnostic> result = await GetDiagnosticsDefaultImpl(test);

        Assert.AreEqual(result.Length, 0);
    }

        
    [TestMethod]
    public async Task BasicUsage_TwoGeneric()
    {
                const string test = $$"""
using System;

return;

interface IBase<T>
{
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual void Test(T _) => throw new NotImplementedException();
}

class Concrete : IBase<int>, IBase<string>
{
    public static void Test(int _) {}
    public static void Test(string _) {}
}
""";

        ImmutableArray<Diagnostic> result = await GetDiagnosticsDefaultImpl(test);
        Assert.AreEqual(result.Length, 0);
    }
        
    [TestMethod]
    public async Task BasicError_WrongGeneric()
    {
                const string test = $$"""
using System;

return;

interface IBase<T>
{
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual T Test() => throw new NotImplementedException();
}

class Concrete : IBase<int>
{
    public static string Test() => "";
}
""";

        ImmutableArray<Diagnostic> result = await GetDiagnosticsDefaultImpl(test);
        AssertDiagnosticIdsMatch(result, RequiredStaticMembersAnalyzer.DiagnosticId);
    }
        
    [TestMethod]
    public async Task BasicError_MissingSecondGeneric()
    {
                const string test = $$"""
using System;

return;

interface IBase<T>
{
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual void Test(T _) => throw new NotImplementedException();
}

class Concrete : IBase<string>, IBase<int>
{
    public static void Test(string _) {}
}
""";

        ImmutableArray<Diagnostic> result = await GetDiagnosticsDefaultImpl(test);
        AssertDiagnosticIdsMatch(result, RequiredStaticMembersAnalyzer.DiagnosticId);
    }

    [TestMethod]
    public async Task AttributeTarget_ValidTarget()
    {
        const string test = $$"""
using System;

return;

interface IBase<T>
{
    [{{SourceGenerator.AttributeClassCompleteName}}]
    static virtual void Test() => throw new NotImplementedException();
}
""";

        ImmutableArray<Diagnostic> result = await GetDiagnosticsDefaultImpl(test);
        Assert.AreEqual(result.Length, 0);
    }
    
    [TestMethod]
    public async Task AttributeTarget_InvalidTarget_1()
    {
        const string test = $$"""
using System;

return;

interface IBase<T>
{
  [{{SourceGenerator.AttributeClassCompleteName}}]
  static void Test() => throw new NotImplementedException();
}
""";

        ImmutableArray<Diagnostic> result = await GetDiagnosticsDefaultImpl(test);
        AssertDiagnosticIdsMatch(result, RequiredAttributeTargetAnalyzer.DiagnosticId);
    }
    
    [TestMethod]
    public async Task AttributeTarget_InvalidTarget_2()
    {
        const string test = $$"""

return;

interface IBase<T>
{
  [{{SourceGenerator.AttributeClassCompleteName}}]
  void Test();
}
""";

        ImmutableArray<Diagnostic> result = await GetDiagnosticsDefaultImpl(test);
        AssertDiagnosticIdsMatch(result, RequiredAttributeTargetAnalyzer.DiagnosticId);
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

        CSharpGeneratorDriver.Create(new SourceGenerator())
                        .RunGeneratorsAndUpdateCompilation(compilation, out var compilationResult, out _);

        var compilationResultWithAnalyzers = compilationResult.WithAnalyzers([new RequiredStaticMembersAnalyzer(), new RequiredAttributeTargetAnalyzer()]);

        return await compilationResultWithAnalyzers.GetAllDiagnosticsAsync();
    }


    /// <summary>
    /// Asserts that the diagnostics' ids are equal to the provided ids. also checks length
    /// </summary>
    /// <param name="diagnostics"></param>
    /// <param name="ids"></param>
    private static void AssertDiagnosticIdsMatch(ImmutableArray<Diagnostic> diagnostics, params string[] ids)
    {
        Assert.AreEqual(diagnostics.Length, ids.Length, "Number of produced diagnostics was different from number of expected diagnostics");
        for (int i = 0; i < ids.Length && i < diagnostics.Length; i++)
        {
            Assert.AreEqual(diagnostics[i].Id, ids[i]);
        }
    }
    
}
