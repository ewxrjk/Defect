using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
  [TestClass]
  public class DefectTests
  {
    [TestMethod]
    public void TestUp()
    {
      Assert.AreEqual(1, Defect.DefectGrid.Up(0, 5));
      Assert.AreEqual(2, Defect.DefectGrid.Up(1, 5));
      Assert.AreEqual(3, Defect.DefectGrid.Up(2, 5));
      Assert.AreEqual(4, Defect.DefectGrid.Up(3, 5));
      Assert.AreEqual(0, Defect.DefectGrid.Up(4, 5));
    }

    [TestMethod]
    public void TestDown()
    {
      Assert.AreEqual(4, Defect.DefectGrid.Down(0, 5));
      Assert.AreEqual(0, Defect.DefectGrid.Down(1, 5));
      Assert.AreEqual(1, Defect.DefectGrid.Down(2, 5));
      Assert.AreEqual(2, Defect.DefectGrid.Down(3, 5));
      Assert.AreEqual(3, Defect.DefectGrid.Down(4, 5));
    }
  }
}
