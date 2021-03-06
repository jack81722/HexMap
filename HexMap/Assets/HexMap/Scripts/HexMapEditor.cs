﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{   
    public HexGrid hexGrid;
    public Material terrainMaterial;

    private int activeTerrainTypeIndex;

    private bool applyElevation = true;
    private int activeElevation;

    private bool applyWaterLevel = false;
    private int activeWaterLevel;

    #region Feature properties
    private bool applyUrbanLevel = false;
    private int activeUrbanLevel;

    private bool applyFarmLevel = false;
    private int activeFarmLevel;

    private bool applyPlantLevel = false;
    private int activePlantLevel;

    private bool applySpecialIndex = false;
    private int activeSpecialIndex;
    #endregion

    private int brushSize;

    private enum OptionalToggle
    {
        Ignore, Yes, No
    }
    private OptionalToggle riverMode, roadMode, walledMode;

    private bool isDrag;
    private HexDirection dragDirection;
    private HexCell previousCell;

    private void Awake()
    {
        terrainMaterial.DisableKeyword("GRID_ON");
        SetEditMode(false);
    }

    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButton(0))
            {
                HandleInput();
                return;
            }
            if (Input.GetKeyDown(KeyCode.U))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    destroyUnit();
                }
                else
                {
                    createUnit();
                }
                return;
            }
        }
        previousCell = null;
    }

    private void HandleInput()
    {
        HexCell currentCell = getCellUnderCursor();
        if (currentCell)
        {
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
            if (activeTerrainTypeIndex >= 0)
                cell.TerrainTypeIndex = activeTerrainTypeIndex;
            if (applyElevation)
                cell.Elevation = activeElevation;
            if (applyWaterLevel)
                cell.WaterLevel = activeWaterLevel;
            if (applyUrbanLevel)
                cell.UrbanLevel = activeUrbanLevel;
            if (applyFarmLevel)
                cell.FarmLevel = activeFarmLevel;
            if (applyPlantLevel)
                cell.PlantLevel = activePlantLevel;
            if (riverMode == OptionalToggle.No)
                cell.RemoveRiver();
            if (roadMode == OptionalToggle.No)
                cell.RemoveRoads();
            if (walledMode != OptionalToggle.Ignore)
                cell.Walled = walledMode == OptionalToggle.Yes;
            if (applySpecialIndex)
                cell.SpecialIndex = activeSpecialIndex;
            else if (isDrag)
            {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    if (riverMode == OptionalToggle.Yes)
                        otherCell.SetOutgoinRiver(dragDirection);
                    if (roadMode == OptionalToggle.Yes)
                        otherCell.AddRoad(dragDirection);

                }
            }
        }
    }

    private void createUnit()
    {
        HexCell cell = getCellUnderCursor();
        if (cell && !cell.Unit)
        {
            hexGrid.AddUnit(
                Instantiate(HexUnit.unitPrefab), cell, Random.Range(0f, 360f));
        }
    }

    private void destroyUnit()
    {
        HexCell cell = getCellUnderCursor();
        if(cell && cell.Unit)
        {
            hexGrid.RemoveUnit(cell.Unit);
        }
    }

    private HexCell getCellUnderCursor()
    {
        return hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
    }

    #region Edit mode set methods
    #region Elevation/Terrain setting methods
    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void SetTerrainTypeIndex(int index)
    {
        activeTerrainTypeIndex = index;
    }
    #endregion

    #region Water/River setting methods
    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }

    public void SetApplyWaterLevel(bool toggle)
    {
        applyWaterLevel = toggle;
    }

    public void SetWaterLevel(float level)
    {
        activeWaterLevel = (int)level;
    }
    #endregion

    #region Feature setting methods
    public void SetApplyUrbanLevel(bool toggle)
    {
        applyUrbanLevel = toggle;
    }

    public void SetUrbanLevel(float level)
    {
        activeUrbanLevel = (int)level;
    }

    public void SetApplyFarmLevel(bool toggle)
    {
        applyFarmLevel = toggle;
    }

    public void SetFarmLevel(float level)
    {
        activeFarmLevel = (int)level;
    }

    public void SetApplyPlantLevel(bool toggle)
    {
        applyPlantLevel = toggle;
    }

    public void SetPlantLevel(float level)
    {
        activePlantLevel = (int)level;
    }

    public void SetWalledMode(int mode)
    {
        walledMode = (OptionalToggle)mode;
    }

    public void SetApplySpecialIndex (bool toggle)
    {
        applySpecialIndex = toggle;
    }

    public void SetSpecialIndex(float index)
    {
        activeSpecialIndex = (int)index;
    }
    #endregion

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle)mode;
    }

    public void ShowGrid(bool visible)
    {
        if (visible)
            terrainMaterial.EnableKeyword("GRID_ON");
        else
            terrainMaterial.DisableKeyword("GRID_ON");
    }

    public void SetEditMode(bool toggle)
    {
        enabled = toggle;
    }
    #endregion
}


