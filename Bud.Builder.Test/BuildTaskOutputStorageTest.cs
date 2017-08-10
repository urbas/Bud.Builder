using System;
using System.Collections.Generic;
using NUnit.Framework;
using static Bud.Cp;

namespace Bud {
  public class BuildTaskOutputStorageTest {
    private string sourceDir1;
    private string sourceDir2;
    private TmpDir dir;
    private string sourceFile1;
    private string targetDir;
    private BuildTaskOutputStorage storage;

    [SetUp]
    public void SetUp() {
      dir = new TmpDir();
      sourceDir1 = dir.CreateDir("sourceDir1");
      sourceDir2 = dir.CreateDir("sourceDir2");
      sourceFile1 = dir.CreateFile("foo", "sourceDir1", "file1");
      targetDir = dir.CreatePath("target");
      storage = new BuildTaskOutputStorage(targetDir, new Dictionary<Uri, byte[]>());
    }

    [TearDown]
    public void TearDown() => dir.Dispose();

    [Test]
    public void Test_initial_copy() {
      CopyDir(sourceDir1, targetDir, NextStorage());
      FileAssert.AreEqual(sourceFile1, dir.CreatePath("target", "file1"));
    }

    [Test]
    public void Test_copy_non_existing_source_dir() {
      CopyDir(dir.CreatePath("invalidDir"), targetDir, NextStorage());
      DirectoryAssert.Exists(targetDir);
    }

    [Test]
    public void Test_skip_second_copy() {
      CopyDir(sourceDir1, targetDir, NextStorage());
      dir.CreateFile("modified", "sourceDir1", "file1");
      CopyDir(sourceDir1, targetDir, NextStorage());
      
      var originalSourceFile = dir.CreateFile("foo", "original");
      FileAssert.AreEqual(originalSourceFile, dir.CreatePath("target", "file1"));
    }

    [Test]
    public void Test_do_second_copy() {
      CopyDir(sourceDir1, targetDir, NextStorage());
      var sourceFile2 = dir.CreateFile("modified", "sourceDir2", "file1");
      CopyDir(sourceDir2, targetDir, NextStorage());
      
      FileAssert.AreEqual(sourceFile2, dir.CreatePath("target", "file1"));
    }

    private BuildTaskOutputStorage NextStorage() {
      storage = new BuildTaskOutputStorage(targetDir, storage.CalculateTargetSignatures());
      return storage;
    }

    [Test]
    public void Test_delete_target_file() {
      CopyDir(sourceDir1, targetDir, NextStorage());
      CopyDir(sourceDir2, targetDir, NextStorage());
      
      FileAssert.DoesNotExist(dir.CreatePath("target", "file1"));
    }

    [Test]
    public void Test_delete_subdirectory() {
      dir.CreateDir("sourceDir1", "subdir1");
      
      CopyDir(sourceDir1, targetDir, NextStorage());
      DirectoryAssert.Exists(dir.CreatePath("target", "subdir1"));
      
      CopyDir(sourceDir2, targetDir, NextStorage());
      DirectoryAssert.DoesNotExist(dir.CreatePath("target", "subdir1"));
    }
  }
}