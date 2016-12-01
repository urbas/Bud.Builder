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
      TrimVerb.TrimTxtFiles(args.RootDir, args.SourceFiles, args.OutDir, args.OutExt);
      return 0;
    }

    private static int OnError(IEnumerable<Error> errors) => 1;
  }
}