using LiteNetLib;
using LiteNetLib.Utils;
using Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HexMapServer
{
    public class Server : PeerGroup
    {
        public int MinRecvPeriod = 10;
        private DateTime lastTime, currentTime;
        private ConcurrentDictionary<int, IPeer> peers;
        private ConcurrentDictionary<int, IPeerGroup> groups;

        EventBasedNetListener listener;
        NetManager netMgr;

        TaskCompletionSource<IServerResult> tcs;
        CancellationTokenSource cts;

        public Server()
        {
            tcs = new TaskCompletionSource<IServerResult>();
            cts = new CancellationTokenSource();
            listener = new EventBasedNetListener();
            netMgr = new NetManager(listener);
            peers = new ConcurrentDictionary<int, IPeer>();
            groups = new ConcurrentDictionary<int, IPeerGroup>();
            groups.TryAdd(Id, this);
        }

        public Task<IServerResult> Start()
        {
            main();
            return tcs.Task;
        }

        private void main()
        {
            try
            {
                listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
                listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
                netMgr.Start(8849);
                lastTime = currentTime = DateTime.Now;
                while (!cts.Token.IsCancellationRequested)
                {
                    TimeSpan span = currentTime - lastTime;
                    foreach (var peer in peers.Values)
                    {
                        peer.HandleReceiveEvent();
                    }
                    foreach(var group in groups.Values)
                    {
                        group.Process(span);
                    }
                    int deltaMs = (int)span.TotalMilliseconds;
                    if (deltaMs < MinRecvPeriod)
                    {
                        Thread.Sleep(MinRecvPeriod - deltaMs);
                    }
                }
                tcs.SetCanceled();
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            int length = reader.AvailableBytes;
            byte[] dgram = new byte[length];
            reader.GetBytes(dgram, length);
            // push packet
            IPeer p = peers[peer.Id];
            p.Push(p, Serialization.ToObject(dgram), (ReliabilityType)deliveryMethod);
        }

        private void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            var peer = request.AcceptIfKey("coinmouse");
            Peer newPeer = new Peer(peer);

        }

        public void Close()
        {
            cts.Cancel();
            netMgr.DisconnectAll();
            Console.WriteLine("Server closed.");
        }

        public override void AddPeer(IPeer peer)
        {
            peers.TryAdd(peer.Id, peer);
        }

        public override IEnumerable<IPeer> GetPeers()
        {
            return peers.Values;
        }

        public void RemoveRoom(int roomId)
        {
            if(!groups.TryRemove(roomId, out IPeerGroup group))
                throw new InvalidOperationException("Room was not register in collection.");
        }

        public void RemoveRoom(Room room)
        {
            if (!groups.TryGetValue(room.Id, out IPeerGroup r))
                throw new InvalidOperationException("Room was not register in collection.");
            if (r != room)
                throw new InvalidOperationException("Room was not registered in collection.");
            groups.TryRemove(room.Id, out IPeerGroup group);
        }

        public class Peer : IPeer
        {
            public NetPeer netPeer;
            public event Packet.PacketHandler handler;
            private ConcurrentQueue<Packet> packets;

            public int Id { get { return netPeer.Id; } }

            public Peer(NetPeer peerInstance)
            {
                this.netPeer = peerInstance;
                packets = new ConcurrentQueue<Packet>();
            }

            public void Send(byte[] dgram, ReliabilityType reliability)
            {
                netPeer.Send(dgram, (DeliveryMethod)reliability);
            }

            public void Push(IPeer sender, object obj, ReliabilityType reliability)
            {
                Packet packet = new Packet(sender, obj, reliability);
                packets.Enqueue(packet);
            }

            public void HandleReceiveEvent()
            {
                if (handler != null)
                {
                    foreach (var packet in packets)
                    {
                        handler.Invoke(packet);
                    }
                }
            }

            
        }

        public class Result : IServerResult
        {
            public string Msg { get; }

            public Result(object obj)
            {
                Msg = obj.ToString();
            }

            public override string ToString()
            {
                return Msg;
            }
        }

        public class Room : PeerGroup
        {
            ConcurrentBag<IPeer> peers;
            ConcurrentQueue<Packet> packets;

            public Room()
            {
                peers = new ConcurrentBag<IPeer>();
                packets = new ConcurrentQueue<Packet>();
            }

            public override void Process(TimeSpan deltaTime)
            {
                
            }

            public override void AddPeer(IPeer peer)
            {
                peers.Add(peer);
            }

            public override IEnumerable<IPeer> GetPeers()
            {
                return peers;
            }

            protected override void FreeManagedObjects()
            {
                peers.Clear();
            }
        }

    }

    public interface IServerResult
    {
        string Msg { get; }
    }

    
}
