using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using static System.Math;
using static System.Text.Encoding;

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

    /// <param name="buffer">this array will be used as a buffer of bytes that will be read from files or strings
    /// and passed on to the hashing algorithm. Because this signer uses UTF-32 when converting strings to bytes,
    /// this buffer must be at least 4-bytes long.</param>
    /// <exception cref="ArgumentException">this exception is thrown when the buffer is less than 4 bytes
    /// long.</exception>
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

    /// <summary>
    /// Converts the string into UTF-32 bytes and passes them to the hashing algorithm.
    /// </summary>
    /// <param name="str">the string to add to the signature by passing it to the hashing algorithm.</param>
    /// <returns>this task signer.</returns>
    public TaskSigner Digest(string str) {
      var blockMaxCharCount = buffer.Length >> 2;
      var strLength = str.Length;
      for (int charsDigested = 0; charsDigested < strLength; charsDigested += blockMaxCharCount) {
        var bytes = UTF32.GetBytes(str, charsDigested, Min(blockMaxCharCount, strLength - charsDigested), buffer, 0);
        hashAlgorithm.TransformBlock(buffer, 0, bytes, null, 0);
      }
      return this;
    }

    /// <param name="bytes">these byte array will be added to the signature by passing it to the hash algorith.</param>
    /// <returns>this task signer.</returns>
    public TaskSigner Digest(byte[] bytes) {
      hashAlgorithm.TransformBlock(bytes, 0, bytes.Length, null, 0);
      return this;
    }

    /// <param name="sources">these files will be added to the signature.</param>
    /// <returns>this task signer.</returns>
    /// <remarks>
    ///   This method digests each file with the <see cref="DigestSource"/> method.
    /// </remarks>
    public TaskSigner DigestSources(IEnumerable<string> sources) {
      foreach (var source in sources) {
        DigestSource(source);
      }
      return this;
    }

    /// <param name="file">the path to the file to be added to the signature.</param>
    /// <returns>this task signer.</returns>
    /// <remarks>This method first digests the path of the file (the <paramref name="file"/> string) and then
    /// it digests the contents of the file.</remarks>
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

    /// <summary>
    /// Finalizes the signature and makes it available in the <see cref="Signature"/> property.
    /// </summary>
    /// <returns>this task signer.</returns>
    public TaskSigner Finish() {
      hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
      hash = hashAlgorithm.Hash;
      return this;
    }

    /// <summary>
    /// The signature of the task.
    /// </summary>
    /// <exception cref="Exception">this is thrown if the <see cref="Finish"/> method hasn't been called
    /// yet.</exception>
    public byte[] Signature {
      get {
        if (hash == null) {
          throw new Exception($"The hash has not yet been calculated. Call '{nameof(Finish)}' to calculate the hash.");
        }
        return hash;
      }
    }
  }
}