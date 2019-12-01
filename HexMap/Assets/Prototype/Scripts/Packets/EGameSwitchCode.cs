using System.Collections;
using System.Collections.Generic;

namespace HexGame.Packets
{
    public enum EGameSwitchCode : byte
    {
        MoveRequest,
        MoveCommand,
        AttackRequest,
        AttackCommand,
    }
}