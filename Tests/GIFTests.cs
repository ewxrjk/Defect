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
using System.Drawing;
using System.IO;

namespace Tests
{
  [TestClass]
  public class GIFTests
  {
    [TestMethod]
    public void EmptyGifTest()
    {
      using (MemoryStream stream = new MemoryStream()) {
        GIF gif = new GIF()
        {
          Output = stream,
          ScreenWidth = 0,
          ScreenHeight = 0,
          GlobalColorTable = new GIF.ColorTable()
          {
            Table = new Color[2] { Color.White, Color.Black }
          }
        };
        gif.Begin();
        gif.WriteImage(new GIF.Image()
        {
          ImageData = new byte[0],
        });
        gif.End();
        int codeSize = 2;
        int clear = 1 << codeSize;
        int end = 1 + clear;
        TestUtils.AreEqual(new byte[] {
          // 0: Signature
          (byte)'G', (byte)'I', (byte)'F',(byte)'8', (byte)'9', (byte)'a',
          // 6: Logical screen descriptor
          0, 0,
          0, 0,
          0xF0,
          0,
          49,
          // 13: Global Color Table
          0xFF, 0xFF, 0xFF,
          0, 0, 0,
          // 19: Graphic control extension
          0x21, 0xF9, 0x04,
          1 << 2, 0, 0, 0,
          0,
          // 27: Image descriptor
          0x2C,
          0, 0,
          0, 0,
          0, 0,
          0, 0,
          0,
          // 37: Image data
          // 37: Minimum code size
          2,
          // 38: Data block
          1,
          (byte)(clear
                 | (end << 3)),
          // 40: Block terminator
          0,
          // 41: Trailer
          0x3B
        }, stream.ToArray(), "EmptyGifTest");
      }
    }

    [TestMethod]
    public void TinyGifTest()
    {
      using (MemoryStream stream = new MemoryStream()) {
        GIF gif = new GIF()
        {
          Output = stream,
          ScreenWidth = 1,
          ScreenHeight = 1,
          GlobalColorTable = new GIF.ColorTable()
          {
            Table = new Color[2] { Color.White, Color.Black }
          }
        };
        gif.Begin();
        gif.WriteImage(new GIF.Image()
        {
          ImageData = new byte[] { 0 },
        });
        gif.End();
        int codeSize = 2;
        int clear = 1 << codeSize;
        int end = 1 + clear;
        byte[] got = stream.ToArray();
        byte[] expected = new byte[] {
          // 0: Signature
          (byte)'G', (byte)'I', (byte)'F',(byte)'8', (byte)'9', (byte)'a',
          // 6: Logical screen descriptor
          1, 0,
          1, 0,
          0xF0,
          0,
          49,
          // 13: Global Color Table
          0xFF, 0xFF, 0xFF,
          0, 0, 0,
          // 19: Graphic control extension
          0x21, 0xF9, 0x04,
          1 << 2, 0, 0, 0,
          0,
          // 27: Image descriptor
          0x2C,
          0, 0,
          0, 0,
          1, 0,
          1, 0,
          0,
          // 37: Image data
          // 37: Minimum code size
          2,
          // 38: Data block
          2,
          (byte)(clear
                 | 0 << 3
                 | (end << 6)),
          (byte)(end >> 2),
          // 41: Block terminator
          0,
          // 42: Trailer
          0x3B
        };
        TestUtils.AreEqual(expected, got, "TinyGifTest");
      }
    }

  }
}
