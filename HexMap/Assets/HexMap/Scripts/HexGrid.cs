using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int chunkCountX = 4, chunkCountZ = 3;
    private int cellCountX, cellCountZ;

    public Color defaultColor = Color.white;

    public HexCell cellPrefab;
    public TextMeshProUGUI cellLabelPrefab;

    public HexGridChunk chunkPrefab;
    public Texture2D noiseSource;

    public HexCell[] cells;
    private HexGridChunk[] chunks;

    private void Awake()
    {
        HexMetrics.NoiseSource = noiseSource;

        cellCountX = chunkCountX * HexMetrics.ChunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.ChunkSizeZ;

        createChunks();
        createCells();
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
                CreateCell(x, z, i++);
    }

    /// <summary>
    /// Draw color of cell
    /// </summary>
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

    private void CreateCell(int x, int z, int i)
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
        cell.Color = defaultColor;

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
        label.text = cell.coordinate.ToStringOnSeparateLine();
        cell.uiRect = label.rectTransform;

        cell.Elevation = 0;
        addCellToChunk(x, z, cell);
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
}
