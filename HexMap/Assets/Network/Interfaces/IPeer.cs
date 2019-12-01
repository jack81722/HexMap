using Network.Shared;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Network.Interfaces
{
    public interface IPeer
    {
        int Id { get; }

        void Send(byte operationCode, IDictionary<byte, object> payload, EReliability reliability);

        void Receive(byte[] dgram);

        event Action<IPeer, byte, IDictionary<byte, object>> OnReceivePacket;

        Action<IPeer> OnDisconnectEvent { get; set; }
        
        void Disconnect();
    }

    public interface IPeerGroup
    {
        void AddPeer(IPeer peer);

        void RemovePeer(IPeer peer);

        void RemovePeer(int id);

        IPeer GetPeer(int id);

        bool TryGetPeer(int id, out IPeer peer);
    }
}