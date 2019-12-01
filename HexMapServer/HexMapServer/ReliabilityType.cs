using System;
using System.Collections.Generic;
using System.Text;

namespace HexMapServer
{
    public enum ReliabilityType : byte
    {
        Unreliable = 0,
        ReliableUnordered = 1,
        Sequenced = 2,
        ReliableOrdered = 3,
        ReliableSequenced = 4
    }
}
