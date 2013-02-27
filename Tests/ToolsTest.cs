using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
      Assert.AreEqual(1.0, r);
      Assert.AreEqual(0.0, g);
      Assert.AreEqual(0.0, b);

      Defect.Tools.HsvToRgb(60, 1, 1, out r, out g, out b);
      Assert.AreEqual(1.0, r);
      Assert.AreEqual(1.0, g);
      Assert.AreEqual(0.0, b);

      Defect.Tools.HsvToRgb(120, 1, 1, out r, out g, out b);
      Assert.AreEqual(0.0, r);
      Assert.AreEqual(1.0, g);
      Assert.AreEqual(0.0, b);

      Defect.Tools.HsvToRgb(180, 1, 1, out r, out g, out b);
      Assert.AreEqual(0.0, r);
      Assert.AreEqual(1.0, g);
      Assert.AreEqual(1.0, b);

      Defect.Tools.HsvToRgb(240, 1, 1, out r, out g, out b);
      Assert.AreEqual(0.0, r);
      Assert.AreEqual(0.0, g);
      Assert.AreEqual(1.0, b);

      Defect.Tools.HsvToRgb(300, 1, 1, out r, out g, out b);
      Assert.AreEqual(0.0, r);
      Assert.AreEqual(1.0, g);
      Assert.AreEqual(1.0, b);
    }
  }
}
