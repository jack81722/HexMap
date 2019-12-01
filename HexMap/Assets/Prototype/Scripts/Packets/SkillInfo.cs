using System;
using System.Collections;
using System.Collections.Generic;

namespace HexGame.Packets
{
    [Serializable]
    public class SkillInfo
    {
        public int AimDistance;

        public ESKillRangeType SkillRangeType;

        public int HitRange;
    }
}