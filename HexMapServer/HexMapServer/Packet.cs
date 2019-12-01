using System;
using System.Collections.Generic;
using System.Text;

namespace HexMapServer
{
    public class Packet
    {
        public IPeer sender;
        public object data;
        public ReliabilityType reliability;

        public delegate void PacketHandler(Packet packet);

        public Packet(IPeer sender, object data, ReliabilityType reliability)
        {
            this.data = data;
            this.reliability = reliability;
        }
    }
}
