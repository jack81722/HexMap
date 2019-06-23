using System;
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

            validateRivers();

            for(int i = 0; i < roads.Length; i++)
            {
                if(roads[i] && GetElevationDifference((HexDirection)i) > 1)
                {
                    SetRoad(i, false);
                }
            }

            refresh();
        }
    }

    #region River properties
    public float RiverSurfaceY
    {
        get
        {
            return
                (elevation + HexMetrics.WaterElevationOffset) *
                HexMetrics.ElevationStep;
        }
    }

    public float StreamBedY
    {
        get
        {
            return (elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;
        }
    }
    private bool hasIncomingRiver, hasOutgoingRiver;
    private HexDirection incomingRiver, outgoingRiver;

    public bool HasRiver
    {
        get { return hasIncomingRiver || hasOutgoingRiver; }
    }

    public bool HasRiverBeginOrEnd
    {
        get { return hasIncomingRiver != hasOutgoingRiver; }
    }

    public bool HasIncomingRiver
    {
        get { return hasIncomingRiver; }
    }

    public bool HasOutgoingRiver
    {
        get { return hasOutgoingRiver; }
    }

    public HexDirection IncomingRiver
    {
        get { return incomingRiver; }
    }

    public HexDirection OutgoingRiver
    {
        get { return outgoingRiver; }
    }

    public HexDirection RiverBeginOrEndDirection
    {
        get
        {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }
    #endregion

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

    [SerializeField]
    bool[] roads;

    public int WaterLevel
    {
        get
        {
            return waterLevel;
        }
        set
        {
            if (waterLevel == value)
                return;
            waterLevel = value;
            validateRivers();
            refresh();
        }
    }
    private int waterLevel;

    public float WaterSurfaceY
    {
        get
        {
            return (waterLevel + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;
        }
    }

    public bool IsUnderwater
    {
        get { return waterLevel > elevation; }
    }

    private void Start()
    {
        roads = new bool[6];
    }

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

    private void refreshSelfOnly()
    {
        chunk.Refresh();
    }

    #region Rivers
    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return 
            hasIncomingRiver && incomingRiver == direction ||
            hasOutgoingRiver && outgoingRiver == direction;
    }

    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver)
            return;
        hasOutgoingRiver = false;
        refreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.refreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver)
            return;
        hasIncomingRiver = false;
        refreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.refreshSelfOnly();
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void SetOutgoinRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction)
            return;

        HexCell neighbor = GetNeighbor(direction);
        if (!isValidRiverDestination(neighbor))
            return;

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction)
            RemoveIncomingRiver();

        hasOutgoingRiver = true;
        outgoingRiver = direction;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();

        SetRoad((int)direction, false);
    }

    private bool isValidRiverDestination(HexCell neighbor)
    {
        return neighbor &&
            (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
    }

    private void validateRivers()
    {
        if (HasOutgoingRiver &&
            !isValidRiverDestination(GetNeighbor(outgoingRiver)))
        {
            RemoveOutgoingRiver();
        }
        if(HasIncomingRiver &&
            !GetNeighbor(incomingRiver).isValidRiverDestination(this))
        {
            RemoveIncomingRiver();
        }

    }
    #endregion

    #region Roads
    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }

    public bool HasRoads
    {
        get
        {
            return Array.Exists(roads, road => road);
        }
    }

    public void RemoveRoads()
    {
        for(int i = 0; i < neighbors.Length; i++)
        {
            if(roads[i])
                SetRoad(i, false);
        }
    }

    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int)direction] && !HasRiverThroughEdge(direction) &&
            GetElevationDifference(direction) <= 1)
        {
            SetRoad((int)direction, true);
        }
    }

    public int GetElevationDifference(HexDirection direction)
    {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    private void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].refreshSelfOnly();
        refreshSelfOnly();
    }
    #endregion
}
