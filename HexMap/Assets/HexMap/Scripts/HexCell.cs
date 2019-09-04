using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            refreshPosition();
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

    #region Terrain properties
    public int TerrainTypeIndex
    {
        get { return terrainTypeIndex; }
        set
        {
            if(terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                ShaderData.RefreshTerrain(this);
            }
        }
    }
    private int terrainTypeIndex;
    #endregion

    public RectTransform uiRect;

    public Vector3 Position
    {
        get { return transform.localPosition; }
    }

    [SerializeField]
    public HexCell[] neighbors;

    [SerializeField]
    private bool[] roads;

    #region Water properties
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
    #endregion

    #region Feature properties
    private int urbanLevel, farmLevel, plantLevel;
    public int UrbanLevel
    {
        get { return urbanLevel; }
        set
        {
            if(urbanLevel != value)
            {
                urbanLevel = value;
                refreshSelfOnly();
            }
        }
    }
    public int FarmLevel
    {
        get { return farmLevel; }
        set
        {
            if(farmLevel != value)
            {
                farmLevel = value;
                refreshSelfOnly();
            }
        }
    }
    public int PlantLevel
    {
        get { return plantLevel; }
        set
        {
            if(plantLevel != value)
            {
                plantLevel = value;
                refreshSelfOnly();
            }
        }
    }

    int specialIndex;
    public int SpecialIndex
    {
        get { return specialIndex; }
        set
        {
            if(specialIndex != value && !HasRiver)
            {
                specialIndex = value;
                RemoveRoads();
                refreshSelfOnly();
            }
        }
    }

    public bool IsSpecial
    {
        get
        {
            return specialIndex > 0;
        }
    }
    #endregion

    #region Wall properties
    private bool walled;
    public bool Walled
    {
        get
        {
            return walled;
        }
        set
        {
            if(walled != value)
            {
                walled = value;
                refresh();
            }
        }
    }
    #endregion

    #region Distance properties
    private int distance;
    public int Distance
    {
        get { return distance; }
        set
        {
            distance = value;
        }
    }
    #endregion

    #region Pathfinding properties
    public HexCell PathFrom { get; set; }
    public int SearchHeuristic { get; set; }
    public int SearchPriority
    {
        get
        {
            return distance + SearchHeuristic;
        }
    }
    public int SearchPhase { get; set; }
    public HexCell NextWithSamePriority { get; set; }
    #endregion

    public HexUnit Unit { get; set; }

    public HexCellShaderData ShaderData { get; set; }
    public int Index { get; set; }
    public bool IsVisible
    {
        get
        {
            return visibility > 0;
        }
    }
    int visibility;

    public bool IsExplored { get; private set; }

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

    private void refresh()
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
            if (Unit)
            {
                Unit.ValidateLocation();
            }
        }
    }

    private void refreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.ElevationStep;
        position.y +=
            (HexMetrics.SampleNoise(position).y * 2f - 1f) *
            HexMetrics.ElevationPerturbStrength;
        transform.localPosition = position;

        // set ui position
        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    private void refreshSelfOnly()
    {
        chunk.Refresh();
        if (Unit)
        {
            Unit.ValidateLocation();
        }
    }

    public void DisableHightlight()
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void EnableHighlight(Color color)
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
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
        specialIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.specialIndex = 0;

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
        if (!roads[(int)direction] && 
            !HasRiverThroughEdge(direction) &&
            GetElevationDifference(direction) <= 1 &&
            !IsSpecial && !GetNeighbor(direction).IsSpecial)
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

    #region Save/Load methods
    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write((byte)specialIndex);
        writer.Write(walled);

        if (hasIncomingRiver)
        {
            writer.Write((byte)(incomingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }
        if (hasOutgoingRiver)
        {
            writer.Write((byte)(outgoingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }

        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++)
        {
            if (roads[i])
            {
                roadFlags |= 1 << i;
            }
        }
        writer.Write((byte)roadFlags);
        writer.Write(IsExplored);
    }

    public void Load(BinaryReader reader, int header)
    {
        terrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);
        elevation = reader.ReadByte();
        refreshPosition();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();
        walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasIncomingRiver = false;
        }
        riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for(int i = 0; i < roads.Length; i++)
        {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }
        IsExplored = header >= 3 ? reader.ReadBoolean() : false;
        ShaderData.RefreshVisibility(this);
    }
    #endregion

    public void SetLabel(string text)
    {
        TextMeshProUGUI label = uiRect.GetComponent<TextMeshProUGUI>();
        label.text = text;
    }

    public void IncreaseVisibility()
    {
        visibility++;
        if(visibility == 1)
        {
            IsExplored = true;
            ShaderData.RefreshVisibility(this);
        }
    }

    public void DecreaseVisibility()
    {
        visibility--;
        if(visibility == 0)
        {
            ShaderData.RefreshVisibility(this);
        }
    }
}
