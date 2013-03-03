using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    #endregion

    #region Writing Images

    /// <summary>
    /// Write the header and logical screen descriptor for a GIF.
    /// </summary>
    /// <remarks><para>Before calling this, you must have set the GIF parameters.
    /// Do not change them after calling it.</para></remarks>
    public void Begin()
    {
      WriteVersion();
      WriteLogicalScreenDescriptor();
      if (GlobalColorTable != null) {
        WriteColorTable(GlobalColorTable);
      }
    }

    /// <summary>
    /// Write a single image
    /// </summary>
    /// <param name="image">Image to encode</param>
    /// <remarks><code>Begin()</code> must be called first.  This may be called any number of times.</remarks>
    public void WriteImage(Image image)
    {
      WriteGraphicControlExtension(image);
      WriteImageDescriptor(image);
      if (image.LocalColorTable != null) {
        WriteColorTable(image.LocalColorTable);
      }
      WriteImageData(image);
    }

    /// <summary>
    /// Complete the GIF
    /// </summary>
    /// <remarks><code>Begin()</code> must be called first.
    /// This must be called once at the end of the GIF.</remarks>
    public void End()
    {
      WriteTrailer();
    }

    #endregion

    #region Low Level Utilities

    private void WriteString(string s)
    {
      foreach (char c in s) {
        Output.WriteByte((byte)c);
      }
    }

    private void WriteByte(byte b)
    {
      Output.WriteByte(b);
    }

    private void WriteByte(int b)
    {
      Output.WriteByte((byte)b);
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
      WriteString("GIF89a");
    }

    private void WriteLogicalScreenDescriptor()
    {
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
      WriteByte(codeSize);
      // Initial code length is one more than code size, to accomodate the
      // Clear and End codes.
      BitWriter writer = new BitWriter()
      {
        Output = Output,
        CodeLength = 1 + codeSize,
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
      writer.WriteBits(1 << codeSize);
      int pos = 0;
      while (pos < image.ImageData.Length) {
        // Find the longest known prefix that matches this point in the image data
        int bestLength;
        int bestCode = prefixes.Find(image.ImageData, pos, out bestLength);
        // Output the best code we found
        writer.WriteBits(bestCode);
        // Add a new dictionary entry
        if (nextCode < 4096 && pos + bestLength < image.ImageData.Length) {
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
      writer.WriteBits((1 << codeSize) + 1);
      writer.FlushBits();
      writer.FlushBytes();
      // A zero-sized sub-block terminates.
      WriteByte(0);
    }

    void WriteTrailer()
    {
      WriteByte(0x3B);
    }

    #endregion

  }

}
