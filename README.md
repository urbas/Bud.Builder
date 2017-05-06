[![Build status](https://ci.appveyor.com/api/projects/status/1qvcg4cnnenyl54x/branch/master?svg=true)](https://ci.appveyor.com/project/urbas/bud-builder/branch/master)



# About

Bud.Builder is a library for defining and executing builds.



# Example

```csharp
using System;
using Bud.Builder;

class Program {
  static void Main(string[] args)
    => Builder.Execute("srcDir", "outDir", ".bud", task1, task2, ...);
}
```
