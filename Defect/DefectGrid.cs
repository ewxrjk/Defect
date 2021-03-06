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
using System.Runtime.InteropServices;
using System.Threading;

namespace Defect
{
  /// <summary>
  /// Possible cell neighbourhoods
  /// </summary>
  public enum CellNeighbourhood
  {
    /// <summary>
    /// Von Neumann neighbourhood
    /// </summary>
    /// <remarks>The four cells to the north, east, south and west</remarks>
    VonNeumann,

    /// <summary>
    /// Moore neighbourhood
    /// </summary>
    /// <remarks>The eight cells north, northeast, east, etc.</remarks>
    Moore,
  }

  public class DefectGrid: IDisposable
  {
    /// <summary>
    /// Construct a new defect array of given dimensions
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="levels"></param>
    /// <param name="neighbourhood"></param>
    public DefectGrid(int width, int height, int levels, CellNeighbourhood neighbourhood)
    {
      if (levels < 2) {
        throw new ArgumentException("minimum 2 states");
      }
      if (levels > 256) {
        throw new ArgumentException("maximum 256 states");
      }
      this.Width = width;
      this.Height = height;
      this.Levels = levels;
      this.Neighbourhood = neighbourhood;
      this.Cells = width * height;
      this.Offset = width * (height + 2);
      this.Flip = false;
      InitializeBuffer();
    }

    private Random rng = new Random();

    public int Width { get; private set; }

    public int Height { get; private set; }

    public int Levels { get; private set; }

    public CellNeighbourhood Neighbourhood { get; private set; }

    unsafe private byte* Buffer = null;

    private bool Flip;

    private int Cells;

    private int Offset;

    private unsafe void InitializeBuffer()
    {
      this.Buffer = (byte*)Marshal.AllocHGlobal(Width * (Height + 2) * 2);
      for (int y = 0; y < Height; ++y) {
        for (int x = 0; x < Width; ++x) {
          *Location(x, y) = (byte)rng.Next(this.Levels);
        }
      }
    }

    private unsafe byte* Location(int x, int y)
    {
      return (byte*)this.Buffer + (Flip ? Offset : 0) + (y + 1) * Width + x;
    }

    private unsafe void DuplicateRow(byte *data, int yfrom, int yto) {
      byte* from = data + Width * yfrom;
      byte* to = data + Width * yto;
      for (int x = 0; x < Width; ++x) {
        *to++ = *from++;
      }
    }

    /// <summary>
    /// Step the array
    /// </summary>
    /// <returns>The number of cells that changed state</returns>
    public unsafe int Step()
    {
      // Figure out where we are
      byte* data = (byte*)this.Buffer + (Flip ? Offset : 0);
      Flip = !Flip;
      byte* newdata = (byte*)this.Buffer + (Flip ? Offset : 0);
      // The rule is that if any of a cell's four neighbours are
      // 1 greater (mod Levels) than they are in value, they
      // change to that value.
      int changed = 0;
      switch (Neighbourhood) {
        case CellNeighbourhood.Moore:
          if (IntPtr.Size == 4) {
            changed = cyclic_moore_32_all(data, newdata, Width, Levels, Height);
          }
          else {
            changed = cyclic_moore_64_all(data, newdata, Width, Levels, Height);
          }
          break;
        case CellNeighbourhood.VonNeumann:
          if (IntPtr.Size == 4) {
            changed = cyclic_vn_32_all(data, newdata, Width, Levels, Height);
          }
          else {
            changed = cyclic_vn_64_all(data, newdata, Width, Levels, Height);
          }
          break;
      }
      return changed;
    }

    /// <summary>
    /// Write pixel data
    /// </summary>
    /// <param name="colorData">Destination for pixel data.  Format as PixelFormats.Bgr32.</param>
    /// <param name="palette">Palette mapping level data to RGB triples</param>
    public unsafe void Render(uint[] colorData, uint[] palette, int scale)
    {
      int n = 0;
      for (int y = 0; y < Height; ++y) {
        for (int ys = 0; ys < scale; ++ys) {
          byte* data = Location(0, y);
          for (int x = 0; x < Width; ++x) {
            for (int xs = 0; xs < scale; ++xs) {
              colorData[n++] = palette[*data];
            }
            ++data;
          }
        }
      }
    }

    /// <summary>
    /// Write pixel data
    /// </summary>
    /// <param name="pixelData">Destination for pixel data</param>
    /// <param name="scale"></param>
    public unsafe void Render(byte[] pixelData, int scale)
    {
      int n = 0;
      for (int y = 0; y < Height; ++y) {
        byte* data = Location(0, y);
        for (int ys = 0; ys < scale; ++ys) {
          for (int x = 0; x < Width; ++x) {
            for (int xs = 0; xs < scale; ++xs) {
              pixelData[n++] = *data;
            }
            ++data;
          }
        }
      }
    }

    #region IDisposable

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected unsafe virtual void Dispose(bool disposing)
    {
      if (Buffer != null) {
        Marshal.FreeHGlobal((IntPtr)Buffer);
        Buffer = null;
      }
    }

    ~DefectGrid() {
      Dispose(false);
    }

    #endregion

    #region Native code

    [DllImport("native32.dll", CallingConvention = CallingConvention.Cdecl)]
    private unsafe static extern int cyclic_vn_32_all(byte* from, byte* to, int width, int states, int height);

    [DllImport("native32.dll", CallingConvention = CallingConvention.Cdecl)]
    private unsafe static extern int cyclic_moore_32_all(byte* from, byte* to, int width, int states, int height);

    [DllImport("native64.dll", CallingConvention = CallingConvention.Cdecl)]
    private unsafe static extern int cyclic_vn_64_all(byte* from, byte* to, int width, int states, int height);

    [DllImport("native64.dll", CallingConvention = CallingConvention.Cdecl)]
    private unsafe static extern int cyclic_moore_64_all(byte* from, byte* to, int width, int states, int height);

    #endregion

  }
}
