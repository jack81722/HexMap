using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chess : MonoBehaviour
{
    public int speed;
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
            }
            location = value;
            Grid.IncreaseVisibility(value, visionRange);
            transform.localPosition = value.Position;
        }
    }
}
