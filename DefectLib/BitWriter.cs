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
using System.IO;

namespace Defect
{
  public class BitWriter
  {
    /// <summary>
    /// Output stream
    /// </summary>
    public Stream Output;

    /// <summary>
    /// Current code length
    /// </summary>
    public int CodeLength;

    /// <summary>
    /// Whether to write debug messages
    /// </summary>
    public bool Debug = false;

    /// <summary>
    /// Pending byte
    /// </summary>
    private byte pendingByte = 0;

    /// <summary>
    /// Bits used so far in pendingByte
    /// </summary>
    private int bitsUsed = 0;

    /// <summary>
    /// Write a value using the current code length
    /// </summary>
    /// <param name="n">Value to write</param>
    public void WriteBits(int n)
    {
      if (Debug) {
        Console.Error.WriteLine(" writing {0:X2} in {1} bits", n, CodeLength);
      }
      int bitsRequired = CodeLength;
      while (bitsRequired > 0) {
        int bitsAvailable = 8 - bitsUsed;
        pendingByte |= (byte)(n << bitsUsed);
        bitsUsed += Math.Min(bitsRequired, bitsAvailable);
        n >>= bitsAvailable;
        bitsRequired -= bitsAvailable;
        if (bitsUsed == 8) {
          FlushBits();
        }
      }
    }

    /// <summary>
    /// Flush remaining bits
    /// </summary>
    public void FlushBits()
    {
      if (bitsUsed > 0) {
        if (Debug && bitsUsed < 8) {
          Console.Error.WriteLine(" flushing with only {0} bits used (could fit {1} more codes)",
                                  bitsUsed, (8 - bitsUsed) / CodeLength);
        }
        WriteByte(pendingByte);
        pendingByte = 0;
        bitsUsed = 0;
      }
    }

    /// <summary>
    /// Write a single byte
    /// </summary>
    /// <param name="b"></param>
    private void WriteByte(byte b)
    {
      if (Debug) {
        Console.Error.WriteLine("  storing byte {0:X2}", b);
      }
      buffer[bufferPosition++] = b;
      if (bufferPosition == 255) {
        FlushBytes();
      }
    }

    /// <summary>
    /// Flush remaining bytes 
    /// </summary>
    public void FlushBytes()
    {
      if (bufferPosition > 0) {
        if (Debug) {
          Console.Error.WriteLine("   writing length byte {0} plus data bytes", bufferPosition);
        }
        Output.WriteByte((byte)bufferPosition);
        Output.Write(buffer, 0, bufferPosition);
        bufferPosition = 0;
      }
    }

    /// <summary>
    /// Output buffer
    /// </summary>
    private byte[] buffer = new byte[255];

    /// <summary>
    /// Position used within output buffer
    /// </summary>
    private int bufferPosition = 0;

  }
}
