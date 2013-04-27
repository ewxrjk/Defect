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

namespace Defect
{
  /// <summary>
  /// An efficiently searchable table of up to 4096 prefixes
  /// </summary>
  public class ForwardPrefixTable
  {
    // TODO this implementation is quite allocation-heavy; we should be able to do with a single large array.

    /// <summary>
    /// A node represents a single prefix
    /// </summary>
    private class Node
    {
      /// <summary>
      /// Pointers to all prefixes that extend this node
      /// </summary>
      public Node[] Next = new Node[256];

      /// <summary>
      /// The code for this node
      /// </summary>
      public int Code;
    };

    /// <summary>
    /// All known nodes except <code>root</code>
    /// </summary>
    private Node[] allNodes = new Node[4096];

    /// <summary>
    /// The node that corresponds to an empty byte sequence
    /// </summary>
    private Node root = new Node()
    {
      Code = -1
    };

    /// <summary>
    /// Find the longest prefix that matches part of a byte array
    /// </summary>
    /// <param name="bytes">Byte array to match against</param>
    /// <param name="pos">Position in byte array to match against</param>
    /// <param name="length">The length of the matched prefix</param>
    /// <returns>The code for the longest matching prefix, or -1 if there isn't one</returns>
    public int Find(byte[] bytes, int pos, out int length)
    {
      Node node = root;
      length = 0;
      while (pos < bytes.Length && node.Next[bytes[pos]] != null) {
        node = node.Next[bytes[pos]];
        ++pos;
        ++length;
      }
      return node.Code;
    }

    /// <summary>
    /// Add a new prefix
    /// </summary>
    /// <param name="newCode">The new code prefix's code</param>
    /// <param name="oldCode">The prefix to extend, or -1 to add a single-byte prefix</param>
    /// <param name="extra">The additional byte</param>
    public void Add(int newCode, int oldCode, byte extra)
    {
      Node oldNode = (oldCode >= 0 ? allNodes[oldCode] : root);
      Node newNode = new Node() { Code = newCode };
      allNodes[newCode] = newNode;
      oldNode.Next[extra] = newNode;
    }

  }
}
