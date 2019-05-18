using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexGridChunk chunk;
    public HexCoordinate coordinate;
    private int elevation = int.MinValue;
    public int Elevation
    {
        get { return elevation; }
        set
        {
            if (elevation == value)
                return;

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

            refresh();
        }
    }
    
    public Color Color
    {
        get { return color; }
        set
        {
            if (color == value)
                return;
            color = value;
            refresh();
        }
    }
    private Color color;

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

    void refresh()
    {
        if (chunk != null)
        {  
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }
}
