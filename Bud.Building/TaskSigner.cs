using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Bud {
  /// <summary>
  /// Produces task signatures.
  /// </summary>
  /// <remarks>
  /// A task's signature is used to determine whether it has already been executed and whether it is up to date.
  ///
  /// <para>
  /// A task's signature is typically calculated from its input files (source files) and any configuration parameters
  /// that could influence the output files.
  /// </para>
  /// </remarks>
  public class TaskSigner {
    private byte[] hash;
    private readonly SHA256 hashAlgorithm;
    private readonly byte[] buffer;

    public TaskSigner() {
      buffer = new byte[1 << 15];
      hashAlgorithm = SHA256.Create();
      hashAlgorithm.Initialize();
    }

    public TaskSigner Digest(string str) {
      var bytes = Encoding.UTF8.GetBytes(str);
      hashAlgorithm.TransformBlock(bytes, 0, bytes.Length, null, 0);
      return this;
    }

    public TaskSigner Finish() {
      hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
      hash = hashAlgorithm.Hash;
      return this;
    }

    public byte[] Hash {
      get {
        if (hash == null) {
          throw new Exception($"The hash has not yet been calculated. Call '{nameof(Finish)}' to calculate the hash.");
        }
        return hash;
      }
    }

    public void DigestSources(IEnumerable<string> sources) {
      var buffer = new byte[1 << 15];
      var hashAlgorithm = SHA256.Create();
      hashAlgorithm.Initialize();
      foreach (var source in sources) {
        DigestSource(hashAlgorithm, source, buffer);
      }
      hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
    }

    private static void DigestSource(ICryptoTransform digest, string file, byte[] buffer) {
      using (var fileStream = File.OpenRead(file)) {
        int readBytes;
        do {
          readBytes = fileStream.Read(buffer, 0, buffer.Length);
          digest.TransformBlock(buffer, 0, readBytes, buffer, 0);
        } while (readBytes == buffer.Length);
      }
    }
  }
}