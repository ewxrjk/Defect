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
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Defect
{
  public static class Tools
  {
    /// <summary>
    /// Convert HSV to RGB
    /// </summary>
    /// <param name="h">Hue (0 to 360)</param>
    /// <param name="s">Saturation (0 to 1)</param>
    /// <param name="v">Value (0 to 1)</param>
    /// <param name="r">Red (0 to 1)</param>
    /// <param name="g">Green (0 to 1)</param>
    /// <param name="b">Blue (0 to 1)</param>
    static public void HsvToRgb(double h, double s, double v,
                                out double r, out double g, out double b)
    {
      double chroma = v * s;
      double hprime = h / 60.0; // 0-6
      double r1, g1, b1;
      if (hprime < 1) {
        r1 = chroma;
        g1 = chroma * hprime;
        b1 = 0;
      }
      else if (hprime < 2) {
        r1 = chroma * (2 - hprime);
        g1 = chroma;
        b1 = 0;
      }
      else if (hprime < 3) {
        r1 = 0;
        g1 = chroma;
        b1 = chroma * (hprime - 2);
      }
      else if (hprime < 4) {
        r1 = 0;
        g1 = chroma * (4 - hprime);
        b1 = chroma;
      }
      else if (hprime < 5) {
        r1 = chroma * (hprime - 4);
        g1 = 0;
        b1 = chroma;
      }
      else {
        r1 = chroma;
        g1 = 0;
        b1 = chroma * (6 - hprime);
      }
      double m = v - chroma;
      r = r1 + m;
      g = g1 + m;
      b = b1 + m;
    }
    
    /// <summary>
    /// All available image codecs
    /// </summary>
    public static IEnumerable<Type> ImageCodecs = from type in typeof(BitmapEncoder).Assembly.GetExportedTypes()
                                                  where type.IsSubclassOf(typeof(BitmapEncoder))
                                                  select type;

    /// <summary>
    /// All known image file extensions
    /// </summary>
    public static string BitmapExtensions = string.Join(";",
                                                        from type in ImageCodecs
                                                        from extension in ExtensionsFor(MakeEncoder(type))
                                                        select "*" + extension);

    /// <summary>
    /// Construct a bitmap encoder
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static BitmapEncoder MakeEncoder(Type type)
    {
      return (BitmapEncoder)Activator.CreateInstance(type);
    }

    /// <summary>
    /// Return the list of extensions for a bitmap encoder
    /// </summary>
    /// <param name="encoder"></param>
    /// <returns>List of extensions of the form .jpeg etc.</returns>
    public static IEnumerable<string> ExtensionsFor(BitmapEncoder encoder) {
      return encoder.CodecInfo.FileExtensions.Split(',');
    }

    /// <summary>
    /// Identify the appropriate bitmap encoder for a filename
    /// </summary>
    /// <param name="path">A filename</param>
    /// <returns>A <code>BitmapEncoder</code></returns>
    public static BitmapEncoder FindBitmapEncoder(string path)
    {
      string extension = Path.GetExtension(path).ToLowerInvariant();
      foreach (Type type in ImageCodecs) {
        BitmapEncoder encoder = MakeEncoder(type);
        if (ExtensionsFor(encoder).Contains(extension)) {
          return encoder;
        }
      }
      throw new ApplicationException(string.Format("Unknown file type *{0}", extension));
    }

    /// <summary>
    /// Exchange <paramref name="a"/> and <paramref name="b"/> in place
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static void Swap<T>(ref T a, ref T b) where T : struct
    {
      T tmp;
      tmp = a;
      a = b;
      b = tmp;
    }
  }
}
