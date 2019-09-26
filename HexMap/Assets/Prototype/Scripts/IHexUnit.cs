using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHexUnit
{
    int Group { get; set; }         // 陣營
    JobClass Job {get;set;}         // 職業

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
}

public enum JobClass
{
    SwordMan,
    Gunner
}