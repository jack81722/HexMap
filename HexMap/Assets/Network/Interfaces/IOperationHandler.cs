using System.Collections;
using System.Collections.Generic;

namespace Network.Interfaces
{
    public interface IOperationHandler
    {
        byte OperationCode { get; }

        void Handle(IPeer peer, IDictionary<byte, object> payload);

        void UpdateHandler(float deltaTime);

        void OnPeerConnected(IPeer peer);

        void OnPeerDisconnected(IPeer peer);
    }

    public interface IOperationHandlerManager
    {
        void RegisterHandler(IOperationHandler handler);

        void UnregisterHandler(IOperationHandler handler);

        void UpdateHandlers(float deltaTime);

        void ReceivePacket(IPeer peer, byte operationCode, IDictionary<byte, object> payload);

        void OnPeerConnected(IPeer peer);

        void OnPeerDisconnected(IPeer peer);
    }

}