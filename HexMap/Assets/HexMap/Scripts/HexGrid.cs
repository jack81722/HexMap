using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    #region Cell fields
    private int chunkCountX, chunkCountZ;
    public int cellCountX = 20, cellCountZ = 15;
    public HexCell[] cells;
    private HexGridChunk[] chunks;
    #endregion

    #region Prefab fields
    public HexCell cellPrefab;
    public TextMeshProUGUI cellLabelPrefab;
    public HexGridChunk chunkPrefab;
    #endregion

    public int seed;
    public Texture2D noiseSource;

    #region Pathfinding fields
    private HexCellPriorityQueue searchFrontier;
    private int searchFrontierPhase;
    private HexCell currentPathFrom, currentPathTo;
    private bool currentPathExists;
    public bool HasPath
    {
        get { return currentPathExists; }
    }
    #endregion

    public HexUnit unitPrefab;
    private List<HexUnit> units = new List<HexUnit>();

    HexCellShaderData cellShaderData;

    private void Awake()
    {
        HexMetrics.NoiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexUnit.unitPrefab = unitPrefab;
        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        CreateMap(cellCountX, cellCountZ);
    }

    private void OnEnable()
    {
        if (!HexMetrics.NoiseSource)
        {
            HexMetrics.NoiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;
        }
    }
    
    public bool CreateMap(int x, int z)
    {
        if(x <= 0 || x % HexMetrics.ChunkSizeX != 0 ||
           z <= 0 || z % HexMetrics.ChunkSizeZ != 0)
        {
            Debug.LogError("Unsupported map size.");
            return false;
        }
        ClearPath();
        clearUnits();
        if (chunks != null)
        {
            for(int i = 0; i < chunks.Length; i++)
            {
                Destroy(chunks[i].gameObject);
            }
        }

        cellCountX = x;
        cellCountZ = z;
        chunkCountX = cellCountX / HexMetrics.ChunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.ChunkSizeZ;
        cellShaderData.Initialize(cellCountX, cellCountZ);
        createChunks();
        createCells();

        return true;
    }

    private void createChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for(int z = 0, i = 0; z < chunkCountZ; z++)
            for(int x= 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
    }

    private void createCells()
    {
        cells = new HexCell[cellCountX * cellCountZ];

        for (int z = 0, i = 0; z < cellCountZ; z++)
            for (int x = 0; x < cellCountX; x++)
                createCell(x, z, i++);
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinate coordinate = HexCoordinate.FromPosition(position);
        int index = coordinate.X + coordinate.Z * cellCountX + coordinate.Z / 2;
        return cells[index];
    }

    public HexCell GetCell(HexCoordinate coordinate)
    {
        int z = coordinate.Z;
        if (z < 0 || z >= cellCountZ)
            return null;
        int x = coordinate.X + z / 2;
        if (x < 0 || x >= cellCountX)
            return null;
        return cells[x + z * cellCountX];
    }

    private void createCell(int x, int z, int i)
    {
        // set position
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
        position.y = 0;
        position.z = z * (HexMetrics.OuterRadius * 1.5f);

        // create cell game object
        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.localPosition = position;
        cell.coordinate = HexCoordinate.FromOffsetCoordinates(x, z);
        cell.Index = i;
        cell.ShaderData = cellShaderData;

        // register neighbors
        if(x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if(z > 0)
        {
            if((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if(x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        // mark label of coordinate
        TextMeshProUGUI label = Instantiate<TextMeshProUGUI>(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        cell.uiRect = label.rectTransform;

        cell.Elevation = 0;
        addCellToChunk(x, z, cell);
    }

    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit))
        {
            return GetCell(hit.point);
        }
        return null;
    }

    private void addCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.ChunkSizeX;
        int chunkZ = z / HexMetrics.ChunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.ChunkSizeX;
        int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);
    }

    public void ShowUI(bool visible)
    {
        for(int i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
    }

    #region Save/Load methods
    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);

        for(int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }

        writer.Write(units.Count);
        for(int i = 0; i < units.Count; i++)
        {
            units[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header)
    {
        ClearPath();
        clearUnits();
        int x = 20, z = 15;
        if (header >= 1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }
        if (x != cellCountX || z != cellCountZ)
        {
            if (!CreateMap(x, z))
            {
                return;
            }
        }

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader);
        }
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Refresh();
        }

        if (header >= 2)
        {
            int unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++)
            {
                HexUnit.Load(reader, this);
            }
        }
    }
    #endregion

    #region Pathfinding methods
    public void FindPath(HexCell fromCell, HexCell toCell, int speed)
    {
        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell, speed);
        ShowPath(speed);
    }

    private bool Search(HexCell fromCell, HexCell toCell, int speed)
    {
        searchFrontierPhase += 2;
        if (searchFrontier == null)
            searchFrontier = new HexCellPriorityQueue();
        else
            searchFrontier.Clear();
        
        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;
            if (current == toCell)
            {
                return true;
            }

            int currentTurn = (current.Distance - 1) / speed;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null ||
                    neighbor.SearchPhase > searchFrontierPhase)
                    continue;
                if (neighbor.IsUnderwater || neighbor.Unit)
                    continue;
                HexEdgeType edgeType = current.GetEdgeType(neighbor);
                if (edgeType == HexEdgeType.Cliff)
                    continue;

                int moveCost;
                if (current.HasRoadThroughEdge(d))
                {
                    moveCost = 1;
                }
                else if(current.Walled != neighbor.Walled)
                {
                    continue;
                }
                else
                {
                    moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
                    moveCost += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                }
                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed;
                if(turn > currentTurn)
                {
                    distance = turn * speed + moveCost;
                }

                if(neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.coordinate.DistanceTo(toCell.coordinate);
                    searchFrontier.Enqueue(neighbor);
                }
                else if(distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return false;
    }

    public List<HexCell> GetPath()
    {
        if (!currentPathExists)
        {
            return null;
        }
        List<HexCell> path = ListPool<HexCell>.Get();
        for(HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
        {
            path.Add(c);
        }
        path.Add(currentPathFrom);
        path.Reverse();
        return path;
    }

    private void ShowPath(int speed)
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while(current != currentPathFrom)
            {
                int turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = current.PathFrom;
            }
        }
        currentPathFrom.EnableHighlight(Color.blue);
        currentPathTo.EnableHighlight(Color.red);
    }

    public void ClearPath()
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while(current != currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHightlight();
                current = current.PathFrom;
            }
            current.DisableHightlight();
            currentPathExists = false;
        }
        else if (currentPathFrom)
        {
            currentPathFrom.DisableHightlight();
            currentPathTo.DisableHightlight();
        }
        currentPathFrom = currentPathTo = null;
    }
    #endregion

    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        units.Add(unit);
        unit.Grid = this;
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
        unit.Die();
    }

    private void clearUnits()
    {
        for(int i = 0; i < units.Count; i++)
        {
            units[i].Die();
        }
        units.Clear();
    }

    private List<HexCell> GetVisibleCells(HexCell fromCell, int range)
    {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();

        searchFrontierPhase += 2;
        if (searchFrontier == null)
            searchFrontier = new HexCellPriorityQueue();
        else
            searchFrontier.Clear();

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;
            visibleCells.Add(current);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null ||
                    neighbor.SearchPhase > searchFrontierPhase)
                    continue;
                
                int distance = current.Distance + 1;
                if (distance > range)
                    continue;

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return visibleCells;
    }

    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for(int i = 0; i < cells.Count; i++)
        {
            cells[i].IncreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    public void DecreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].DecreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }
}
