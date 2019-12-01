using System;
using System.Collections.Generic;
using System.Text;

namespace HexMapServer
{
    public interface IPeer
    {
        int Id { get; }
        void Send(byte[] dgram, ReliabilityType reliability);
        void Push(IPeer sender, object data, ReliabilityType reliability);
        void HandleReceiveEvent();
        event Packet.PacketHandler handler;
    }
}
