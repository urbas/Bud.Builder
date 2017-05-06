using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Moq;
using static System.IO.File;

namespace Bud {
  public static class MockBuildTasks {
    public static Mock<IBuildTask> GenerateFile(string taskName, string fileName, string fileContents,
                                                params IBuildTask[] dependencies)
      => BareBuildTask(taskName, dependencies)
        .WithStandardSignature("TaskName:", taskName, "TaskType:", nameof(GenerateFile), "FileName:", fileName,
                               "Parameters:", fileContents)
        .WithExecuteAction((sourceDir, outputDir, deps) => WriteAllText(Path.Combine(outputDir, fileName),
                                                                        fileContents));

    public static Mock<IBuildTask> CopySourceFile(string taskName, string sourceFile, string outputFile,
                                                  params IBuildTask[] dependencies)
      => BareBuildTask(taskName, dependencies)
        .WithStandardSignature("TaskName:", taskName, "TaskType:", nameof(CopySourceFile), "SourceFile:", sourceFile,
                               "OutputFile:", outputFile)
        .WithExecuteAction((sourceDir, outputDir, deps) => Copy(Path.Combine(sourceDir, sourceFile),
                                                                Path.Combine(outputDir, outputFile)));


    public static Mock<IBuildTask> Action(string taskName, Action buildAction,
                                          params IBuildTask[] dependencies)
      => BareBuildTask(taskName, dependencies)
        .WithStandardSignature("TaskName:", taskName, "TaskType:", nameof(Action))
        .WithExecuteAction((sourceDir, outputDir, deps) => buildAction());

    public static Mock<IBuildTask> NoOp(string taskName, params IBuildTask[] dependencies)
      => BareBuildTask(taskName, dependencies)
        .WithStandardSignature("TaskName:", taskName, "TaskType:", nameof(NoOp));

    public static Mock<IBuildTask> WithExecuteAction(this Mock<IBuildTask> buildTaskMock,
                                                     Action<string, string, ImmutableArray<BuildTaskResult>> action) {
      buildTaskMock.Setup(f => f.Execute(It.IsAny<string>(),
                                         It.IsAny<string>(),
                                         It.IsAny<ImmutableArray<BuildTaskResult>>()))
                   .Callback(action);
      return buildTaskMock;
    }

    public static Mock<IBuildTask> WithStandardSignature(this Mock<IBuildTask> buildTaskMock, params string[] salts)
      => WithSignature(buildTaskMock, (srcDir, deps) => CalculateSignature(deps, salts));

    public static Mock<IBuildTask> WithSignature(this Mock<IBuildTask> buildTaskMock, string signature)
      => buildTaskMock.WithSignature((srcDir, deps) => signature);

    public static Mock<IBuildTask> WithSignature(this Mock<IBuildTask> buildTaskMock,
                                                 Func<string, ImmutableArray<BuildTaskResult>, string> signature) {
      buildTaskMock.Setup(f => f.Signature(It.IsAny<string>(), It.IsAny<ImmutableArray<BuildTaskResult>>()))
                   .Returns(signature);
      return buildTaskMock;
    }

    private static string CalculateSignature(ImmutableArray<BuildTaskResult> dependenciesResults,
                                             params string[] salts) {
      var signer = new Sha256Signer();

      signer.Digest("Dependencies:");

      foreach (var dependencyResult in dependenciesResults) {
        signer.Digest(dependencyResult.TaskSignature);
      }

      signer.Digest("Salts:");

      foreach (var salt in salts) {
        signer.Digest(salt);
      }

      return signer.Finish().HexSignature;
    }

    private static Mock<IBuildTask> BareBuildTask(string taskName, IEnumerable<IBuildTask> dependencies) {
      var fileGeneratorMock = new Mock<IBuildTask>();
      fileGeneratorMock.SetupGet(f => f.Name).Returns(taskName);
      fileGeneratorMock.SetupGet(f => f.Dependencies).Returns(dependencies.ToImmutableArray);
      return fileGeneratorMock;
    }
  }
}