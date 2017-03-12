using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Moq;

namespace Bud {
  public static class MockBuildTasks {
    public static Mock<IBuildTask> FileGenerator(string taskName, string fileName, string fileContents,
                                                 params IBuildTask[] dependencies)
      => BareBuildTask(taskName, dependencies)
        .WithStandardSignature("TaskName:", taskName, "TaskType:", nameof(FileGenerator), "FileName:", fileName,
                               "Parameters:", fileContents)
        .WithExecuteAction(ctx => File.WriteAllText(Path.Combine(ctx.OutputDir, fileName), fileContents));

    public static Mock<IBuildTask> ActionBuildTask(string taskName, Action buildAction,
                                                   params IBuildTask[] dependencies)
      => BareBuildTask(taskName, dependencies)
        .WithStandardSignature("TaskName:", taskName, "TaskType:", nameof(ActionBuildTask))
        .WithExecuteAction(ctx => buildAction());

    public static Mock<IBuildTask> NoOpBuildTask(string taskName, params IBuildTask[] dependencies)
      => BareBuildTask(taskName, dependencies)
        .WithStandardSignature("TaskName:", taskName, "TaskType:", nameof(NoOpBuildTask));

    public static Mock<IBuildTask> WithExecuteAction(this Mock<IBuildTask> buildTaskMock,
                                                     Action<BuildTaskContext> executeAction) {
      buildTaskMock.Setup(f => f.Execute(It.IsAny<BuildTaskContext>())).Callback(executeAction);
      return buildTaskMock;
    }

    public static Mock<IBuildTask> WithStandardSignature(this Mock<IBuildTask> buildTaskMock, params string[] salts)
      => WithSignature(buildTaskMock, deps => GetTaskSigner(deps, salts).Finish().HexSignature);

    public static Mock<IBuildTask> WithSignature(this Mock<IBuildTask> buildTaskMock, string signature)
      => buildTaskMock.WithSignature(deps => signature);

    public static Mock<IBuildTask> WithSignature(this Mock<IBuildTask> buildTaskMock,
                                                 Func<ImmutableArray<BuildTaskResult>, string> signature) {
      buildTaskMock.Setup(f => f.GetSignature(It.IsAny<ImmutableArray<BuildTaskResult>>()))
                   .Returns(signature);
      return buildTaskMock;
    }

    private static Sha256Signer GetTaskSigner(ImmutableArray<BuildTaskResult> dependenciesResults,
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

      return signer;
    }

    private static Mock<IBuildTask> BareBuildTask(string taskName, IEnumerable<IBuildTask> dependencies) {
      var fileGeneratorMock = new Mock<IBuildTask>();
      fileGeneratorMock.SetupGet(f => f.Name).Returns(taskName);
      fileGeneratorMock.SetupGet(f => f.Dependencies).Returns(dependencies.ToImmutableArray);
      return fileGeneratorMock;
    }
  }
}