﻿using System.Collections;
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

    private enum OptionalToggle
    {
        Ignore, Yes, No
    }
    private OptionalToggle riverMode;

    private bool isDrag;
    private HexDirection dragDirection;
    private HexCell previousCell;

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
        else
        {
            previousCell = null;
        }
    }

    private void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            HexCell currentCell = hexGrid.GetCell(hit.point);
            if (previousCell && previousCell != currentCell)
                validateDrag(currentCell);
            else
                isDrag = false;
            editCells(currentCell);
            previousCell = currentCell;
        }
        else
        {
            previousCell = null;
        }
    }

    private void validateDrag(HexCell currentCell)
    {
        for(
            dragDirection = HexDirection.NE;
            dragDirection <= HexDirection.NW;
            dragDirection++)
        {
            if(previousCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }
        isDrag = false;
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
            if (riverMode == OptionalToggle.No)
                cell.RemoveRiver();
            else if (isDrag && riverMode == OptionalToggle.Yes)
            {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    otherCell.SetOutgoinRiver(dragDirection);
                }
                //previousCell.SetOutgoinRiver(dragDirection);
            }
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

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }
}
