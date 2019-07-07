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

    private bool applyWaterLevel = false;
    private int activeWaterLevel;

    #region Feature properties
    private bool applyUrbanLevel = false;
    private int activeUrbanLevel;

    private bool applyFarmLevel = false;
    private int activeFarmLevel;

    private bool applyPlantLevel = false;
    private int activePlantLevel;
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
        if (colors.Length <= 0)
            colors = new Color[] { Color.white };
        SelectColor(-1);
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

    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle)mode;
    }

    public void SetApplyWaterLevel(bool toggle)
    {
        applyWaterLevel = toggle;
    }

    public void SetWaterLevel(float level)
    {
        activeWaterLevel = (int)level;
    }

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
    #endregion

    public void SetWalledMode(int mode)
    {
        walledMode = (OptionalToggle)mode;
    }
}


