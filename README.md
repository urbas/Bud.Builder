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
    var typeScript = Build(command:   ctx => ctx.Command("tsc.exe", $"--outDir {ctx.OutputDir} {Args(ctx.Sources)}"),
                           sourceDir:  "src",
                           sourceExt: ".ts", 
                           outputDir: "build/js",
                           outputExt: ".js");

    RunBuild(typeScript);
  }
}
```

If you run the above program, you will get output similar to this:

```
$ Program.exe
[1/1   0.000s] Building 'src/**/.*ts' -> 'build/js/**/*.js'.
[1/1   0.147s] Running: tsc.exe --outDir build/js src/main.ts
[1/1   0.239s] out> Typescript output...
[1/1   1.033s] out> Typescript output...
[1/1  10.762s] out> Typescript output...
[1/1  27.486s] out> Typescript output...
[1/1 120.007s] Done building 'src/**/.*ts' -> 'build/js/**/*.js'.
Wall time: 1120.007s Total: 1120.007s Status: success

$ Program.exe
[1/1| 21:32:45.123] Skipping 'src/**/.*ts' -> 'build/js/**/*.js'. Up-to-date.

$ rm -Rf build
$ Program.exe
[1/1   0.000s] Building 'src/**/.*ts' -> 'build/js/**/*.js'.
[1/1   0.147s] Running: tsc.exe --outDir build/js src/main.ts
[1/1   0.239s] err> Typescript error output...
[1/1   1.033s] exit-code> 7
[1/1   1.033s] Failed building 'src/**/.*ts' -> 'build/js/**/*.js'.
Wall time: 1.033s Total: 1.033s Status: failure
```

The process will exit with error code 0 if the build succeeds. Otherwise it will exit with error code 1.


# Overview

The library consists of two main parts:

- __Build Definition API__: classes that make up the build graph.

- __Build Execution API__: engines that execute the build graph.


## Build Definition API


### The `IBuildTask` class

The basic building block of the Build Definition API is the `IBuildTask` interface:

```csharp
public interface IBuildTask {
  void Execute(BuildContext ctx);
  ImmutableArray<IBuildTask> Dependencies {get;};
}
```


### `Bud.Building.Build` functions

Call `Bud.Building.Build(...)` family of methods to create build tasks and combine them into graphs.


## Build Execution API


### The `Bud.Building.RunBuild` function

This executes your build tasks in parallel and outputs progress to the standard output (or a text writer if you provide one).

This function will throw an exception if the build fails.