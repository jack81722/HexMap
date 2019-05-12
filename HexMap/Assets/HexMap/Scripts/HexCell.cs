using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinate coordinate;
    private int elevation;
    public int Elevation
    {
        get { return elevation; }
        set
        {
            elevation = value;
            // set position
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.ElevationStep;
            position.y +=
                (HexMetrics.SampleNoise(position).y * 2f - 1f) *
                HexMetrics.ElevationPerturbStrength;
            transform.localPosition = position;

            // set ui position
            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;
        }
    }
    
    public Color color;

    public RectTransform uiRect;

    public Vector3 Position
    {
        get { return transform.localPosition; }
    }

    [SerializeField]
    public HexCell[] neighbors;

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }
}
