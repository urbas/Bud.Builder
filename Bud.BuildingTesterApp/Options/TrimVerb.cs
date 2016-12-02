using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace Bud.BuildingTesterApp.Options {
  [Verb("trim", HelpText = "Removes whitespace from the beginning and the end of given files.")]
  public class TrimVerb {
    [Option("rootDir", HelpText = "This directory will be taken as the base directory in which source files are " +
                                  "located. Relative paths and folder structure is calculated relative to this " +
                                  "directory.", Required = true)]
    public string RootDir { get; set; }

    [Option("outDir", HelpText = "Output files will be placed into this directory. The folder structure of the " +
                                 "output is the same as that of the corresponing source files relative to the root " +
                                 "directory.", Required = true)]
    public string OutDir { get; set; }

    [Option("outExt", HelpText = "The extension of output files.", Default = ".nospace")]
    public string OutExt { get; set; }

    [Value(0, MetaName = "SOURCE_FILES", HelpText = "The files to trim.", Default = new string[0])]
    public IEnumerable<string> SourceFiles { get; set; }

    public static int DoTrim(TrimVerb args) {
      TrimTxtFiles(args.RootDir, args.SourceFiles, args.OutDir, args.OutExt);
      return 0;
    }

    public static void TrimTxtFiles(string rootDir, IEnumerable<string> sourceFiles, string outDir, string outputExt) {
      var rootDirUri = new Uri($"{rootDir}/");
      foreach (var sourceFile in sourceFiles) {
        var content = File.ReadAllText(sourceFile).Trim();
        var relativeSrcPath = rootDirUri.MakeRelativeUri(new Uri(sourceFile)).ToString();
        var outputFile = Path.Combine(outDir,
                                      Path.GetDirectoryName(relativeSrcPath),
                                      Path.GetFileNameWithoutExtension(relativeSrcPath) + outputExt);
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
        Console.WriteLine($"Producing file: {outputFile}");

        File.WriteAllText(outputFile, content);
      }
    }
  }
}