using Defect;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
  [TestClass]
  public class PrefixTableTests
  {
    [TestMethod]
    public void EmptyPrefixTableTest()
    {
      PrefixTable pt = new PrefixTable();
      int length;
      Assert.AreEqual(-1, pt.Find(new byte[0], 0, out length));
      Assert.AreEqual(0, length);
      Assert.AreEqual(-1, pt.Find(new byte[1] { 0 }, 0, out length));
      Assert.AreEqual(0, length);
      Assert.AreEqual(-1, pt.Find(new byte[1] { 1 }, 0, out length));
      Assert.AreEqual(0, length);
    }

    [TestMethod]
    public void OneBytePrefixTableTest()
    {
      PrefixTable pt = new PrefixTable();
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
    public void ShortPrefixTableTest()
    {
      PrefixTable pt = new PrefixTable();
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
    public void DivergentPrefixTableTest()
    {
      PrefixTable pt = new PrefixTable();
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

    public void OffsetPrefixTableTest()
    {
      PrefixTable pt = new PrefixTable();
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
