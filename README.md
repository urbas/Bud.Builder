[![Build status](https://ci.appveyor.com/api/projects/status/6u8p65sqh4hka0bk/branch/master?svg=true)](https://ci.appveyor.com/project/urbas/bud-building/branch/master)


# About

Bud.Building is a library for defining and executing builds.


# Example

```csharp
using System;
using static Bud.Exec;
using static Bud.Building;

class Program {
  static void Main(string[] args) {
    var typeScript = Build(command: ctx => ctx.Command("tsc.exe", $"--outDir {Arg(ctx.OutputDir)} {Args(ctx.Sources)}"),
                           sourceExt: ".ts",
                           outputExt: ".js");

    RunBuild(typeScript, sourceDir: "src/ts", outputDir: "out/js");
  }
}
```

If the build succeeds, the output JavaScript files will end up in `./out/js`. An exception is thrown if the build fails.
