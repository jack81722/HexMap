using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Chess Ability")]
public class ChessAbility : ScriptableObject
{
    public string Name;
    public JobClass Job;
    public int MaxHitPoint;
    public int Attack;
    public int Speed;
    public float Agility;
    public int VisionRange;

    public List<SkillData> skills;
}
