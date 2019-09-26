using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chess : MonoBehaviour, IHexUnit
{
    public int Group { get; set; }
    public JobClass Job { get; set; }

    public int speed;
    public int Speed { get { return speed; } }
    public int visionRange;

    public HexGrid Grid { get; set; }
    private HexCell location;
    public HexCell Location
    {
        get { return location; }
        set
        {
            if (location)
            {
                Grid.DecreaseVisibility(location, visionRange);
                location.Unit = null;
                if(location.iUnit == this)
                    location.iUnit = null;
            }
            location = value;
            if(location.iUnit == null)
                location.iUnit = this;
            Grid.IncreaseVisibility(value, visionRange);
            Position = value.Position;
        }
    }

    private bool positionChanged;
    private Vector3 position;
    public Vector3 Position
    {
        get
        {   
            return position;
        }
        set
        {
            positionChanged = true;
            position = value;
        }
    }

    public float agility;
    public float Agility { get { return agility; } set { agility = value; } }
    public float actionProcess;
    public float ActionProcess { get { return actionProcess; } set { actionProcess = value; } }

    [SerializeField]
    private int actionPoint;
    public int ActionPoint { get { return actionPoint; } set { actionPoint = value; } }


    public bool IsAlive { get { return hitPoint > 0; } }
    [SerializeField]
    private int hitPoint;
    public int HitPoint
    {
        get { return hitPoint; }
        set
        {
            hitPoint = value;
            if (hitPoint < 0)
                hitPoint = 0;
        }
    }

    [SerializeField]
    private int attack;
    public int Attack { get { return attack; } set { attack = value; } }

    private void LateUpdate()
    {
        if (positionChanged)
            transform.localPosition = Position;
    }
}
