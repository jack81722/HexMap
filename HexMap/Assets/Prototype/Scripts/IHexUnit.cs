using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHexUnit
{
    int Id { get; }
    int Group { get; set; }         // 陣營
    JobClass Job { get; }         // 職業

    string name { get; }
    Transform transform { get; }

    bool IsAlive { get; }           // 是否活著
    int HitPoint { get; set; }
    int Attack { get; set; }
    int ActionPoint { get; set; }

    int Speed { get; }
    HexCell Location { get; set; }
    Vector3 Position { get; set; }

    float Agility { get; }
    float ActionProcess { get; set; }

    IList<ISkill> GetSkills();
}

public enum JobClass
{
    SwordMan,
    Gunner
}