using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHexUnit
{
    int Group { get; set; }         // 陣營
    Transform transform { get; }

    int HitPoint { get; set; }
    int Attack { get; set; }

    int Speed { get; }
    HexCell Location { get; set; }
    Vector3 Position { get; set; }

    float Agility { get; }
    float ActionProcess { get; set; }

    int ActionPoint { get; set; }
}
