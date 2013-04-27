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

namespace Defect
{
  public partial class GIF
  {

    #region Writing Images

    /// <summary>
    /// Write the header and logical screen descriptor for a GIF.
    /// </summary>
    /// <remarks><para>Before calling this, you must have set the GIF parameters.
    /// Do not change them after calling it.</para></remarks>
    public void Begin()
    {
      if (CurrentState != State.Initial) {
        throw new InvalidOperationException("GIF.Begin must be called only once");
      }
      CurrentState = State.Broken;
      WriteVersion();
      WriteLogicalScreenDescriptor();
      if (GlobalColorTable != null) {
        WriteColorTable(GlobalColorTable);
      }
      CurrentState = State.Open;
    }

    /// <summary>
    /// Write a single image
    /// </summary>
    /// <param name="image">Image to encode</param>
    /// <remarks><code>Begin()</code> must be called first.  This may be called any number of times.</remarks>
    public void WriteImage(Image image)
    {
      if (AutoClose && CurrentState == State.Closed) {
        Reopen();
      }
      if (CurrentState != State.Open) {
        throw new InvalidOperationException("GIF.WriteImage must be called after GIF.Begin and before GIF.End");
      }
      CurrentState = State.Broken;
      WriteGraphicControlExtension(image);
      WriteImageDescriptor(image);
      if (image.LocalColorTable != null) {
        WriteColorTable(image.LocalColorTable);
      }
      WriteImageData(image);
      CurrentState = State.Open;
      if (AutoClose) {
        End();
      }
    }

    /// <summary>
    /// Complete the GIF
    /// </summary>
    /// <remarks><code>Begin()</code> must be called first.
    /// This must be called once at the end of the GIF unless auto-close mode is set.</remarks>
    public void End()
    {
      if (CurrentState != State.Open) {
        throw new InvalidOperationException("GIF.End must only be called after GIF.Begin");
      }
      CurrentState = State.Broken;
      WriteTrailer();
      CurrentState = State.Closed;
    }

    public void Reopen()
    {
      if (CurrentState != State.Closed) {
        throw new InvalidOperationException("GIF.Reopen must only be called after GIF.End");
      }
      CurrentState = State.Broken;
      Output.SetLength(Output.Length - 1);
      CurrentState = State.Open;
    }

    #endregion

    #region Low Level Utilities

    private void WriteString(string s)
    {
      foreach (char c in s) {
        WriteByte((byte)c);
      }
    }

    private void WriteByte(byte b)
    {
      if (Debug) {
        Console.Error.WriteLine("   byte: {0:X2}", b);
      }
      Output.WriteByte(b);
    }

    private void WriteByte(int b)
    {
      WriteByte((byte)b);
    }

    private void WriteShort(int n)
    {
      WriteByte(n);
      WriteByte(n >> 8);
    }

    #endregion

    #region Data Stream Components

    void WriteVersion()
    {
      if (Debug) {
        Console.Error.WriteLine("Writing signature");
      }
      WriteString(Header + Version89a);
    }

    private void WriteLogicalScreenDescriptor()
    {
      if (Debug) {
        Console.Error.WriteLine("Writing logical screen descriptor");
      }
      WriteShort(ScreenWidth);
      WriteShort(ScreenHeight);
      int packedFields = 0;
      if (GlobalColorTable != null) {
        packedFields |= 128 | (GlobalColorTable.BitSize() - 1);
        if (GlobalColorTable.Ordered) {
          packedFields |= 8;
        }
      }
      packedFields |= (ColorResolution - 1) << 4;
      WriteByte(packedFields);
      if (GlobalColorTable != null) {
        WriteByte(GlobalColorTable.BackgroundColorIndex);
      }
      else {
        WriteByte(0);
      }
      if (PixelAspectRatio != 0) {
        WriteByte((int)(64 * PixelAspectRatio - 15));
      }
      else {
        WriteByte(0);
      }
    }

    private void WriteColorTable(ColorTable table)
    {
      int entries = 1 << table.BitSize();
      int n;
      for (n = 0; n < table.Table.Length; ++n) {
        WriteByte(table.Table[n].R);
        WriteByte(table.Table[n].G);
        WriteByte(table.Table[n].B);
      }
      for (; n < entries; ++n) {
        WriteByte(0);
        WriteByte(0);
        WriteByte(0);
      }
    }

    private void WriteGraphicControlExtension(Image image)
    {
      if (Debug) {
        Console.Error.WriteLine("Writing graphic control extension");
      }
      WriteByte(ExtensionIntroducer);
      WriteByte(GraphicControlExtension);
      WriteByte(GraphicControlExtensionSize);
      int packedFields = 0;
      if (image.TransparencyIndex != -1) {
        packedFields |= 1;
      }
      packedFields |= (int)image.Disposal << 2;
      // User input is not supported.
      WriteByte(packedFields);
      WriteShort(image.DelayCentiSeconds);
      if (image.TransparencyIndex != -1) {
        WriteByte(image.TransparencyIndex);
      }
      else {
        WriteByte(0);
      }
      WriteByte(0);
    }

    private void WriteImageDescriptor(Image image)
    {
      if (Debug) {
        Console.Error.WriteLine("Writing image descriptor");
      }
      WriteByte(ImageDescriptor);
      WriteShort(image.X);
      WriteShort(image.Y);
      WriteShort(image.Width > 0 ? image.Width : ScreenWidth);
      WriteShort(image.Height > 0 ? image.Height : ScreenHeight);
      int packedFields = 0;
      if (image.LocalColorTable != null) {
        packedFields |= 128 + (image.LocalColorTable.BitSize() - 1);
        if (image.LocalColorTable.Ordered) {
          packedFields |= 32;
        }
      }
      // Interlacing is not supported.
      WriteByte(packedFields);
    }

    private void WriteImageData(Image image)
    {
      // Figure out which color table is in use and deduce the code size
      ColorTable colorTable = image.LocalColorTable ?? GlobalColorTable;
      int colorTableBitSize = colorTable.BitSize();
      int codeSize = (colorTableBitSize == 1 ? 2 : colorTableBitSize);
      if (Debug) {
        Console.Error.WriteLine("Writing code size {0} bits", codeSize);
      }
      WriteByte(codeSize);
      // Initial code length is one more than code size, to accomodate the
      // Clear and End codes.
      BitWriter writer = new BitWriter()
      {
        Output = Output,
        CodeLength = 1 + codeSize,
        Debug = this.Debug,
      };
      // Initialize the prefix table with matches for all single-unit codes
      ForwardPrefixTable prefixes = new ForwardPrefixTable();
      for (int n = 0; n < (1 << codeSize); ++n) {
        prefixes.Add(n, -1, (byte)n);
      }
      int minCode = (1 << codeSize) + 2;
      int nextCode = minCode;
      // Start with a Clear.  (Pointless - the decoder knows perfectly well what
      // the initial state is.)
      if (Debug) {
        Console.Error.WriteLine("Bit-writing clear marker {0}", 1 << codeSize);
      }
      writer.WriteBits(1 << codeSize);
      int pos = 0;
      if (Debug) {
        Console.Error.WriteLine("Bit-writing {0} pixels of image data", image.ImageData.Length);
      }
      while (pos < image.ImageData.Length) {
        // Find the longest known prefix that matches this point in the image data
        int bestLength;
        int bestCode = prefixes.Find(image.ImageData, pos, out bestLength);
        // Output the best code we found
        writer.WriteBits(bestCode);
        // Add a new dictionary entry
        if (nextCode < 4096 && pos + bestLength < image.ImageData.Length) {
          if (Debug) {
            Console.Error.WriteLine("Define code {0:X} as code {1:X} + byte {2:X2}", nextCode, bestCode, image.ImageData[pos + bestLength]);
          }
          prefixes.Add(nextCode, bestCode, image.ImageData[pos + bestLength]);
          // Update the output bit size
          if (nextCode >= (1 << writer.CodeLength)) {
            ++writer.CodeLength;
          }
          ++nextCode;
        }
        pos += bestLength;
      }
      // Finish with an End.  (Also pointless - the decoder knows what size the image data should be.)
      if (Debug) {
        Console.Error.WriteLine("Bit-writing end marker {0}", (1 << codeSize) + 1);
      }
      writer.WriteBits((1 << codeSize) + 1);
      if (Debug) {
        Console.Error.WriteLine("Flushing bits");
      }
      writer.FlushBits();
      if (Debug) {
        Console.Error.WriteLine("Flushing bytes");
      }
      writer.FlushBytes();
      // A zero-sized sub-block terminates.
      if (Debug) {
        Console.Error.WriteLine("Writing image terminator");
      }
      WriteByte(0);
    }

    void WriteTrailer()
    {
      if (Debug) {
        Console.Error.WriteLine("Writing trailer");
      }
      WriteByte(Trailer);
    }

    #endregion

  }
}
