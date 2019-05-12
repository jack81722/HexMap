using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int width = 6;
    public int height = 6;

    public Color defaultColor = Color.white;

    private HexMesh hexMesh;

    public HexCell cellPrefab;
    public TextMeshProUGUI cellLabelPrefab;

    private Canvas gridCanvas;

    public HexCell[] cells;

    public Texture2D noiseSource;

    private void Awake()
    {
        HexMetrics.NoiseSource = noiseSource;

        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();

        cells = new HexCell[width * height];

        for (int z = 0, i = 0; z < height; z++)
            for (int x = 0; x < width; x++)
                CreateCell(x, z, i++);
    }

    private void Start()
    {
        hexMesh.Triangulate(cells);
    }

    /// <summary>
    /// Draw color of cell
    /// </summary>
    public HexCell GetCell(Vector3 position)
    {   
        position = transform.InverseTransformPoint(position);
        HexCoordinate coordinate = HexCoordinate.FromPosition(position);
        int index = coordinate.X + coordinate.Z * width + coordinate.Z / 2;
        return cells[index];
    }

    public void Refresh()
    {
        hexMesh.Triangulate(cells);
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
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.coordinate = HexCoordinate.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;

        // register neighbors
        if(x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if(z > 0)
        {
            if((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - width]);
                if(x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - width]);
                if (x < width - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
                }
            }
        }

        // mark label of coordinate
        TextMeshProUGUI label = Instantiate<TextMeshProUGUI>(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.coordinate.ToStringOnSeparateLine();
        cell.uiRect = label.rectTransform;

        cell.Elevation = 0;
    }

    

}
