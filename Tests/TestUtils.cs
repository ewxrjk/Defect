using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tests
{
  static class TestUtils
  {
    static public void AreEqual(byte[] expected, byte[] got, string description) {
      for (int i = 0; i < Math.Min(expected.Length, got.Length); ++i) {
        Assert.AreEqual(expected[i], got[i],
                        "{0}: index {1}: expected 0x{2:X2} got 0x{3:X2}",
                        description, i, expected[i], got[i]);
      }
      Assert.AreEqual(expected.Length, got.Length,
                      "{0}: expected length {1} got {2}",
                      description, expected.Length, got.Length);
    }

  }
}
