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
        .WithSignature(deps => GetTaskSigner(taskName, nameof(FileGenerator), deps)
                         .Digest("FileName:")
                         .Digest(fileName)
                         .Digest("Parameters:")
                         .Digest(fileContents)
                         .Finish()
                         .HexSignature)
        .WithExecuteAction(ctx => File.WriteAllText(Path.Combine(ctx.OutputDir, fileName), fileContents));

    public static Mock<IBuildTask> ActionBuildTask(string taskName, Action buildAction,
                                                   params IBuildTask[] dependencies)
      => BareBuildTask(taskName, dependencies)
        .WithSignature(deps => GetTaskSigner(taskName, nameof(ActionBuildTask), deps).Finish().HexSignature)
        .WithExecuteAction(ctx => buildAction());

    public static Mock<IBuildTask> NoOpBuildTask(string taskName, params IBuildTask[] dependencies)
      => BareBuildTask(taskName, dependencies)
        .WithSignature(deps => GetTaskSigner(taskName, nameof(ActionBuildTask), deps).Finish().HexSignature);

    public static Mock<IBuildTask> WithExecuteAction(this Mock<IBuildTask> buildTaskMock,
                                                     Action<BuildTaskContext> executeAction) {
      buildTaskMock.Setup(f => f.Execute(It.IsAny<BuildTaskContext>())).Callback(executeAction);
      return buildTaskMock;
    }

    public static Mock<IBuildTask> WithSignature(this Mock<IBuildTask> buildTaskMock, string signature)
      => buildTaskMock.WithSignature(deps => signature);

    public static Mock<IBuildTask> WithSignature(this Mock<IBuildTask> buildTaskMock,
                                                 Func<ImmutableArray<BuildTaskResult>, string> signature) {
      buildTaskMock.Setup(f => f.GetSignature(It.IsAny<ImmutableArray<BuildTaskResult>>()))
                   .Returns(signature);
      return buildTaskMock;
    }

    private static Sha256Signer GetTaskSigner(string taskName, string taskType,
                                              ImmutableArray<BuildTaskResult> dependenciesResults) {
      var signatureBytes = new Sha256Signer().Digest("Type:")
                                             .Digest(taskType)
                                             .Digest("Name:")
                                             .Digest(taskName)
                                             .Digest("Dependencies:");

      foreach (var dependencyResult in dependenciesResults) {
        signatureBytes.Digest(dependencyResult.TaskSignature);
      }

      return signatureBytes;
    }

    private static Mock<IBuildTask> BareBuildTask(string taskName, IEnumerable<IBuildTask> dependencies) {
      var fileGeneratorMock = new Mock<IBuildTask>();
      fileGeneratorMock.SetupGet(f => f.Name).Returns(taskName);
      fileGeneratorMock.SetupGet(f => f.Dependencies).Returns(dependencies.ToImmutableArray);
      return fileGeneratorMock;
    }
  }
}