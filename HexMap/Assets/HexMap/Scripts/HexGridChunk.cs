using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    private HexCell[] cells;
    public HexMesh terrain, rivers, roads, water, waterShore, estuaries;
    private Canvas gridCanvas;

    private void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();

        cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
        ShowUI(false);
    }

    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    private void LateUpdate()
    {
        Triangulate(cells);
        enabled = false;
    }

    #region Triangulate methods
    public void Triangulate(HexCell[] cells)
    {
        terrain.Clear();
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();
        for (int i = 0; i < cells.Length; i++)
        {
            triangulate(cells[i]);
        }
        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();
    }

    private void triangulate(HexCell cell)
    {
        if (cell)
        {
            Vector3 center = cell.transform.localPosition;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                triangulate(d, cell);
            }
        }
    }

    private void triangulate(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.Position;
        EdgeVertices e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                e.v3.y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd)
                {
                    triangulateWithRiverBeginOrEnd(direction, cell, center, e);
                }
                else
                {
                    triangulateWithRiver(direction, cell, center, e);
                }
            }
            else
            {
                triangulateAdjacentToRiver(direction, cell, center, e);
            }
        }
        else
        {
            //triangulateEdgeFan(center, e, cell.Color);
            triangulateWithoutRiver(direction, cell, center, e);
        }

        if (direction <= HexDirection.SE)
            triangulateConnection(direction, cell, e);

        if (cell.IsUnderwater)
            triangulateWater(direction, cell, center);
    }

    private void triangulateConnection(
        HexDirection direction, HexCell cell, EdgeVertices e1)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
            return;

        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y;
        EdgeVertices e2 = new EdgeVertices(
            e1.v1 + bridge,
            e1.v5 + bridge
        );

        if (cell.HasRiverThroughEdge(direction))
        {
            e2.v3.y = neighbor.StreamBedY;

            if (!cell.IsUnderwater)
            {
                if (!neighbor.IsUnderwater)
                {
                    triangulateRiverQuad(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                        cell.HasIncomingRiver && cell.IncomingRiver == direction);
                }
                else if(cell.Elevation > neighbor.WaterLevel)
                {
                    triangulateWaterfallInWater(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        neighbor.WaterSurfaceY);
                }
            }else if(
                !neighbor.IsUnderwater &&
                neighbor.Elevation > cell.WaterLevel)
            {
                triangulateWaterfallInWater(
                    e2.v4, e2.v2, e1.v4, e1.v2,
                    neighbor.RiverSurfaceY, cell.RiverSurfaceY,
                    cell.WaterSurfaceY);
            }
        }

        if (cell.GetEdgeType(direction) == HexEdgeType.Slop)
        {
            triangulateEdgeTerraces(
                e1, cell, e2, neighbor, cell.HasRoadThroughEdge(direction));
        }
        else
        {
            triangulateEdgeStrip(
                e1, cell.Color, e2, neighbor.Color,
                cell.HasRoadThroughEdge(direction));
        }

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    triangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
                }
                else
                {
                    triangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                triangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
            }
            else
            {
                triangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
            }
        }
    }

    private void triangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slop)
        {
            if (rightEdgeType == HexEdgeType.Slop)
                triangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            else if (rightEdgeType == HexEdgeType.Flat)
                triangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            else
                triangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
        }
        else if (rightEdgeType == HexEdgeType.Slop)
        {
            if (leftEdgeType == HexEdgeType.Flat)
                triangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            else
                triangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slop)
        {
            if (leftCell.Elevation < rightCell.Elevation)
                triangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            else
                triangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
        }
        else
        {
            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }

    private void triangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0) b = -b;
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

        triangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slop)
        {
            triangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void triangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0) b = -b;
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

        triangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slop)
        {
            triangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void triangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor)
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }

    private void triangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleColor(beginCell.Color, c3, c4);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadColor(c1, c2, c3, c4);
        }

        terrain.AddQuad(v3, v4, left, right);
        terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
    }

    private void triangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell,
        bool hasRoad)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        triangulateEdgeStrip(begin, beginCell.Color, e2, c2, hasRoad);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            triangulateEdgeStrip(e1, c1, e2, c2, hasRoad);
        }

        triangulateEdgeStrip(e2, c2, end, endCell.Color, hasRoad);
    }

    private void triangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v4, edge.v5);
        terrain.AddTriangleColor(color);
    }

    private void triangulateEdgeStrip(
        EdgeVertices e1, Color c1,
        EdgeVertices e2, Color c2,
        bool hasRoad = false)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        terrain.AddQuadColor(c1, c2);

        if (hasRoad)
        {
            triangulateRoadSegment(
                e1.v2, e1.v3, e1.v4, 
                e2.v2, e2.v3, e2.v4);
        }
    }

    #region Triangulate river methods
    private void triangulateWithRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR =
                center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) *
                (0.5f * HexMetrics.InnerToOuter);
        }
        else
        {
            centerL =
                center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) *
                (0.5f * HexMetrics.InnerToOuter);
            centerR = center;
        }
        center = Vector3.Lerp(centerL, centerR, 0.5f);

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(centerL, e.v1, 0.5f),
            Vector3.Lerp(centerR, e.v5, 0.5f),
            1f / 6f);
        m.v3.y = center.y = e.v3.y;
        triangulateEdgeStrip(m, cell.Color, e, cell.Color);

        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddTriangleColor(cell.Color);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuadColor(cell.Color);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddQuadColor(cell.Color);
        terrain.AddTriangle(centerR, m.v4, m.v5);
        terrain.AddTriangleColor(cell.Color);

        if (!cell.IsUnderwater)
        {
            bool reversed = cell.IncomingRiver == direction;
            triangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
            triangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
        }
    }

    private void triangulateWithoutRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        triangulateEdgeFan(center, e, cell.Color);

        if (cell.HasRoads)
        {
            Vector2 interpolators = GetRoadInterpolators(direction, cell);
            triangulateRoad(
                center,
                Vector3.Lerp(center, e.v1, interpolators.x),
                Vector3.Lerp(center, e.v5, interpolators.y),
                e, cell.HasRoadThroughEdge(direction));
        }
    }

    private void triangulateWithRiverBeginOrEnd(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f));
        m.v3.y = e.v3.y;

        triangulateEdgeStrip(m, cell.Color, e, cell.Color);
        triangulateEdgeFan(center, m, cell.Color);

        if (!cell.IsUnderwater)
        {
            bool reversed = cell.HasIncomingRiver;
            triangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);

            center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
            rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed)
            {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
            }
            else
            {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
            }
        }
    }

    private void triangulateAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        if (cell.HasRoads)
            triangulateRoadAdjacentToRiver(direction, cell, center, e);
        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.InnerToOuter * 0.5f);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous2()))
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (
            cell.HasRiverThroughEdge(direction.Previous()) &&
            cell.HasRiverThroughEdge(direction.Next2()))
        {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f));

        triangulateEdgeStrip(m, cell.Color, e, cell.Color);
        triangulateEdgeFan(center, m, cell.Color);
    }

    private void triangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float v, bool reversed)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        rivers.AddQuad(v1, v2, v3, v4);
        if (reversed)
        {
            rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
        }
        else
        {
            rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
        }
    }

    private void triangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y, float v, bool reversed)
    {
        triangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
    }

    private void triangulateWaterfallInWater(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float waterY)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);
        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);
        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
    }
    #endregion

    #region Triangulate road methods
    private void triangulateRoad(
        Vector3 center, Vector3 mL, Vector3 mR, 
        EdgeVertices e, bool hasRoadThroughCellEdge)
    {
        if (hasRoadThroughCellEdge)
        {
            Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
            triangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);
            roads.AddTriangle(center, mL, mC);
            roads.AddTriangle(center, mC, mR);
            roads.AddTriangleUV(
                new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            roads.AddTriangleUV(
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
        }
        else
        {
            triangulateRoadEdge(center, mL, mR);
        }
    }

    private void triangulateRoadSegment(
        Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 v4, Vector3 v5, Vector3 v6)
    {
        roads.AddQuad(v1, v2, v4, v5);
        roads.AddQuad(v2, v3, v5, v6);
        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
    }

    private void triangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
    {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangleUV(
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
    }

    private Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
    {
        Vector2 interpolators = Vector2.zero;
        if (cell.HasRoadThroughEdge(direction))
        {
            interpolators.x = interpolators.y = 0.5f;
        }
        else
        {
            interpolators.x =
                cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.y =
                cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }
        return interpolators;
    }

    private void triangulateRoadAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        bool nexHasRiver = cell.HasRiverThroughEdge(direction.Next());
        Vector2 interpolators = GetRoadInterpolators(direction, cell);
        Vector3 roadCenter = center;

        if (cell.HasRiverBeginOrEnd)
        {
            roadCenter += 
                HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite())
                * (1f / 3f);
        }
        else if(cell.IncomingRiver == cell.OutgoingRiver.Opposite())
        {
            Vector3 corner;
            if (previousHasRiver)
            {
                if(!hasRoadThroughEdge &&
                    !cell.HasRoadThroughEdge(direction.Next()))
                    return;
                corner = HexMetrics.GetSecondSolidCorner(direction);
            }
            else
            {
                if (!hasRoadThroughEdge &&
                    !cell.HasRoadThroughEdge(direction.Previous()))
                    return;
                corner = HexMetrics.GetFirstSolidCorner(direction);
            }
            roadCenter += corner * 0.5f;
            center += corner * 0.25f;
        }
        else if(cell.IncomingRiver == cell.OutgoingRiver.Previous())
        {
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
        }
        else if(cell.IncomingRiver == cell.OutgoingRiver.Next())
        {
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
        }
        else if(previousHasRiver && nexHasRiver)
        {
            if (!hasRoadThroughEdge)
                return;
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) *
                HexMetrics.InnerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        else
        {
            HexDirection middle;
            if (previousHasRiver)
                middle = direction.Next();
            else if (nexHasRiver)
                middle = direction.Previous();
            else
                middle = direction;
            if (
                !cell.HasRoadThroughEdge(middle) &&
                !cell.HasRoadThroughEdge(middle.Previous()) &&
                !cell.HasRoadThroughEdge(middle.Next()))
            {
                return;
            }
            roadCenter += HexMetrics.GetSolidEdgeMiddle(middle) * 0.25f;
        }

        Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
        triangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);
        if (previousHasRiver)
        {
            triangulateRoadEdge(roadCenter, center, mL);
        }
        if (nexHasRiver)
        {
            triangulateRoadEdge(roadCenter, mR, center);
        }
    }
    #endregion

    #region Triangulate water methods
    private void triangulateWater(HexDirection direction, HexCell cell, Vector3 center)
    {
        center.y = cell.WaterSurfaceY;

        HexCell neighbor = cell.GetNeighbor(direction);
        if(neighbor != null && !neighbor.IsUnderwater)
        {
            triangulateWaterShore(direction, cell, neighbor, center);
        }
        else
        {
            triangulateOpenWater(direction, cell, neighbor, center);
        }
    }

    private void triangulateOpenWater(
        HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

        water.AddTriangle(center, c1, c2);

        if(direction <= HexDirection.SE && neighbor != null)
        {
            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            water.AddQuad(c1, c2, e1, e2);
            if (direction <= HexDirection.E)
            {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater)
                    return;
                water.AddTriangle(
                    c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
            }
        }
    }

    private void triangulateWaterShore(
        HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(direction),
            center + HexMetrics.GetSecondWaterCorner(direction));
        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);

        Vector3 center2 = neighbor.Position;
        center2.y = center.y;
        EdgeVertices e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite()));
        if (cell.HasRiverThroughEdge(direction))
        {
            triangulateEstuary(e1, e2, cell.IncomingRiver == direction);
        }
        else
        {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        }

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if(nextNeighbor != null)
        {
            Vector3 v3 = nextNeighbor.Position + (nextNeighbor.IsUnderwater ?
                HexMetrics.GetFirstWaterCorner(direction.Previous()) :
                HexMetrics.GetFirstSolidCorner(direction.Previous()));
            v3.y = center.y;
            waterShore.AddTriangle(e1.v5, e2.v5, v3);
            waterShore.AddTriangleUV(
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f));
        }
    }

    private void triangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver)
    {
        waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
        waterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        waterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
        estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        estuaries.AddQuadUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 0f));
        estuaries.AddTriangleUV(
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        estuaries.AddQuadUV(
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f));

        if (incomingRiver)
        {
            estuaries.AddQuadUV2(
                new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),
                new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
            estuaries.AddTriangleUV2(
                new Vector2(0.5f, 1.1f),
                new Vector2(1f, 0.8f),
                new Vector3(0f, 0.8f));
            estuaries.AddQuadUV2(
                new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
                new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
        }
        else
        {
            estuaries.AddQuadUV2(
                new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                new Vector3(0f, 0f), new Vector2(0.5f, -0.3f));
            estuaries.AddTriangleUV2(
                new Vector2(0.5f, -0.3f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f));
            estuaries.AddQuadUV2(
                new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                new Vector2(1f, 0f), new Vector3(1.5f, -0.2f));
        }
    }
    #endregion
    #endregion

    public void Refresh()
    {
        enabled = true;
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }
}
