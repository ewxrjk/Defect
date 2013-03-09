using System;
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

  public class DefectGrid
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
      this.Data = new byte[height, width];
      this.NewData = new byte[height, width];
      for (int y = 0; y < Height; ++y) {
        for (int x = 0; x < Width; ++x) {
          this.Data[y, x] = (byte)rng.Next(this.Levels);
        }
      }
    }

    private Random rng = new Random();

    public int Width { get; private set; }

    public int Height { get; private set; }

    public int Levels { get; private set; }

    public CellNeighbourhood Neighbourhood { get; private set; }

    private byte[,] Data;

    private byte[,] NewData;

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
      int step = Environment.ProcessorCount;
      int left = step;
      for (int n = 0; n < step; ++n) {
        int n_rebound = n;
        ThreadPool.QueueUserWorkItem((object unused) =>
        {
          int changed_here = 0;
          for (int y = n_rebound; y < Height; y += step) {
            for (int x = 0; x < Width; ++x) {
              byte nextLevel = (byte)Up(Data[y, x], Levels);
              if (Data[Up(y, Height), x] == nextLevel
                 || Data[Down(y, Height), x] == nextLevel
                 || Data[y, Up(x, Width)] == nextLevel
                 || Data[y, Down(x, Width)] == nextLevel
                 || (Neighbourhood == CellNeighbourhood.Moore
                     && (Data[Up(y, Height), Up(x, Width)] == nextLevel
                         || Data[Down(y, Height), Up(x, Width)] == nextLevel
                         || Data[Up(y, Height), Down(x, Width)] == nextLevel
                         || Data[Down(y, Height), Down(x, Width)] == nextLevel))) {
                NewData[y, x] = nextLevel;
                ++changed_here;
              }
              else {
                NewData[y, x] = Data[y, x];
              }
            }
          }
          lock (this) {
            --left;
            changed += changed_here;
            Monitor.Pulse(this);
          }
        });
      }
      lock (this) {
        while (left > 0) {
          Monitor.Wait(this);
        }
      }
      byte[,] tmp = Data;
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
    /// Write pixel data
    /// </summary>
    /// <param name="pixelData">Destination for pixel data</param>
    /// <param name="scale"></param>
    public void Render(byte[] pixelData, int scale)
    {
      int n = 0;
      for (int y = 0; y < Height; ++y) {
        for (int ys = 0; ys < scale; ++ys) {
          for (int x = 0; x < Width; ++x) {
            for (int xs = 0; xs < scale; ++xs) {
              pixelData[n++] = Data[y, x];
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
