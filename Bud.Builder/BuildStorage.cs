using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static System.IO.SearchOption;

namespace Bud {
  internal class BuildStorage : IStorage {
    private Uri TargetDir { get; }
    private Dictionary<Uri, byte[]> TargetSignatures { get; }
    private readonly Dictionary<Uri, byte[]> sourceFileToSignature = new Dictionary<Uri, byte[]>();
    private readonly Dictionary<Uri, Uri> sourceFileToDir = new Dictionary<Uri, Uri>();

    public BuildStorage(string targetDir, Dictionary<Uri, byte[]> targetSignatures) {
      TargetDir = ToDirUri(targetDir);
      TargetSignatures = targetSignatures;
    }

    public void CreateDirectory(Uri dir) => Directory.CreateDirectory(dir.AbsolutePath);

    public IEnumerable<Uri> EnumerateFiles(Uri dir) {
      var files = Directory.Exists(dir.AbsolutePath)
                    ? Directory.EnumerateFiles(dir.AbsolutePath, "*", AllDirectories)
                               .Select(path => new Uri(path))
                               .ToList()
                    : new List<Uri>();
      if (files.Count > 0 && IsSourceDir(dir)) {
        MemorizeSourceFileSignatures(dir, files);
      }
      return files;
    }

    public IEnumerable<Uri> EnumerateDirectories(Uri dir)
      => Directory.Exists(dir.AbsolutePath)
           ? Directory.EnumerateDirectories(dir.AbsolutePath, "*", AllDirectories).Select(path => new Uri(path))
           : Enumerable.Empty<Uri>();


    public byte[] GetSignature(Uri file) {
      byte[] signature;
      if (sourceFileToSignature.TryGetValue(file, out signature) || 
          TargetSignatures.TryGetValue(TargetDir.MakeRelativeUri(file), out signature)) {
        return signature;
      }
      return Array.Empty<byte>();
    }

    public void DeleteFile(Uri file) => File.Delete(file.AbsolutePath);

    public void CopyFile(Uri sourceFile, Uri targetFile)
      => File.Copy(sourceFile.AbsolutePath, targetFile.AbsolutePath, overwrite: true);

    public void DeleteDirectory(Uri dir) => Directory.Delete(dir.AbsolutePath, recursive: true);


    private static Uri ToDirUri(string targetDir) => new Uri(targetDir.EndsWith("/") ? targetDir : targetDir + "/");

    public Dictionary<Uri, byte[]> CalculateTargetSignatures() {
      var targetSignatures = new Dictionary<Uri, byte[]>();
      foreach (var sourceFileAndSignature in sourceFileToSignature) {
        var sourceFile = sourceFileAndSignature.Key;
        var signature = sourceFileAndSignature.Value;
        var fileRelPath = sourceFileToDir[sourceFile].MakeRelativeUri(sourceFile);
        targetSignatures[fileRelPath] = signature;
      }
      return targetSignatures;
    }
    
    private bool IsSourceDir(Uri dir) => dir != TargetDir;
    
    private void MemorizeSourceFileSignatures(Uri dir, List<Uri> files) {
      var signature = Encoding.UTF8.GetBytes(Path.GetFileName(Path.GetDirectoryName(dir.AbsolutePath)));
      foreach (var file in files) {
        sourceFileToSignature[file] = signature;
        sourceFileToDir[file] = dir;
      }
    }
  }
}