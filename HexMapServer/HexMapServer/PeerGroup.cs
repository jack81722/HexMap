using Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace HexMapServer
{
    public abstract class PeerGroup : DisposableObject, IPeerGroup
    {
        protected static readonly IdGenerator idGenerator = new IdGenerator();
        private int _id = idGenerator.NewId;
        public int Id => _id;

        public abstract void AddPeer(IPeer peer);

        public virtual void Broadcast(object obj, ReliabilityType reliability)
        {
            byte[] dgram = Serialization.ToByteArray(obj);
            foreach(var peer in GetPeers())
            {
                peer.Send(dgram, reliability);
            }
        }

        public virtual void Process(TimeSpan deltaTime) { }

        public abstract IEnumerable<IPeer> GetPeers();
    }
}
