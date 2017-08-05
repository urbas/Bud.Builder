[![Build status](https://ci.appveyor.com/api/projects/status/1qvcg4cnnenyl54x/branch/master?svg=true)](https://ci.appveyor.com/project/urbas/bud-builder/branch/master) [![NuGet](https://img.shields.io/nuget/v/Bud.Builder.svg)](https://www.nuget.org/packages/Bud.Builder/)



# About

Bud.Builder is a library for defining and executing builds.



# Example

```csharp
using Bud;

class Program {
  static void Main(string[] args) {
    var task1 = new FooBuildTask("");
    var task2 = new BarBuildTask("", dependsOn: task1);
    
    Builder.Execute("srcDir", "outDir", ".bud", task1);
  }
}

// Your build tasks have to implement the IBuildTask interface. 
class FooBuildTask : IBuildTask { ... }
class BarBuildTask : IBuildTask { ... }
```
