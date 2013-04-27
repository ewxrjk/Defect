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

using Defect;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
  [TestClass]
  public class ReversePrefixTableTests
  {
    [TestMethod]
    public void EmptyReversePrefixTableTest()
    {
      ReversePrefixTable pt = new ReversePrefixTable();
      Assert.IsNull(pt.Find(0));
      Assert.IsNull(pt.Find(1));
    }

    [TestMethod]
    public void OneByteReversePrefixTableTest()
    {
      ReversePrefixTable pt = new ReversePrefixTable();
      for (int n = 0; n < 16; ++n) {
        pt.Add(n, (byte)n);
      }
      for (int n = 0; n < 16; ++n) {
        byte[] value = pt.Find(n);
        Assert.IsNotNull(value);
        Assert.AreEqual(1, value.Length);
        Assert.AreEqual(n, value[0]);
      }
      Assert.IsNull(pt.Find(16));
    }

    [TestMethod]
    public void ShortReversePrefixTableTest()
    {
      ReversePrefixTable pt = new ReversePrefixTable();
      pt.Add(0, 0);
      pt.Add(1, 0, 1);
      pt.Add(2, 1, 2);
      pt.Add(3, 2, 3);
      byte[] p0 = pt.Find(0);
      Assert.IsNotNull(p0);
      Assert.AreEqual(1, p0.Length);
      Assert.AreEqual(0, p0[0]);
      byte[] p1 = pt.Find(1);
      Assert.IsNotNull(p1);
      Assert.AreEqual(2, p1.Length);
      Assert.AreEqual(0, p1[0]);
      Assert.AreEqual(1, p1[1]);
      byte[] p2 = pt.Find(2);
      Assert.IsNotNull(p2);
      Assert.AreEqual(3, p2.Length);
      Assert.AreEqual(0, p2[0]);
      Assert.AreEqual(1, p2[1]);
      Assert.AreEqual(2, p2[2]);
      byte[] p3 = pt.Find(3);
      Assert.IsNotNull(p3);
      Assert.AreEqual(4, p3.Length);
      Assert.AreEqual(0, p3[0]);
      Assert.AreEqual(1, p3[1]);
      Assert.AreEqual(2, p3[2]);
      Assert.AreEqual(3, p3[3]);
    }

  }
}
