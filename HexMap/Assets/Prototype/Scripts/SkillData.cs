using HexGame.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skill Data")]
public class SkillData : ScriptableObject, ISkill
{
    [SerializeField]
    private Sprite _SkillIcon;
    public Sprite SkillIcon { get => _SkillIcon; set => _SkillIcon = value; }
    [SerializeField]
    private int _AimDistance;
    public int AimDistance { get => _AimDistance; set => _AimDistance = value; }

    [SerializeField]
    private ESKillRangeType skillRangeType = ESKillRangeType.Single;
    public ESKillRangeType SkillRangeType { get => skillRangeType; set => skillRangeType = value; }
    [SerializeField]
    private int hitRange;
    public int HitRange { get => hitRange; set => hitRange = value; }

    public static explicit operator SkillInfo(SkillData data)
    {
        return new SkillInfo
        {
            AimDistance = data.AimDistance,
            SkillRangeType = data.SkillRangeType,
            HitRange = data.HitRange
        };
    }
}

public interface ISkill
{
    Sprite SkillIcon { get; set; }

    int AimDistance { get; set; }
    
    ESKillRangeType SkillRangeType { get; set; }
    int HitRange { get; set; }
}

[Serializable]
public enum ESKillRangeType
{
    Single,
    Circle,
    Line
}
