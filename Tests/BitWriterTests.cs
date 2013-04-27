// This program is © 2013 Richard Kettlewell.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY// without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Defect;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.ObjectModel;
using System.IO;

namespace Tests
{
  [TestClass]
  public class BitWriterTests
  {
    private void Check(int[] input, int CodeLength, byte[] expectBytes, bool expectTruncated)
    {
      using (MemoryStream ms = new MemoryStream()) {
        BitWriter bw = new BitWriter()
        {
          Output = ms,
          CodeLength = CodeLength,
        };
        foreach (int n in input) {
          bw.WriteBits(n);
        }
        bw.FlushBits();
        bw.FlushBytes();
        byte[] actualBytes = ms.ToArray();
        Assert.AreEqual(expectBytes.Length, actualBytes.Length, "output length");
        for (int i = 0; i < expectBytes.Length; ++i) {
          Assert.AreEqual(expectBytes[i], actualBytes[i],
                          string.Format("position {0}", i));
        }
        ms.Seek(0, SeekOrigin.Begin);
        BitReader br = new BitReader()
        {
          Input = ms,
          CodeLength = CodeLength,
        };
        int m;
        Collection<int> output = new Collection<int>();
        bool truncated = false;
        try {
          while ((m = br.ReadCode()) != -1) {
            output.Add(m);
            if (output.Count == input.Length && br.WholeCodeLeft()) {
              break;
            }
          }
        }
        catch (TruncatedInputException) {
          truncated = true;
        }
        if (!truncated) {
          br.Clear();
        }
        Assert.AreEqual(input.Length, output.Count, "bitread length");
        for(int i = 0; i < input.Length; ++i) {
          Assert.AreEqual(input[i], output[i],
                          string.Format("position {0}", i));
        }
        Assert.AreEqual(expectTruncated, truncated);
      }
    }

    [TestMethod]
    public void BitWriterEmptyTest()
    {
      Check(new int[] { }, 4, new byte[] { }, true);
    }

    [TestMethod]
    public void BitWriterOneUnitTest()
    {
      Check(new int[] { 5 }, 4, new byte[] { 0x01, 0x05 }, true);
    }

    [TestMethod]
    public void BitWriterTwoUnitsTest()
    {
      Check(new int[] { 5, 7 }, 4, new byte[] { 0x01, 0x75 }, true);
    }

    [TestMethod]
    public void BitWriterThreeUnitsTest()
    {
      Check(new int[] { 5, 7, 1 }, 4, new byte[] { 0x02, 0x75, 0x01 }, false);
    }

    [TestMethod]
    public void BitWriterUnalignedTest()
    {
      Check(new int[] { 5, 7, 3, 7 }, 5, new byte[] { 0x03, 0xE5, 0x8C, 0x03 }, true);
    }

    [TestMethod]
    public void BitWriterWideTest()
    {
      Check(new int[] { 5, 7, 3, 7 },
            12,
            new byte[] { 0x06, 0x05, 0x70, 0x00, 0x03, 0x70, 0x00 }, true);
    }

    [TestMethod]
    public void BitWriterWpTest()
    {
      // http://en.wikipedia.org/wiki/Graphics_Interchange_Format
      Check(new int[] { 0x100, 0x028, 0x0FF, 0x103, 0x102, 0x103, 0x106, 0x107, 0x101 },
            9,
            new byte[] { 0x0B, 0x00, 0x51, 0xFC, 0x1B, 0x28, 0x70, 0xA0, 0xC1, 0x83, 0x01, 0x01 },
            true);
    }

  }
}