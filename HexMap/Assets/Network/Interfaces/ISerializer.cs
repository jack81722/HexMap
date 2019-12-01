using System.Collections;
using System.Collections.Generic;

namespace Network.Interfaces
{
    public interface ISerializer
    {
        byte[] Serialize(object data);
        object Deserialize(byte[] dgram);
    }
}