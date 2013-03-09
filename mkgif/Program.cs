using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Defect;
using System.Drawing;

namespace mkgif
{
  class Program
  {
    static void Main(string[] args)
    {
      int width = int.Parse(args[0]);
      int height = int.Parse(args[1]);
      int colors = int.Parse(args[2]);
      string path = args[3];
      using (FileStream fs = new FileStream(path, FileMode.Create)) {
        GIF gif = new GIF()
        {
          Output = fs,
          ScreenWidth = width,
          ScreenHeight = height,
          GlobalColorTable = new GIF.ColorTable()
          {
            Table = new Color[colors],
          },
          Debug = true,
        };
        for(int n = 0; n < colors; ++n) {
          gif.GlobalColorTable.Table[n] = Color.FromArgb(n, n, n);
        }
        GIF.Image image = new GIF.Image()
        {
          ImageData = new byte[width * height],
        };
        for (int n = 0; n < image.ImageData.Length; ++n) {
          image.ImageData[n] = (byte)(n % colors);
        }
        gif.Begin();
        gif.WriteImage(image);
        gif.End();
        fs.Flush();
      }
    }
  }
}
