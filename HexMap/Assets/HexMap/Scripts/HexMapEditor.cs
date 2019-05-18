using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;

    public HexGrid hexGrid;

    private Color activeColor;
    private int activeElevation;

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
            editCell(hexGrid.GetCell(hit.point));
        }
    }

    private void editCell(HexCell cell)
    {
        cell.Color = activeColor;
        cell.Elevation = activeElevation;
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void SelectColor(int index)
    {
        Debug.Log("Select " + index);
        activeColor = colors[index];
    }
}
