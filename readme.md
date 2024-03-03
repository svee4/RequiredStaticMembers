# RequiredStaticMembers

[![NuGet](https://img.shields.io/nuget/v/Svee4.RequiredStaticMembers.svg?style=plastic)](https://www.nuget.org/packages/Svee4.RequiredStaticMembers/)
[![GitHub license](https://img.shields.io/github/license/svee4/RequiredStaticMembers.svg)](https://github.com/svee4/RequiredStaticMembers/blob/main/license.txt)

## Enforce types to implement static virtual interface members

```cs
using Svee4.RequiredStaticMembers;

interface INode
{
  [Abstract]
  public static virtual string Color => throw new InvalidOperationException("This will never be called on accident");
}

class GreenNode : INode
{
  // No error - property is implemented as expected
  public static string Color => "Green";
}

class BlueNode : INode
{
  // Error RequiredStaticMembers001: Type 'BlueNode' does not implement required static member 'GetColor' from interface 'INode'
}
```

## Why?

- Problem: An interface with a `static abstract` member cannot be used as a generic, such as `List<T>`.
- Solution: Replace the `abstract` modifier with `virtual`.
- Problem: Deriving classes are no longer required to implement the member. The interface must provide an implementation, likely one that throws an exception at
  runtime.
- Solution: Use `[AbstractAttribute]` to enforce all deriving classes to implement the static member, making accidental calls to the default implementation
  impossible.

## How do i use it?

1. Install the package from [Nuget](https://www.nuget.org/packages/Svee4.RequiredStaticMembers/)
2. Give a static interface member the attribute `Svee4.RequiredStaticMembers.AbstractAttribute`
3. All done! A deriving type that does not implement the given member will cause an error

## I have an issue or I want to participate in development

Please open an issue or discussion

## Acknowledgments

- The repo of [Immediate.Handlers](https://github.com/viceroypenguin/Immediate.Handlers) was helpful in setting up csproj and CI/CD
