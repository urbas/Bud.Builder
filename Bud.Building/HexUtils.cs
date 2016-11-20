using System;
using System.Collections.Generic;

namespace Bud {
  /// <summary>
  /// This class contains static methods that convert between hexadecimal strings and byte arrays.
  /// </summary>
  public static class HexUtils {
    /// <summary>
    /// Converts a string of hexadecimal digits into an array of bytes.
    ///
    /// <para>This method is case-insensitive.</para>
    /// </summary>
    /// <param name="hex">the string of hexadecimal digits to convert to bytes.</param>
    /// <returns>an array of bytes where every byte corresponds to a pair of hexadecimal digits in the given
    /// <paramref name="hex"/> string.</returns>
    /// <exception cref="ArgumentNullException">this exception is thrown if the <paramref name="hex"/> parameter is
    /// null.</exception>
    /// <exception cref="ArgumentException">this exception is thrown if the length of the given <paramref name="hex"/>
    /// string is odd.</exception>
    public static byte[] ToBytesFromHexString(string hex) {
      if (hex == null) {
        throw new ArgumentNullException(nameof(hex), "Cannot convert a null string.");
      }

      if ((hex.Length & 1) == 1) {
        throw new ArgumentException("The given string has an odd length. Hex strings must be of even length.",
                                    nameof(hex));
      }
      var numberOfBytes = hex.Length >> 1;
      var arr = new byte[numberOfBytes];
      for (int i = 0; i < numberOfBytes; ++i) {
        var hexDigitIndex = i << 1;
        arr[i] = (byte) ((ToNibbleFromHexDigit(hex[hexDigitIndex]) << 4) + ToNibbleFromHexDigit(hex[hexDigitIndex + 1]));
      }
      return arr;
    }

    /// <summary>
    /// Converts the given byte array to a string of hexadecimal digits.
    ///
    /// <para>This method produces an upper-cased string.</para>
    /// </summary>
    /// <param name="bytes">the bytes to convert to a string of hexadecimal digits.</param>
    /// <returns>a string of hexadecimal digits where every pair of charaters corresponds to an element in the
    /// <paramref name="bytes"/> array.</returns>
    /// <exception cref="ArgumentNullException">this exception is thrown is the given <paramref name="bytes"/> array
    /// is <c>null</c>.</exception>
    public static string ToHexStringFromBytes(IReadOnlyList<byte> bytes) {
      if (bytes == null) {
        throw new ArgumentNullException(nameof(bytes), "Cannot convert a null array of bytes.");
      }

      var hexDigits = new char[bytes.Count * 2];

      for (int byteIdx = 0; byteIdx < bytes.Count; ++byteIdx) {
        var hexxDigitIdx = byteIdx << 1;
        hexDigits[hexxDigitIdx] = ToHexDigitFromNibble((byte) (bytes[byteIdx] >> 4));
        hexDigits[hexxDigitIdx + 1] = ToHexDigitFromNibble((byte) (bytes[byteIdx] & 0x0F));
      }

      return new string(hexDigits);
    }

    private static int ToNibbleFromHexDigit(char hex) {
      int val = hex;
      var nibble = val - (val < 58 ? 48 : (val < 97 ? 55 : 87));

      if (nibble < 0 || nibble > 0xf) {
        throw new ArgumentException($"The character '{hex}' is not a valid hexadecimal digit. " +
                                    $"Allowed characters: 0-9, a-f, A-F.");
      }
      return nibble;
    }

    private static char ToHexDigitFromNibble(byte nibble) => (char) (nibble > 9 ? nibble - 10 + 'A' : nibble + '0');
  }
}