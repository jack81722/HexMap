using System;
using System.Collections.Generic;
using System.Text;

namespace HexMapServer
{
    public interface IPeerGroup
    {
        int Id { get; }

        void AddPeer(IPeer peer);

        void Process(TimeSpan deltaTime);

        void Broadcast(object obj, ReliabilityType reliability);

        IEnumerable<IPeer> GetPeers();
    }
}
