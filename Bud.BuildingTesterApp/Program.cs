using System;
using System.Collections.Generic;
using Bud.BuildingTesterApp.Options;
using CommandLine;

namespace Bud.BuildingTesterApp {
  public class Program {
    public static void Main(string[] args)
      => Parser.Default
               .ParseArguments<TrimVerb>(args)
               .MapResult(DoTrim, OnError);

    private static int DoTrim(TrimVerb args) {
      Console.WriteLine($"Trimming. outDir: {args.OutDir}, rootDir: {args.RootDir}, files: {string.Join(", ", args.SourceFiles)}");
      return 0;
    }

    private static int OnError(IEnumerable<Error> errors) {
      return 1;
    }
  }
}