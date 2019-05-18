using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{   
    public Color[] colors;

    public HexGrid hexGrid;

    private bool applyColor;
    private Color activeColor;

    private bool applyElevation = true;
    private int activeElevation;

    private int brushSize;

    private void Awake()
    {
        if (colors.Length <= 0)
            colors = new Color[] { Color.white };
        SelectColor(0);
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) &&
            !EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            editCells(hexGrid.GetCell(hit.point));
        }
    }

    private void editCells(HexCell center)
    {
        int centerX = center.coordinate.X;
        int centerZ = center.coordinate.Z;

        for(int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
        {
            for(int x = centerX - r; x <= centerX + brushSize; x++)
            {
                editCell(hexGrid.GetCell(new HexCoordinate(x, z)));
            }
        }

        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - brushSize; x <= centerX + r; x++)
            {
                editCell(hexGrid.GetCell(new HexCoordinate(x, z)));
            }
        }
    }

    private void editCell(HexCell cell)
    {
        if (cell)
        {
            if (applyColor)
                cell.Color = activeColor;
            if (applyElevation)
                cell.Elevation = activeElevation;
        }
    }

    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void SelectColor(int index)
    {
        applyColor = index >= 0;
        if(applyColor)
            activeColor = colors[index];
    }

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    public void ShowUI(bool visible)
    {
        hexGrid.ShowUI(visible);
    }
}
