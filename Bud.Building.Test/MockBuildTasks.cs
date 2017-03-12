using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Moq;

namespace Bud {
  public static class MockBuildTasks {
    public static Mock<IBuildTask> FileGenerator(string taskName, string fileName, string fileContents,
                                                 params IBuildTask[] dependencies) {
      var fileGeneratorMock = BareBuildTask(taskName, dependencies);

      fileGeneratorMock.Setup(f => f.Execute(It.IsAny<BuildTaskContext>()))
                       .Callback((BuildTaskContext ctx) => {
                         File.WriteAllText(Path.Combine(ctx.OutputDir, fileName), fileContents);
                       });


      fileGeneratorMock.Setup(f => f.GetSignature(It.IsAny<ImmutableArray<BuildTaskResult>>()))
                       .Returns((ImmutableArray<BuildTaskResult> dependenciesResults) =>
                                  GetTaskSigner(taskName, nameof(FileGenerator), dependenciesResults)
                                    .Digest("FileName:")
                                    .Digest(fileName)
                                    .Digest("Parameters:")
                                    .Digest(fileContents)
                                    .Finish()
                                    .HexSignature);

      return fileGeneratorMock;
    }

    public static Mock<IBuildTask> ActionBuildTask(string taskName, Action buildAction,
                                                   params IBuildTask[] dependencies) {
      var fileGeneratorMock = BareBuildTask(taskName, dependencies);

      fileGeneratorMock.Setup(f => f.Execute(It.IsAny<BuildTaskContext>()))
                       .Callback((BuildTaskContext ctx) => buildAction());


      fileGeneratorMock.Setup(f => f.GetSignature(It.IsAny<ImmutableArray<BuildTaskResult>>()))
                       .Returns((ImmutableArray<BuildTaskResult> dependenciesResults) =>
                                  GetTaskSigner(taskName, nameof(ActionBuildTask), dependenciesResults)
                                    .Finish()
                                    .HexSignature);

      return fileGeneratorMock;
    }

    public static Mock<IBuildTask> NoOpBuildTask(string taskName, params IBuildTask[] dependencies) {
      var fileGeneratorMock = BareBuildTask(taskName, dependencies);


      fileGeneratorMock.Setup(f => f.GetSignature(It.IsAny<ImmutableArray<BuildTaskResult>>()))
                       .Returns((ImmutableArray<BuildTaskResult> dependenciesResults) =>
                                  GetTaskSigner(taskName, nameof(ActionBuildTask), dependenciesResults)
                                    .Finish()
                                    .HexSignature);

      return fileGeneratorMock;
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