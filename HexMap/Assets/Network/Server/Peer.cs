using LiteNetLib;
using Network.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Network.Shared
{
    public class Peer : IPeer
    {
        public int Id => _netPeer.Id;

        public Action<IPeer> OnDisconnectEvent { get; set; }
        public event Action<IPeer, byte, IDictionary<byte, object>> OnReceivePacket;

        private NetPeer _netPeer;
        private ISerializer _serializer;

        public Peer(NetPeer peer, ISerializer serializer)
        {
            _netPeer = peer;
            _serializer = serializer;
        }

        public void Disconnect()
        {
            _netPeer.Disconnect();
        }

        public void Send(byte operationCode, IDictionary<byte, object> payload, EReliability reliability)
        {
            _netPeer.Send(
                _serializer.Serialize(new Packet{ OperationCode = operationCode, Payload = payload }), 
                (DeliveryMethod)reliability);
        }

        public void Receive(byte[] dgram)
        {
            var packet = (Packet)_serializer.Deserialize(dgram);
            OnReceivePacket?.Invoke(this, packet.OperationCode, packet.Payload);
        }
    }
}