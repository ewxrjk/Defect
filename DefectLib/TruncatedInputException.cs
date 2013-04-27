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

namespace Defect
{
  /// <summary>
  /// Thrown when the input stream terminates before the GIF does
  /// </summary>
  [Serializable()]
  public class TruncatedInputException : Exception
  {
    public TruncatedInputException() : base() { }
    public TruncatedInputException(string message) : base(message) { }
    public TruncatedInputException(string message, Exception inner) : base(message, inner) { }
    protected TruncatedInputException(System.Runtime.Serialization.SerializationInfo info,
                                 System.Runtime.Serialization.StreamingContext context) { }
  }
}
