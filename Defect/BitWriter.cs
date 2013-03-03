using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
      //Console.WriteLine("WriteBits {0:X2} in {1}", n, CodeLength);
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
      //Console.WriteLine("FlushBits");
      if (bitsUsed > 0) {
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
      //Console.WriteLine("  WriteByte {0:X2}, b");
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
      //Console.WriteLine("  FlushBytes {0}", index);
      if (bufferPosition > 0) {
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
