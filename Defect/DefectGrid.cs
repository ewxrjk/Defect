using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Defect
{
  public class DefectGrid
  {
    /// <summary>
    /// Construct a new defect array of given dimensions
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="levels"></param>
    public DefectGrid(int width, int height, int levels)
    {
      this.Width = width;
      this.Height = height;
      this.Levels = levels;
      this.Data = new int[height, width];
      this.NewData = new int[height, width];
      for (int y = 0; y < Height; ++y) {
        for (int x = 0; x < Width; ++x) {
          this.Data[y, x] = rng.Next(this.Levels);
        }
      }
    }

    private Random rng = new Random();

    public int Width { get; private set; }

    public int Height { get; private set; }

    public int Levels { get; private set; }

    private int[,] Data;

    private int[,] NewData;

    private int ChunkSize = 8;

    /// <summary>
    /// Step the array
    /// </summary>
    /// <returns>The number of cells that changed state</returns>
    public int Step()
    {
      // The rule is that if any of a cell's four neighbours are
      // 1 greater (mod Levels) than they are in value, they
      // change to that value.
      int changed = 0;
      int left = Height / ChunkSize;
      for (int y0 = 0; y0 < Height; y0 += ChunkSize) {
        int y0_rebound = y0;
        ThreadPool.QueueUserWorkItem((object unused) =>
        {
          int limit = Math.Min(Height, y0_rebound + ChunkSize);
          for (int y = y0_rebound; y < limit; ++y) {
            for (int x = 0; x < Width; ++x) {
              int nextLevel = Up(Data[y, x], Levels);
              if (Data[Up(y, Height), x] == nextLevel
                 || Data[Down(y, Height), x] == nextLevel
                 || Data[y, Up(x, Width)] == nextLevel
                 || Data[y, Down(x, Width)] == nextLevel) {
                NewData[y, x] = nextLevel;
                ++changed;
              }
              else {
                NewData[y, x] = Data[y, x];
              }
            }
          }
          lock (this) {
            --left;
            Monitor.Pulse(this);
          }
        });
      }
      lock (this) {
        while (left > 0) {
          Monitor.Wait(this);
        }
      }
      int[,] tmp = Data;
      Data = NewData;
      NewData = tmp;
      return changed;
    }

    /// <summary>
    /// Write pixel data
    /// </summary>
    /// <param name="colorData">Destination for pixel data.  Format as PixelFormats.Bgr32.</param>
    /// <param name="palette">Palette mapping level data to RGB triples</param>
    public void Render(uint[] colorData, uint[] palette, int scale)
    {
      int n = 0;
      for (int y = 0; y < Height; ++y) {
        for (int ys = 0; ys < scale; ++ys) {
          for (int x = 0; x < Width; ++x) {
            for (int xs = 0; xs < scale; ++xs) {
              colorData[n++] = palette[Data[y, x]];
            }
          }
        }
      }
    }

    /// <summary>
    /// Return (n+1) mod m
    /// </summary>
    /// <param name="n"></param>
    /// <param name="m"></param>
    /// <returns>(n+1) mod m</returns>
    static public int Up(int n, int m)
    {
      ++n;
      return n < m ? n : 0;
    }

    /// <summary>
    /// Return (n-1) mod m
    /// </summary>
    /// <param name="n"></param>
    /// <param name="m"></param>
    /// <returns>(n-1) mod m</returns>
    static public int Down(int n, int m)
    {
      --n;
      return n >= 0 ? n : m - 1;
    }
  }
}
