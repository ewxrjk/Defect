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
  public class ForwardPrefixTableTests
  {
    [TestMethod]
    public void EmptyForwardPrefixTableTest()
    {
      ForwardPrefixTable pt = new ForwardPrefixTable();
      int length;
      Assert.AreEqual(-1, pt.Find(new byte[0], 0, out length));
      Assert.AreEqual(0, length);
      Assert.AreEqual(-1, pt.Find(new byte[1] { 0 }, 0, out length));
      Assert.AreEqual(0, length);
      Assert.AreEqual(-1, pt.Find(new byte[1] { 1 }, 0, out length));
      Assert.AreEqual(0, length);
    }

    [TestMethod]
    public void OneByteForwardPrefixTableTest()
    {
      ForwardPrefixTable pt = new ForwardPrefixTable();
      int length;
      for (int n = 0; n < 16; ++n) {
        pt.Add(n, -1, (byte)n);
      }
      for (int n = 0; n < 16; ++n) {
        Assert.AreEqual(n, pt.Find(new byte[1] { (byte)n }, 0, out length), "checking code");
        Assert.AreEqual(1, length, "checking length");
      }
      Assert.AreEqual(-1, pt.Find(new byte[1] { 17 }, 0, out length), "checking code not found");
      Assert.AreEqual(0, length, "checking length not found");
    }

    [TestMethod]
    public void ShortForwardrefixTableTest()
    {
      ForwardPrefixTable pt = new ForwardPrefixTable();
      int length;
      pt.Add(0, -1, 0);
      pt.Add(1, 0, 1);
      pt.Add(2, 1, 2);
      pt.Add(3, 2, 3);
      Assert.AreEqual(0, pt.Find(new byte[2] { 0, 99 }, 0, out length));
      Assert.AreEqual(1, length);
      Assert.AreEqual(1, pt.Find(new byte[3] { 0, 1, 99 }, 0, out length));
      Assert.AreEqual(2, length);
      Assert.AreEqual(2, pt.Find(new byte[4] { 0, 1, 2, 99 }, 0, out length));
      Assert.AreEqual(3, length);
    }

    [TestMethod]
    public void DivergentForwardPrefixTableTest()
    {
      ForwardPrefixTable pt = new ForwardPrefixTable();
      int length;
      pt.Add(0, -1, 0);
      pt.Add(1, 0, 1);
      pt.Add(2, 0, 2);
      pt.Add(3, 0, 3);
      Assert.AreEqual(0, pt.Find(new byte[2] { 0, 99 }, 0, out length));
      Assert.AreEqual(1, length);
      Assert.AreEqual(1, pt.Find(new byte[3] { 0, 1, 99 }, 0, out length));
      Assert.AreEqual(2, length);
      Assert.AreEqual(2, pt.Find(new byte[3] { 0, 2, 99 }, 0, out length));
      Assert.AreEqual(2, length);
      Assert.AreEqual(3, pt.Find(new byte[3] { 0, 3, 99 }, 0, out length));
      Assert.AreEqual(2, length);
    }

    public void OffsetForwardPrefixTableTest()
    {
      ForwardPrefixTable pt = new ForwardPrefixTable();
      int length;
      pt.Add(0, -1, 0);
      pt.Add(1, 0, 1);
      pt.Add(2, 0, 2);
      pt.Add(3, 0, 3);
      Assert.AreEqual(1, pt.Find(new byte[7] { 0, 1, 0, 2, 0, 3, 99 }, 0, out length));
      Assert.AreEqual(2, length);
      Assert.AreEqual(2, pt.Find(new byte[7] { 0, 1, 0, 2, 0, 3, 99 }, 2, out length));
      Assert.AreEqual(2, length);
      Assert.AreEqual(3, pt.Find(new byte[7] { 0, 1, 0, 2, 0, 3, 99 }, 4, out length));
      Assert.AreEqual(2, length);
    }
  }
}
