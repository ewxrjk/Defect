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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows.Media.Imaging;

namespace Tests
{
  [TestClass]
  public class ToolsTest
  {
    [TestMethod]
    public void HsvTest()
    {
      double r, g, b;

      Defect.Tools.HsvToRgb(0, 1, 1, out r, out g, out b);
      Assert.AreEqual(1.0, r, "h=0 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(0.0, g, "h=0 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(0.0, b, "h=0 rgb={0} {1} {2}", r, g, b);

      Defect.Tools.HsvToRgb(60, 1, 1, out r, out g, out b);
      Assert.AreEqual(1.0, r, "h=60 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(1.0, g, "h=60 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(0.0, b, "h=60 rgb={0} {1} {2}", r, g, b);

      Defect.Tools.HsvToRgb(120, 1, 1, out r, out g, out b);
      Assert.AreEqual(0.0, r, "h=120 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(1.0, g, "h=120 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(0.0, b, "h=120 rgb={0} {1} {2}", r, g, b);

      Defect.Tools.HsvToRgb(180, 1, 1, out r, out g, out b);
      Assert.AreEqual(0.0, r, "h=180 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(1.0, g, "h=180 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(1.0, b, "h=180 rgb={0} {1} {2}", r, g, b);

      Defect.Tools.HsvToRgb(240, 1, 1, out r, out g, out b);
      Assert.AreEqual(0.0, r, "h=240 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(0.0, g, "h=240 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(1.0, b, "h=240 rgb={0} {1} {2}", r, g, b);

      Defect.Tools.HsvToRgb(300, 1, 1, out r, out g, out b);
      Assert.AreEqual(1.0, r, "h=300 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(0.0, g, "h=300 rgb={0} {1} {2}", r, g, b);
      Assert.AreEqual(1.0, b, "h=300 rgb={0} {1} {2}", r, g, b);
    }

    [TestMethod]
    public void BitmapExtensionsTest()
    {
      foreach(Type type in Defect.Tools.ImageCodecs) {
        BitmapEncoder encoder = Defect.Tools.MakeEncoder(type);
        foreach (string extension in Defect.Tools.ExtensionsFor(encoder)) {
          Assert.AreEqual('.', extension[0]);
        }
      }
    }
  }
}
