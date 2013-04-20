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
using System.IO;

namespace Tests
{
  [TestClass]
  public class BitWriterTests
  {
    [TestMethod]
    public void BitWriterEmptyTest()
    {
      using (MemoryStream ms = new MemoryStream()) {
        BitWriter bw = new BitWriter()
        {
          Output = ms,
          CodeLength = 4,
        };
        bw.FlushBits();
        bw.FlushBytes();
        byte[] bytes = ms.ToArray();
        Assert.AreEqual(0, bytes.Length);
      }
    }

    [TestMethod]
    public void BitWriterOneUnitTest()
    {
      using (MemoryStream ms = new MemoryStream()) {
        BitWriter bw = new BitWriter()
        {
          Output = ms,
          CodeLength = 4,
        };
        bw.WriteBits(5);
        bw.FlushBits();
        bw.FlushBytes();
        byte[] bytes = ms.ToArray();
        Assert.AreEqual(2, bytes.Length);
        Assert.AreEqual(0x01, bytes[0]);
        Assert.AreEqual(0x05, bytes[1]);
      }
    }

    [TestMethod]
    public void BitWriterTwoUnitsTest()
    {
      using (MemoryStream ms = new MemoryStream()) {
        BitWriter bw = new BitWriter()
        {
          Output = ms,
          CodeLength = 4,
        };
        bw.WriteBits(5);
        bw.WriteBits(7);
        bw.FlushBits();
        bw.FlushBytes();
        byte[] bytes = ms.ToArray();
        Assert.AreEqual(2, bytes.Length);
        Assert.AreEqual(0x01, bytes[0]);
        Assert.AreEqual(0x75, bytes[1]);
      }
    }

    [TestMethod]
    public void BitWriterThreeUnitsTest()
    {
      using (MemoryStream ms = new MemoryStream()) {
        BitWriter bw = new BitWriter()
        {
          Output = ms,
          CodeLength = 4,
        };
        bw.WriteBits(5);
        bw.WriteBits(7);
        bw.WriteBits(1);
        bw.FlushBits();
        bw.FlushBytes();
        byte[] bytes = ms.ToArray();
        Assert.AreEqual(3, bytes.Length);
        Assert.AreEqual(0x02, bytes[0]);
        Assert.AreEqual(0x75, bytes[1]);
        Assert.AreEqual(0x01, bytes[2]);
      }
    }

    [TestMethod]
    public void BitWriterUnalignedTest()
    {
      using (MemoryStream ms = new MemoryStream()) {
        BitWriter bw = new BitWriter()
        {
          Output = ms,
          CodeLength = 5,
        };
        bw.WriteBits(5);
        bw.WriteBits(7);
        bw.WriteBits(3);
        bw.WriteBits(7);
        bw.FlushBits();
        bw.FlushBytes();
        byte[] bytes = ms.ToArray();
        Assert.AreEqual(4, bytes.Length);
        Assert.AreEqual(0x03, bytes[0]);
        Assert.AreEqual(0xE5, bytes[1]);
        Assert.AreEqual(0x8C, bytes[2]);
        Assert.AreEqual(0x03, bytes[3]);
      }
    }

    [TestMethod]
    public void BitWriterWideTest()
    {
      using (MemoryStream ms = new MemoryStream()) {
        BitWriter bw = new BitWriter()
        {
          Output = ms,
          CodeLength = 12,
        };
        bw.WriteBits(5);
        bw.WriteBits(7);
        bw.WriteBits(3);
        bw.WriteBits(7);
        bw.FlushBits();
        bw.FlushBytes();
        byte[] bytes = ms.ToArray();
        Assert.AreEqual(7, bytes.Length);
        Assert.AreEqual(0x06, bytes[0]);
        Assert.AreEqual(0x05, bytes[1]);
        Assert.AreEqual(0x70, bytes[2]);
        Assert.AreEqual(0x00, bytes[3]);
        Assert.AreEqual(0x03, bytes[4]);
        Assert.AreEqual(0x70, bytes[5]);
        Assert.AreEqual(0x00, bytes[6]);
      }
    }

    [TestMethod]
    public void BitWriterWpTest()
    {
      using (MemoryStream ms = new MemoryStream()) {
        BitWriter bw = new BitWriter()
        {
          Output = ms,
          CodeLength = 9,
        };
        // http://en.wikipedia.org/wiki/Graphics_Interchange_Format
        bw.WriteBits(0x100);
        bw.WriteBits(0x028);
        bw.WriteBits(0x0FF);
        bw.WriteBits(0x103);
        bw.WriteBits(0x102);
        bw.WriteBits(0x103);
        bw.WriteBits(0x106);
        bw.WriteBits(0x107);
        bw.WriteBits(0x101);
        bw.FlushBits();
        bw.FlushBytes();
        byte[] bytes = ms.ToArray();
        Assert.AreEqual(12, bytes.Length);
        Assert.AreEqual(0x0B, bytes[0]);
        Assert.AreEqual(0x00, bytes[1]);
        Assert.AreEqual(0x51, bytes[2]);
        Assert.AreEqual(0xFC, bytes[3]);
        Assert.AreEqual(0x1B, bytes[4]);
        Assert.AreEqual(0x28, bytes[5]);
        Assert.AreEqual(0x70, bytes[6]);
        Assert.AreEqual(0xA0, bytes[7]);
        Assert.AreEqual(0xC1, bytes[8]);
        Assert.AreEqual(0x83, bytes[9]);
        Assert.AreEqual(0x01, bytes[10]);
        Assert.AreEqual(0x01, bytes[11]);
      }
    }

  }
}