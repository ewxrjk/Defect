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

namespace Defect
{
  public class ReversePrefixTable
  {
    /// <summary>
    /// Add a new prefix
    /// </summary>
    /// <param name="newCode">The new code prefix's code</param>
    /// <param name="extra">The byte value of the prefix</param>
    public void Add(int newCode, byte value)
    {
      Codes[newCode] = new byte[1] { value };
    }

    /// <summary>
    /// Add a new prefix
    /// </summary>
    /// <param name="newCode">The new code prefix's code</param>
    /// <param name="oldCode">The prefix to extend</param>
    /// <param name="extra">The additional byte</param>
    public void Add(int newCode, int oldCode, byte extra)
    {
      byte[] oldPrefix = Codes[oldCode];
      byte[] newPrefix = new byte[oldPrefix.Length + 1];
      Array.Copy(oldPrefix, newPrefix, oldPrefix.Length);
      newPrefix[newPrefix.Length - 1] = extra;
      Codes[newCode] = newPrefix;
    }

    /// <summary>
    /// Look up a prefix
    /// </summary>
    /// <param name="code">Code to look up</param>
    /// <returns>The byte sequence for <paramref name="code"/>.</returns>
    public byte[] Find(int code)
    {
      if (Codes.ContainsKey(code)) {
        return Codes[code];
      }
      else {
        return null;
      }
    }

    private Dictionary<int, byte[]> Codes = new Dictionary<int, byte[]>();
  }
}
