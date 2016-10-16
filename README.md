[![Build status](https://ci.appveyor.com/api/projects/status/6u8p65sqh4hka0bk/branch/master?svg=true)](https://ci.appveyor.com/project/urbas/bud-building/branch/master)

__Table of contents__

* [About](#about)


# About

Bud.Building is a library for defining and executing builds.

# Example

```csharp
using System;
using static Bud.Exec;
using static Bud.Building;

class Program {
  static void Main(string[] args) {
    var typeScript = Build(command:   ctx => ctx.Run("tsc.exe", $"--outDir {ctx.OutputDir} {Args(ctx.Sources)}"),
                           sources:   "src/**/*.ts", 
                           outputDir: "build/js",
                           outputExt: ".js");

    RunBuild(typeScript);
  }
}
```

If you run the above program, you will get output similar to this:

```
$ Program.exe
[1/1| 21:32:45.123] Building 'src/**/.*ts' -> 'build/js/**/*.js'.
[1/1| 21:32:45.147] Running: tsc.exe --outDir build/js src/main.ts
[1/1| 21:32:45.239] out> Typescript output...
[1/1| 21:32:46.033] out> Typescript output...
[1/1| 21:32:55.762] out> Typescript output...
[1/1| 21:33:12.486] out> Typescript output...
[1/1| 21:51:26.007] Done building 'src/**/.*ts' -> 'build/js/**/*.js' in 00:18:40.884.

$ Program.exe
[1/1| 21:32:45.123] Skipping 'src/**/.*ts' -> 'build/js/**/*.js'. Up-to-date.

$ rm -Rf build
$ Program.exe
[1/1| 21:32:45.123] Building 'src/**/.*ts' -> 'build/js/**/*.js'.
[1/1| 21:32:45.147] Running: tsc.exe --outDir build/js src/main.ts
[1/1| 21:32:45.239] err> Typescript error output...
[1/1| 21:32:46.033] exit-code> 7
[1/1| 21:32:55.762] Failed.
```

The process will exit with error code 0 if the build succeeds. Otherwise it will exit with error code 1.