using System;
using System.Collections;
using System.Collections.Generic;

namespace HexGame.Packets
{
    [Serializable]
    public class MoveCommand
    {
        public int UnitId;
        public int CellId;
    }
}