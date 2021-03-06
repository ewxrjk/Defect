﻿// This program is © 2013 Richard Kettlewell.
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
using System.IO;

namespace Defect
{
  public class GIF
  {
    /// <summary>
    /// Constructor
    /// </summary>
    public GIF()
    {
      Output = null;
      ScreenWidth = 0;
      ScreenHeight = 0;
      ColorResolution = 8;
      GlobalColorTable = null;
      PixelAspectRatio = 1;
      Debug = false;
      AutoClose = false;
    }

    #region Subclasses

    /// <summary>
    /// Definition of a color table
    /// </summary>
    /// <remarks>Use for <code>GlobalColorTable</code> and per-image color tables.</remarks>
    public class ColorTable
    {
      public ColorTable()
      {
        Table = null;
        BackgroundColorIndex = 0;
        Ordered = false;
      }

      /// <summary>
      /// The color table itself.
      /// </summary>
      /// <remarks>
      /// <para>The GIF format constraints color tables to be a power of two entries long.
      /// If the supplied table does not meet this constraint then it will be padded with
      /// extra black entries.</para>
      /// <para>The maximum size for a color table is 256 entries.</para>
      /// <para>The default value is <code>null</code>.</para>
      /// </remarks>
      public System.Drawing.Color[] Table { get; set; }

      /// <summary>
      /// Index of the background color.
      /// </summary>
      /// <remarks>The default value is <code>0</code>.</remarks>
      public int BackgroundColorIndex { get; set; }

      /// <summary>
      /// Set to <code>true</code> if Table is in decreasing order of importance
      /// </summary>
      /// <remarks>The default value is <code>false</code>.</remarks>
      public bool Ordered { get; set; }

      /// <summary>
      /// Find the number of bits required to represent a color
      /// </summary>
      /// <returns>The least positive <code>n</code> such that <code>1 &lt;&lt; n</code> is sufficient to represent <code>Table</code>.</returns>
      internal int BitSize()
      {
        for (int bits = 1; bits < 8; ++bits) {
          if ((1 << bits) >= Table.Length) {
            return bits;
          }
        }
        throw new ArgumentException("invalid size for color table");
      }
    }

    /// <summary>
    /// Disposal types for an image
    /// </summary>
    public enum DisposalType
    {
      /// <summary>
      /// Decoder is not required to take any action.
      /// </summary>
      None,

      /// <summary>
      /// Image is left in place.
      /// </summary>
      DoNotDispose,

      /// <summary>
      /// Area used by the image is restored to background color.
      /// </summary>
      RestoreToBackgroundColor,

      /// <summary>
      /// Area used by the image is restored to the previous state.
      /// </summary>
      RestoreToPrevious
    };

    /// <summary>
    /// A single image within the GIF
    /// </summary>
    public class Image
    {
      public  Image()
      {
        X = 0;
        Y = 0;
        Width = 0;
        Height = 0;
        DelayCentiSeconds = 0;
        TransparencyIndex = -1;
        Disposal = DisposalType.DoNotDispose;
        LocalColorTable = null;
        ImageData = null;
      }

      /// <summary>
      /// Image X position
      /// </summary>
      /// <remarks>The default value is <code>0</code>.</remarks>
      public int X { get; set; }

      /// <summary>
      /// Image X position
      /// </summary>
      /// <remarks>The default value is <code>0</code>.</remarks>
      public int Y { get; set; }

      /// <summary>
      /// Image width in pixels
      /// </summary>
      /// <remarks>The default value is <code>0</code>.</remarks>
      public int Width { get; set; }

      /// <summary>
      /// Image height in pixels
      /// </summary>
      /// <remarks>The default value is <code>0</code>.</remarks>
      public int Height { get; set; }

      /// <summary>
      /// Delay before rendering this image
      /// </summary>
      /// <remarks>The default value is <code>0</code>.</remarks>
      public int DelayCentiSeconds { get; set; }

      /// <summary>
      /// Transparency index for this image
      /// </summary>
      /// <remarks>If set to <code>-1</code> then there is no transparency index.
      /// The default value is <code>-1</code>.</remarks>
      public int TransparencyIndex { get; set; }

      /// <summary>
      /// Disposal action for this image
      /// </summary>
      /// <remarks>The default value is <code>DisposalType.DoNotDispose</code>.</remarks>
      public DisposalType Disposal { get; set; }

      /// <summary>
      /// Local color table
      /// </summary>
      /// <remarks>The default value is <code>null</code>.</remarks>
      public ColorTable LocalColorTable { get; set; }

      /// <summary>
      /// The image data
      /// </summary>
      /// <remarks>The default value is <code>null</code>.</remarks>
      public byte[] ImageData { get; set; }
    }

    #endregion

    #region Parameters

    /// <summary>
    /// Stream which GIF is written to
    /// </summary>
    /// <remarks>The default value is <code>null</code>.</remarks>
    public Stream Output { get; set; }
    
    /// <summary>
    /// Logical screen Width in pixels
    /// </summary>
    /// <remarks>The default value is <code>0</code>.</remarks>
    public int ScreenWidth { get; set; }

    /// <summary>
    /// Logical screen height in pixels
    /// </summary>
    /// <remarks>The default value is <code>0</code>.</remarks>
    public int ScreenHeight { get; set; }

    /// <summary>
    /// Color resolution
    /// </summary>
    /// <remarks>The default value is 8.</remarks>
    public int ColorResolution { get; set; }

    /// <summary>
    /// Pixel aspect ratio
    /// </summary>
    /// <remarks>
    /// <para>Defined as pixel width divided by pixel height, or 0 for "no information".</para>
    /// <para>The default value is <code>1</code>.</para>
    /// </remarks>
    public double PixelAspectRatio { get; set; }

    /// <summary>
    /// Global color table
    /// </summary>
    /// <remarks>The default value is <code>null</code>.</remarks>
    public ColorTable GlobalColorTable { get; set; }

    /// <summary>
    /// Whether to issue debug messages
    /// </summary>
    public bool Debug { get; set; }

    /// <summary>
    /// Autoclose mode
    /// </summary>
    /// <remarks><para>If <code>true</code> then GIFs have the trailer written after each
    /// frame and removed before the next one.  There is never any need to call
    /// <code>End()</code>.  The output stream must support seeking and truncation.</para>
    /// <para>If <code>false</code> then the trailer must be written manually by calling
    /// <code>End()</code> after the last frame.  This fiddlier but likely to be more efficient,
    /// and will work on streams that do not support seeking.</para>
    /// <para>The default is <code>false</code>.</para>
    /// </remarks>
    public bool AutoClose { get; set; }

    #endregion

    #region State

    enum State
    {
      Initial,
      Open,
      Closed,
      Broken,
    };

    private State CurrentState = State.Initial;

    #endregion

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
      WriteString("GIF89a");
    }

    private void WriteLogicalScreenDescriptor()
    {
      if (Debug) {
        Console.Error.WriteLine("Writing logical screen descriptor");
      }
      WriteShort(ScreenWidth);
      WriteShort(ScreenHeight);
      int packedFields = 0;
      if(GlobalColorTable != null) {
        packedFields |= 128 | (GlobalColorTable.BitSize() - 1);
        if(GlobalColorTable.Ordered) {
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
      WriteByte(0x21);
      WriteByte(0xF9);
      WriteByte(0x04);
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
      WriteByte(0x2C);
      WriteShort(image.X);
      WriteShort(image.Y);
      WriteShort(image.Width > 0 ? image.Width : ScreenWidth);
      WriteShort(image.Height > 0 ? image.Height : ScreenHeight);
      int packedFields = 0;
      if (image.LocalColorTable != null) {
        packedFields |= 128 + (image.LocalColorTable.BitSize() - 1);
        if(image.LocalColorTable.Ordered) {
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
      PrefixTable prefixes = new PrefixTable();
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
          if(nextCode >= (1 << writer.CodeLength)) {
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
      WriteByte(0x3B);
    }

    #endregion

  }

}
