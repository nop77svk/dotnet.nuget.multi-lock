# Multi-Lock - The keyed thread synchronisation

This library introduces simple, yet out-of-the-box functionality for synchronising multiple threads dealing with the same data identifiable by a specific key.

Imagine you want to restrict calls to a web service or queries to a database table in your code. The webservice call or DB query is identifiable by a natural key used for accessing the resource. You may need to restrict your code so that only one thread accesses the resource at a time.

## Features

- Provide encapsulation of dealing with key-specific locks for both sync and async code.
- Lightweight and (hopefully) fast.
- Open source and free to use.
- (Yet to come...) Comprehensive unit tests to ensure reliability and documentation of examples.
- Available on NuGet for easy integration into your projects.

## Supported platforms

- .NET Standard 2.1

## Useful Links

- [Source Code](https://github.com/nop77svk/dotnet.nuget.multi-lock)
- [Change Log](https://github.com/nop77svk/dotnet.nuget.multi-lock/releases)
- [Author](https://www.linkedin.com/in/code-we-trust/)
