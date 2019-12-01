using LiteNetLib;
using Network.Interfaces;
using Network.Shared;
using Network.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

namespace Network.Client
{
    public class ClientLogin : MonoBehaviour, IOperationHandlerManager
    {
        public string ServerUrl = "http://127.0.0.1/";
        public int Port = 32001;

        public IPeer Peer { get; private set; }

        EventBasedNetListener listener;
        NetManager netManager;

        public GameObject[] CustomOperationHandlers;

        private void Awake()
        {
            _handlers = new Dictionary<byte, IOperationHandler>();
        }

        private void Start()
        {
            listener = new EventBasedNetListener();
            netManager = new NetManager(listener);

            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;

            var handlers = GetComponentsInChildren<MonoBehaviour>().OfType<IOperationHandler>();
            foreach(var handler in handlers)
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

            netManager.Start();

            Connect(ServerUrl, Port);
        }

        //private void OnGUI()
        //{
        //    if (GUI.Button(new Rect(10, 10, 50, 20), "Connect"))
        //    {
        //        Connect(ServerUrl, Port);
        //    }
        //}

        public void Connect(string url, int port)
        {
            var ip = Dns.GetHostAddresses(ServerUrl)[0];
            try
            {
                var netPeer = netManager.Connect(new IPEndPoint(ip, Port), "coinmouse");
                Peer = new Peer(netPeer, DefaultSerializer.GetInstance());
                netPeer.Tag = Peer;
                Peer.OnReceivePacket += ReceivePacket;
                OnPeerConnected(Peer);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void Update()
        {
            netManager.PollEvents();
            UpdateHandlers(Time.deltaTime);
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

        private void Listener_NetworkReceiveEvent(NetPeer netPeer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            IPeer peer = (IPeer)netPeer.Tag;
            byte[] dgram = new byte[reader.AvailableBytes];
            reader.GetBytes(dgram, dgram.Length);
            peer.Receive(dgram);
        }

        #endregion

        #region -- IOperationHandlerManager methods --
        Dictionary<byte, IOperationHandler> _handlers;

        public void RegisterHandler(IOperationHandler handler)
        {
            if (!_handlers.ContainsKey(handler.OperationCode))
            {
                _handlers.Add(handler.OperationCode, handler);
            }
            else
            {
                Debug.LogWarning($"Specific handler (Name : {handler.GetType().Name}) of operation code ({handler.OperationCode}) has been existed.");
            }
        }

        public void UnregisterHandler(IOperationHandler handler)
        {
            _handlers.Remove(handler.OperationCode);
        }

        public void UpdateHandlers(float deltaTime)
        {
            foreach(var handler in _handlers.Values)
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
            foreach (var handler in _handlers.Values)
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