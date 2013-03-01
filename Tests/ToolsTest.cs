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
  }
}
