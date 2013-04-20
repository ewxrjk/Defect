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
