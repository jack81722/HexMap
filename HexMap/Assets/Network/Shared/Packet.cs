using System;
using System.Collections;
using System.Collections.Generic;

namespace Network.Shared
{
    [Serializable]
    public class Packet
    {
        public byte OperationCode;
        public IDictionary<byte, object> Payload;
    }
}