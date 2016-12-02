using System.Collections.Generic;
using Bud.BuildingTesterApp.Options;
using CommandLine;

namespace Bud.BuildingTesterApp {
  public class Program {
    public static void Main(string[] args)
      => Parser.Default
               .ParseArguments<TrimVerb>(args)
               .MapResult(TrimVerb.DoTrim, OnError);

    private static int OnError(IEnumerable<Error> errors) => 1;
  }
}