using System;
using System.Collections;
using System.Collections.Generic;

namespace HexGame.Packets
{
    [Serializable]
    public class AttackCommand
    {
        public int AttackerId;
        public int DefenderId;
        public SkillInfo SkillInfo;
    }
}