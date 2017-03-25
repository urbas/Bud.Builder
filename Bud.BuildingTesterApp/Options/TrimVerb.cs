using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using static System.IO.Path;

namespace Bud.BuildingTesterApp.Options {
  [Verb("trim", HelpText = "Removes whitespace from the beginning and the end of given files.")]
  public class TrimVerb {
    [Option("srcDir", HelpText = "This directory will be taken as the base directory in which source files are " +
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

    public static void TrimTxtFiles(string srcDir, IEnumerable<string> srcFiles, string outDir, string outExt) {
      var srcDirUri = new Uri($"{srcDir}/");
      foreach (var srcFile in srcFiles) {
        TrimTxtFile(srcFile, outDir, srcDirUri, outExt);
      }
    }

    private static void TrimTxtFile(string srcFile, string outDir, Uri srcDir, string outExt) {
      var content = File.ReadAllText(srcFile).Trim();
      var relSrcPath = srcDir.MakeRelativeUri(new Uri(srcFile)).ToString();
      var outFile = Combine(outDir, GetDirectoryName(relSrcPath), GetFileNameWithoutExtension(relSrcPath) + outExt);
      Directory.CreateDirectory(GetDirectoryName(outFile));
      Console.WriteLine($"Trimmed file '{srcFile}' to file '{outFile}'.");
      File.WriteAllText(outFile, content);
    }
  }
}