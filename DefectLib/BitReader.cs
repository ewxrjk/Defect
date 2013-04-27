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
  public class BitReader
  {
    /// <summary>
    /// Input stream
    /// </summary>
    public Stream Input;

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
    /// Bits left in pendingByte
    /// </summary>
    private int bitsLeft = 0;

    /// <summary>
    /// Size of this block
    /// </summary>
    private int blockSize = 0;

    /// <summary>
    /// Position in block
    /// </summary>
    private int bufferPosition = 0;

    /// <summary>
    /// Current input block
    /// </summary>
    private byte[] block = new byte[255];

    /// <summary>
    /// Read a value using the current code length
    /// </summary>
    /// <returns>Next value or -1 if EOF happens first</returns>
    public int ReadCode()
    {
      int n = 0;
      int bitsTaken = 0;
      while (bitsTaken < CodeLength) {
        if (bitsLeft == 0) {
          if (Fill() < 0) {
            return -1;
          }
        }
        int bitsThisTime = Math.Min(bitsLeft, CodeLength - bitsTaken);
        int mask = (1 << bitsThisTime) - 1;
        n |= (pendingByte & mask) << bitsTaken;
        pendingByte >>= bitsThisTime;
        bitsTaken += bitsThisTime;
        bitsLeft -= bitsThisTime;
      }
      return n;
    }

    /// <summary>
    /// Read remaining blocks until no more input
    /// </summary>
    public void Clear()
    {
      for (; ; ) {
        blockSize = Input.ReadByte();
        if (blockSize < 0) {
          throw new TruncatedInputException();
        }
        if (blockSize == 0) {
          return;
        }
        int bytes = Input.Read(block, 0, blockSize);
        if (bytes < blockSize) {
          throw new TruncatedInputException();
        }
      }
    }

    /// <summary>
    /// Return true if there is at least one code readable without reading another byte
    /// </summary>
    /// <returns></returns>
    public bool WholeCodeLeft()
    {
      return bitsLeft >= CodeLength;
    }

    private int Fill()
    {
      if (bufferPosition >= blockSize) {
        blockSize = Input.ReadByte();
        if (blockSize <= 0) {
          throw new TruncatedInputException();
        }
        int bytes = Input.Read(block, 0, blockSize);
        if (bytes < blockSize) {
          throw new TruncatedInputException();
        }
      }
      pendingByte = block[bufferPosition++];
      bitsLeft = 8;
      return 0;
    }

  }
}
