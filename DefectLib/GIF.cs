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
using System.IO;

namespace Defect
{
  public partial class GIF
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

    #region Constants

    static readonly string Version = "GIF89a";

    const byte ExtensionIntroducer = 0x21;

    const byte GraphicControlExtension = 0xF9;

    const byte GraphicControlExtensionSize = 0x04;

    const byte ImageDescriptor = 0x2C;

    const byte Trailer = 0x3B;

    #endregion
    
  }

}
