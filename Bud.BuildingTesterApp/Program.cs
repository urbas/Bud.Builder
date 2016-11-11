using System;
using System.Collections.Generic;
using System.IO;
using Bud.BuildingTesterApp.Options;
using CommandLine;

namespace Bud.BuildingTesterApp {
  public class Program {
    public static void Main(string[] args)
      => Parser.Default
               .ParseArguments<TrimVerb>(args)
               .MapResult(DoTrim, OnError);

    private static int DoTrim(TrimVerb args) {
      var rootDirUri = new Uri($"{args.RootDir}/");
      foreach (var sourceFile in args.SourceFiles) {
        var content = File.ReadAllText(sourceFile).Trim();
        var relativeUri = rootDirUri.MakeRelativeUri(new Uri(sourceFile));
        var combine = Path.Combine(args.OutDir, $"{relativeUri}.nospace");
        Directory.CreateDirectory(Path.GetDirectoryName(combine));
        File.WriteAllText(combine, content);
      }
      return 0;
    }

    private static int OnError(IEnumerable<Error> errors) {
      return 1;
    }
  }
}