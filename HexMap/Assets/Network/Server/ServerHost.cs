using LiteNetLib;
using Network.Interfaces;
using Network.Shared;
using Network.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Network.Server
{
    public class ServerHost : MonoBehaviour, IOperationHandlerManager
    {
        public int Port;

        private EventBasedNetListener listener;
        private NetManager netManager;

        private IPeerGroup peerGroup;
        private ISerializer serializer;

        public GameObject[] CustomOperationHandlers;

        private void Start()
        {
            listener = new EventBasedNetListener();
            netManager = new NetManager(listener);

            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;

            serializer = DefaultSerializer.GetInstance();

            var handlers = GetComponentsInChildren<MonoBehaviour>().OfType<IOperationHandler>();
            foreach (var handler in handlers)
            {
                RegisterHandler(handler);
            }

            if (CustomOperationHandlers != null)
            {
                foreach (var go in CustomOperationHandlers)
                {
                    handlers = go.GetComponents<MonoBehaviour>().OfType<IOperationHandler>();
                    if (handlers.Count() > 0)
                    {
                        foreach (var handler in handlers)
                        {
                            RegisterHandler(handler);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"GameObject {go.name} don't have operation handler script.");
                    }
                }
            }

            netManager.Start(Port);
        }

        private void Update()
        {
            netManager.PollEvents();
        }

        private void OnDestroy()
        {
            netManager.DisconnectAll();
        }

        #region -- Listener events --
        private void Listener_PeerDisconnectedEvent(NetPeer netPeer, DisconnectInfo disconnectInfo)
        {
            IPeer peer = (IPeer)netPeer.Tag;
            peer.OnDisconnectEvent?.Invoke(peer);
            OnPeerDisconnected(peer);
        }

        private void Listener_PeerConnectedEvent(NetPeer netPeer)
        {
            Peer peer = new Peer(netPeer, DefaultSerializer.GetInstance());
            netPeer.Tag = peer;
            OnPeerConnected(peer);
        }

        private void Listener_NetworkReceiveEvent(NetPeer netPeer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            IPeer peer = (IPeer)netPeer.Tag;
            byte[] dgram = new byte[reader.AvailableBytes];
            reader.GetBytes(dgram, dgram.Length);
            reader.Recycle();
            Packet packet = (Packet)serializer.Deserialize(dgram);
            if(_handlers.TryGetValue(packet.OperationCode, out IOperationHandler handler))
            {
                handler.Handle(peer, packet.Payload);
            }
        }

        private void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            request.AcceptIfKey("coinmouse");
        }
        #endregion

        #region -- IOperationHandlerManager methods --
        Dictionary<byte, IOperationHandler> _handlers = new Dictionary<byte, IOperationHandler>();

        public void RegisterHandler(IOperationHandler handler)
        {
            if (!_handlers.ContainsKey(handler.OperationCode))
            {
                _handlers.Add(handler.OperationCode, handler);
            }
            else
            {
                Debug.LogWarning("Specific handler of operation code has been existed.");
            }
        }

        public void UnregisterHandler(IOperationHandler handler)
        {
            _handlers.Remove(handler.OperationCode);
        }

        public void UpdateHandlers(float deltaTime)
        {
            foreach (var handler in _handlers.Values)
            {
                try
                {
                    handler.UpdateHandler(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        public void ReceivePacket(IPeer peer, byte operationCode, IDictionary<byte, object> payload)
        {
            if (_handlers.TryGetValue(operationCode, out IOperationHandler handler))
            {
                try
                {
                    handler.Handle(peer, payload);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
            else
            {
                Debug.LogWarning("Specific handler of operation code not found.");
            }
        }

        public void OnPeerConnected(IPeer peer)
        {
            foreach(var handler in _handlers.Values)
            {
                handler.OnPeerConnected(peer);
            }
        }

        public void OnPeerDisconnected(IPeer peer)
        {
            foreach (var handler in _handlers.Values)
            {
                handler.OnPeerDisconnected(peer);
            }
        }
        #endregion
    }
}