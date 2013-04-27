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
using System.Linq;

namespace Tests
{
  [TestClass]
  public class GIFTests
  {
    [TestMethod]
    public void EmptyGifTest()
    {
      using (MemoryStream stream = new MemoryStream()) {
        GIF saveGif = new GIF()
        {
          Output = stream,
          ScreenWidth = 0,
          ScreenHeight = 0,
          GlobalColorTable = new GIF.ColorTable()
          {
            Table = new Color[2] { Color.White, Color.Black }
          }
        };
        saveGif.Begin();
        GIF.Image image = new GIF.Image()
        {
          ImageData = new byte[0],
        };
        saveGif.WriteImage(image);
        saveGif.End();
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
        stream.Seek(0, SeekOrigin.Begin);
        GIF loadGIF = new GIF()
        {
          Input = stream,
        };
        loadGIF.Load();
        Assert.AreEqual(1, loadGIF.Images.Count);
        Assert.AreEqual(image.Width, loadGIF.Images[0].Width);
        Assert.AreEqual(image.Height, loadGIF.Images[0].Height);
        TestUtils.AreEqual(image.ImageData, loadGIF.Images[0].ImageData, "image data");
        Assert.AreEqual(2, loadGIF.GlobalColorTable.Table.Length);
        Assert.AreEqual(Color.White.A, loadGIF.GlobalColorTable.Table[0].A);
        Assert.AreEqual(Color.White.R, loadGIF.GlobalColorTable.Table[0].R);
        Assert.AreEqual(Color.White.G, loadGIF.GlobalColorTable.Table[0].G);
        Assert.AreEqual(Color.White.B, loadGIF.GlobalColorTable.Table[0].B);
        Assert.AreEqual(Color.Black.A, loadGIF.GlobalColorTable.Table[1].A);
        Assert.AreEqual(Color.Black.R, loadGIF.GlobalColorTable.Table[1].R);
        Assert.AreEqual(Color.Black.G, loadGIF.GlobalColorTable.Table[1].G);
        Assert.AreEqual(Color.Black.B, loadGIF.GlobalColorTable.Table[1].B);
      }
    }

    [TestMethod]
    public void TinyGifTest()
    {
      using (MemoryStream stream = new MemoryStream()) {
        GIF saveGif = new GIF()
        {
          Output = stream,
          ScreenWidth = 1,
          ScreenHeight = 1,
          GlobalColorTable = new GIF.ColorTable()
          {
            Table = new Color[2] { Color.White, Color.Black }
          }
        };
        saveGif.Begin();
        GIF.Image image = new GIF.Image()
        {
          Width = 1,
          Height = 1,
          ImageData = new byte[] { 0 },
        };
        saveGif.WriteImage(image);
        saveGif.End();
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
        stream.Seek(0, SeekOrigin.Begin);
        GIF loadGIF = new GIF()
        {
          Input = stream,
        };
        loadGIF.Load();
        Assert.AreEqual(1, loadGIF.Images.Count);
        Assert.AreEqual(image.Width, loadGIF.Images[0].Width);
        Assert.AreEqual(image.Height, loadGIF.Images[0].Height);
        TestUtils.AreEqual(image.ImageData, loadGIF.Images[0].ImageData, "image data");
      }
    }

    [TestMethod]
    public void TrickyGIFTest()
    {
      using (MemoryStream stream = new MemoryStream()) {
        GIF saveGif = new GIF()
        {
          Output = stream,
          ScreenWidth = 8,
          ScreenHeight = 8,
          GlobalColorTable = new GIF.ColorTable()
          {
            Table = new Color[4] { Color.White, Color.Black, Color.Red, Color.Green }
          }
        };
        saveGif.Begin();
        GIF.Image image = new GIF.Image()
        {
          Width = 8,
          Height = 8,
          ImageData = new byte[64],
        };
        for (int i = 0; i < 64; ++i) {
          image.ImageData[i] = (byte)(i % 4);
        }
        saveGif.WriteImage(image);
        saveGif.End();
        // TODO ideally check the byte stream is as expected
        stream.Seek(0, SeekOrigin.Begin);
        GIF loadGIF = new GIF()
        {
          Input = stream,
        };
        loadGIF.Load();
        Assert.AreEqual(1, loadGIF.Images.Count);
        Assert.AreEqual(image.Width, loadGIF.Images[0].Width);
        Assert.AreEqual(image.Height, loadGIF.Images[0].Height);
        TestUtils.AreEqual(image.ImageData, loadGIF.Images[0].ImageData, "image data");
      }
    }
  }
}
