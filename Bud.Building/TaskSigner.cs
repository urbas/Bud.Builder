using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using static System.Math;

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
    private static readonly UTF32Encoding Encoding = new UTF32Encoding(bigEndian: false, byteOrderMark: false);
    private byte[] hash;
    private readonly SHA256 hashAlgorithm;
    private readonly byte[] buffer;

    public TaskSigner(byte[] buffer = null) {
      if (buffer == null) {
        this.buffer = new byte[1 << 15];
      } else {
        if (buffer.Length < 4) {
          throw new ArgumentException("The buffer must have at least 4 bytes.", nameof(buffer));
        }
        this.buffer = buffer;
      }
      hashAlgorithm = SHA256.Create();
      hashAlgorithm.Initialize();
    }

    public TaskSigner Digest(string str) {
      var blockMaxCharCount = buffer.Length >> 2;
      var strLength = str.Length;
      for (int charsDigested = 0; charsDigested < strLength; charsDigested += blockMaxCharCount) {
        var bytes = Encoding.GetBytes(str, charsDigested, Min(blockMaxCharCount, strLength - charsDigested), buffer, 0);
        hashAlgorithm.TransformBlock(buffer, 0, bytes, null, 0);
      }
      return this;
    }

    public TaskSigner DigestSources(IEnumerable<string> sources) {
      foreach (var source in sources) {
        DigestSource(source);
      }
      return this;
    }

    public TaskSigner DigestSource(string file) {
      Digest(file);
      using (var fileStream = File.OpenRead(file)) {
        int readBytes;
        do {
          readBytes = fileStream.Read(buffer, 0, buffer.Length);
          hashAlgorithm.TransformBlock(buffer, 0, readBytes, buffer, 0);
        } while (readBytes == buffer.Length);
      }
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
  }
}