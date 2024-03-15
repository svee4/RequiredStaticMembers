using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Svee4.RequiredStaticMembers;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{

    public const string AttributeName = "Required";
    public const string AttributeClassname = $"{AttributeName}Attribute";
    public const string AttributeClassCompleteName = $"{Utilities.BaseNamespace}.{AttributeClassname}";
    public const string ExceptionClassName = "RequiredStaticMemberAccessException";
    public const string ExceptionClassCompleteName = $"{Utilities.BaseNamespace}.{ExceptionClassName}";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context2 =>
        {
            context2.AddSource($"{Utilities.BaseNamespace}.g.cs",
                $$"""
                using System;
                 
                namespace {{Utilities.BaseNamespace}}
                {
                    /// <summary>
                    /// Apply this attribute to an interface member to enforce types to implement this member.<br/>
                    /// <see href='https://github.com/svee4/RequiredStaticMembers'>Github</see>
                    /// </summary>
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
                    public sealed class {{AttributeClassname}} : Attribute {}
                    
                    /// <summary>
                    /// This exception should be thrown from a required static member which should not have been called
                    /// </summary>
                    public sealed class {{ExceptionClassName}}: Exception
                    {
                        public override string Message { get; }
                        
                        public {{ExceptionClassName}}(string memberName) => 
                            Message = $"Attempted to access required static virtual interface member '{memberName}' that does not have an implementation in the base type";
                        
                        /// <summary>
                        /// Throws a new <see cref="{{ExceptionClassName}}" />
                        /// </summary>
                        /// <param name="memberName">Name of the member</param>
                        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
                        public static void Throw(string memberName) => 
                            throw new {{ExceptionClassName}}(memberName);
                        
                        /// <summary>
                        /// Throws a new <see cref="{{ExceptionClassName}}" />
                        /// </summary>
                        /// <param name="memberName">Name of the member</param>
                        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
                        public static T Throw<T>(string memberName) => 
                            throw new {{ExceptionClassName}}(memberName);
                        
                        /// <summary>
                        /// Throws a new <see cref="{{ExceptionClassName}}" /> with the calling member as the name of the member
                        /// </summary>
                        /// <remarks>
                        /// This method automatically uses the name of the calling member as the memberName argument, 
                        /// and thus should only be called directly from a Required interface member
                        /// </remarks>
                        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
                        public static void ThrowWithCallerName(
                            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "") => 
                            throw new {{ExceptionClassName}}(memberName);
                            
                        /// <summary>
                        /// Throws a new <see cref="{{ExceptionClassName}}" /> with the calling member as the name of the member
                        /// </summary>
                        /// <remarks>
                        /// This method automatically uses the name of the calling member as the memberName argument, 
                        /// and thus should only be called directly from a Required interface member
                        /// </remarks>
                        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
                        public static T ThrowWithCallerName<T>(
                            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "") => 
                            throw new {{ExceptionClassName}}(memberName);
                    }
                    
                }
                """);
        });
    }
}
