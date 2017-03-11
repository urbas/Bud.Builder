using System.Collections.Immutable;
using System.IO;
using Moq;

namespace Bud {
  public static class MockBuildTasks {
    public static Mock<IBuildTask> FileGenerator(string taskName, string fileName, string fileContents,
                                                 params IBuildTask[] dependencies) {
      var fileGeneratorMock = new Mock<IBuildTask>();

      fileGeneratorMock.SetupGet(f => f.Name).Returns(taskName);

      fileGeneratorMock.SetupGet(f => f.Dependencies).Returns(dependencies.ToImmutableArray);

      fileGeneratorMock.Setup(f => f.Execute(It.IsAny<string>()))
                       .Callback((string buildDir) => {
                         File.WriteAllText(Path.Combine(buildDir, fileName), fileContents);
                       });


      fileGeneratorMock.Setup(f => f.GetSignature(It.IsAny<ImmutableArray<BuildTaskResult>>()))
                       .Returns((ImmutableArray<BuildTaskResult> dependenciesResults) => {
                         var signatureBytes = new Sha256Signer().Digest("Type:")
                                                                .Digest("MockFileGenerator")
                                                                .Digest("Name:")
                                                                .Digest(fileName)
                                                                .Digest("Parameters:")
                                                                .Digest(fileContents);

                         signatureBytes.Digest("Dependencies:");
                         foreach (var dependencyResult in dependenciesResults) {
                           signatureBytes.Digest(dependencyResult.TaskSignature);
                         }

                         return HexUtils.ToHexStringFromBytes(signatureBytes.Finish().Signature);
                       });

      return fileGeneratorMock;
    }
  }
}