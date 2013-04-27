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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Defect
{
  public partial class GIF
  {

    #region GIF Loading API

    /// <summary>
    /// Called for each image
    /// </summary>
    /// <remarks>The default is <c>DefaultImageHandler</c></remarks>
    public Action<Image> ImageHandler { get; set; }

    /// <summary>
    /// Images accumulated by the <c>DefaultImageHandler</c>
    /// </summary>
    public IList<Image> Images { get; set; }

    /// <summary>
    /// Read a GIF from a stream
    /// </summary>
    /// <remarks>Calls <c>ImageHandler</c> for each image found</remarks>
    public void Load()
    {
      Images = null;
      ReadHeader();
      ReadVersion();
      ReadLogicalScreenDescriptor();
      int label;
      NextImage = null;
      while ((label = ReadByte()) != Trailer) {
        switch (label) {
          case -1:
            throw new TruncatedInputException();
          case ExtensionIntroducer:
            ReadExtension();
            break;
          case ImageDescriptor:
            ReadImageDescriptor();
            ReadImageData();
            ImageHandler(NextImage);
            NextImage = null;
            break;
          default:
            throw new MalformedGIFException(string.Format("unrecognize GIF label {0} at {1}", label, Input.Position - 1));
        }
      }
    }

    /// <summary>
    /// Default image handling callback
    /// </summary>
    /// <param name="image"></param>
    public void DefaultImageHandler(Image image)
    {
      if (Images == null) {
        Images = new Collection<Image>();
      }
      Images.Add(image);
    }

    #endregion

    #region GIF Parsing

    private Image NextImage;

    private void ReadHeader()
    {
      byte[] input = ReadBytes(3);
      if (!Matches(input, Header)) {
        throw new MalformedGIFException("invalid GIF header");
      }
    }

    private void ReadVersion()
    {
      byte[] input = ReadBytes(3);
      if (!Matches(input, Version89a) && !Matches(input, Version87a)) {
        throw new MalformedGIFException("unrecognized GIF version");
      }
    }

    private void ReadLogicalScreenDescriptor()
    {
      ScreenWidth = ReadShort();
      ScreenHeight = ReadShort();
      int packedFields = ReadByte();
      int bci = ReadByte();
      int par = ReadByte();
      PixelAspectRatio = (double)par / 64 + 15;
      if ((packedFields & 128) != 0) {
        GlobalColorTable = new ColorTable()
        {
          Table = ReadColors(1 << (1 + (packedFields & 7))),
          Ordered = (packedFields & 8) != 0,
          BackgroundColorIndex = bci,
        };
      }
      else {
        GlobalColorTable = null;
      }
    }

    private void ReadExtension()
    {
      int label = ReadByte();
      switch (label) {
        case GraphicControlExtension:
          ReadGraphicControlExtension();
          break;
        default:
          int length = ReadByte();
          while (length > 0) {
            while (length > 0) {
              ReadByte();
            }
            length = ReadByte();
          }
          break;
      }
    }

    private void ReadGraphicControlExtension()
    {
      int length = ReadByte();
      if (length != 4) {
        throw new MalformedGIFException(string.Format("wrong length GIF graphic control extension at {0}", Input.Position - 1));
      }
      int packedFields = ReadByte();
      int delay = ReadShort();
      NextImage = new Image();
      NextImage.TransparencyIndex = ReadByte();
      // TODO all this should end up in the Image object
      int terminator = ReadByte();
      if (terminator != 0) {
        throw new MalformedGIFException(string.Format("invalid GIF graphic control extension terminate {0} at {1}",
                                        terminator, Input.Position - 1));
      }
    }

    private void ReadImageDescriptor()
    {
      if (NextImage == null) {
        NextImage = new Image();
      }
      NextImage.X = ReadShort();
      NextImage.Y = ReadShort();
      NextImage.Width = ReadShort();
      NextImage.Height = ReadShort();
      int packedFields = ReadByte();
      if ((packedFields & 128) != 0) {
        NextImage.LocalColorTable = new ColorTable()
        {
          Table = ReadColors(1 << (1 + (packedFields & 7))),
          Ordered = (packedFields & 64) != 0,
        };
      }
      // TODO interlace
    }

    private void ReadImageData()
    {
      NextImage.ImageData = new byte[NextImage.Width * NextImage.Height];
      ColorTable colorTable = NextImage.LocalColorTable ?? GlobalColorTable;
      int expectedCodeSize = Math.Max(colorTable.BitSize(), 2);
      int codeSize = ReadByte();
      if (codeSize != expectedCodeSize) {
        throw new MalformedGIFException(string.Format("code size {0} but expected {1} at {2}",
                                        codeSize, expectedCodeSize, Input.Position - 1));
      }
      BitReader reader = new BitReader()
      {
        Input = Input,
        CodeLength = codeSize + 1,
        Debug = this.Debug,
      };
      ReversePrefixTable prefixes = new ReversePrefixTable();
      for (int n = 0; n < colorTable.Table.Length; ++n) {
        prefixes.Add(n, (byte)n);
      }
      int clear = 1 << codeSize;
      int eoi = clear + 1;
      int nextCode = clear + 2;
      int pos = 0;
      int lastCode = -1;
      int updateCode = -1;
      for (; ; ) {
        int code = reader.ReadCode();
        if(code == clear) {
          reader.CodeLength = codeSize + 1;
          nextCode = clear + 2;
          prefixes = new ReversePrefixTable();
          for (int n = 0; n < colorTable.Table.Length; ++n) {
            prefixes.Add(n, (byte)n);
          }
          updateCode = -1;
          lastCode = -1;
          continue;
        }
        if(code == eoi) {
          reader.Clear();
          break;
        }
        if(code == -1) {
          throw new TruncatedInputException();
        }
        byte[] bytes;
        if(code == updateCode) {
          byte[] prevBytes = prefixes.Find(lastCode);
          bytes = new byte[prevBytes.Length + 1];
          Array.Copy(prevBytes, bytes, prevBytes.Length);
          bytes[prevBytes.Length] = prevBytes[0];
        } else {
          bytes = prefixes.Find(code);
          if(bytes == null) {
            throw new MalformedGIFException(string.Format("unrecognized code {0} before {1}", code, Input.Position));
          }
        }
        if (pos + bytes.Length > NextImage.ImageData.Length) {
          throw new MalformedGIFException(string.Format("code {0} at pos {1} overflows image before {2}",
                                          code, pos, Input.Position));
        }
        if(updateCode != -1) {
          prefixes.Add(updateCode, lastCode, bytes[0]);
        }
        Array.Copy(bytes, 0, NextImage.ImageData, pos, bytes.Length);
        pos += bytes.Length;
        if(nextCode < 4096) {
          updateCode = nextCode;
          lastCode = code;
          if (nextCode >= (1 << reader.CodeLength)) {
            ++reader.CodeLength;
          }
          ++nextCode;
        } else {
          updateCode = -1;
          lastCode = -1;
        }
      }
    }

    #endregion

    #region GIF Reading Utilities

    private System.Drawing.Color[] ReadColors(int ncolors)
    {
      System.Drawing.Color[] table = new System.Drawing.Color[ncolors];
      for (int n = 0; n < ncolors; ++n) {
        int r = ReadByte();
        int g = ReadByte();
        int b = ReadByte();
        table[n] = System.Drawing.Color.FromArgb(r, g, b);
      }
      return table;
    }

    private bool Matches(byte[] bytes, string text)
    {
      if (bytes.Length != text.Length) {
        return false;
      }
      for (int i = 0; i < bytes.Length; ++i) {
        if (bytes[i] != text[i]) {
          return false;
        }
      }
      return true;
    }

    #endregion

    #region Low level GIF reading

    private byte[] ReadBytes(int n)
    {
      byte[] input = new byte[n];
      if (Input.Read(input, 0, n) < n) {
        throw new TruncatedInputException();
      }
      return input;
    }

    private int ReadByte()
    {
      int b = Input.ReadByte();
      if (b < 0) {
        throw new TruncatedInputException();
      }
      return b;
    }

    private int ReadShort()
    {
      int l = ReadByte();
      int h = ReadByte();
      return l + 256 * h;
    }

    #endregion

  }
}
