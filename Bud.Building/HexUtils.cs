using System;
using System.Collections.Generic;

namespace Bud {
  /// <summary>
  /// This class contains static methods that convert between hexadecimal strings and byte arrays.
  /// </summary>
  public static class HexUtils {
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

    public static int ToNibbleFromHexDigit(char hex) {
      int val = hex;
      var nibble = val - (val < 58 ? 48 : (val < 97 ? 55 : 87));

      Console.WriteLine($"Hex: {hex}, nibble: {nibble}");

      if (nibble < 0 || nibble > 0xf) {
        throw new ArgumentException($"The character '{hex}' is not a valid hexadecimal digit. " +
                                    $"Allowed characters: 0-9, a-f, A-F.");
      }
      return nibble;
    }

    public static string ToHexStringFromBytes(IReadOnlyList<byte> bytes) {
      var hexDigits = new char[bytes.Count * 2];

      for (int byteIdx = 0; byteIdx < bytes.Count; ++byteIdx) {
        var hexxDigitIdx = byteIdx << 1;
        hexDigits[hexxDigitIdx] = ToHexDigitFromNibble((byte) (bytes[byteIdx] >> 4));
        hexDigits[hexxDigitIdx + 1] = ToHexDigitFromNibble((byte) (bytes[byteIdx] & 0x0F));
      }

      return new string(hexDigits);
    }

    public static char ToHexDigitFromNibble(byte nibble) => (char) (nibble > 9 ? nibble - 10 + 'A' : nibble + '0');
  }
}