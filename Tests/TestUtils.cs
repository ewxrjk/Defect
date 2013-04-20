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

namespace Tests
{
  static class TestUtils
  {
    static public void AreEqual(byte[] expected, byte[] got, string description) {
      for (int i = 0; i < Math.Min(expected.Length, got.Length); ++i) {
        Assert.AreEqual(expected[i], got[i],
                        "{0}: index {1}: expected 0x{2:X2} got 0x{3:X2}",
                        description, i, expected[i], got[i]);
      }
      Assert.AreEqual(expected.Length, got.Length,
                      "{0}: expected length {1} got {2}",
                      description, expected.Length, got.Length);
    }

  }
}
