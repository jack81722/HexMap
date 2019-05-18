using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    private Mesh hexMesh;
    private static List<Vector3> vertices = new List<Vector3>();
    private static List<int> triangles = new List<int>();
    private static List<Color> colors = new List<Color>();

    private MeshCollider meshCollider;

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        hexMesh.name = "Hex Mesh";
    }

    private Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;
        return position;
    }

    #region Triangulate methods
    public void Triangulate(HexCell[] cells)
    {
        hexMesh.Clear();
        vertices.Clear();
        triangles.Clear();
        colors.Clear();
        for(int i = 0; i < cells.Length; i++)
        {
            triangulate(cells[i]);
        }
        hexMesh.vertices = vertices.ToArray();
        hexMesh.triangles = triangles.ToArray();
        hexMesh.colors = colors.ToArray();
        hexMesh.RecalculateNormals();

        meshCollider.sharedMesh = hexMesh;
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
        triangulateEdgeFan(center, e, cell.Color);

        if(direction <= HexDirection.SE)
            triangulateConnection(direction, cell, e);
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
            e1.v4 + bridge
        );

        if (cell.GetEdgeType(direction) == HexEdgeType.Slop)
        {
            triangulateEdgeTerraces(e1, cell, e2, neighbor);
        }
        else
        {
            triangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color);
        }

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if(direction <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = e1.v4 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if(cell.Elevation <= nextNeighbor.Elevation)
                {
                    triangulateCorner(e1.v4, cell, e2.v4, neighbor, v5, nextNeighbor);
                }
                else
                {
                    triangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
                }
            }
            else if(neighbor.Elevation <= nextNeighbor.Elevation)
            {
                triangulateCorner(e2.v4, neighbor, v5, nextNeighbor, e1.v4, cell);
            }
            else
            {
                triangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
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
            addTriangle(bottom, left, right);
            addTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }

    private void triangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0) b = -b;
        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

        triangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

        if(leftCell.GetEdgeType(rightCell) == HexEdgeType.Slop)
        {
            triangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            addTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            addTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void triangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0) b = -b;
        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

        triangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slop)
        {
            triangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            addTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            addTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void triangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor)
    {
        Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        addTriangleUnperturbed(Perturb(begin), v2, boundary);
        addTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            addTriangleUnperturbed(v1, v2, boundary);
            addTriangleColor(c1, c2, boundaryColor);
        }

        addTriangleUnperturbed(v2, Perturb(left), boundary);
        addTriangleColor(c2, leftCell.Color, boundaryColor);
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

        addTriangle(begin, v3, v4);
        addTriangleColor(beginCell.Color, c3, c4);

        for(int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            addQuad(v1, v2, v3, v4);
            addQuadColor(c1, c2, c3, c4);
        }

        addQuad(v3, v4, left, right);
        addQuadColor(c3, c4, leftCell.Color, rightCell.Color);
    }

    private void triangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        triangulateEdgeStrip(begin, beginCell.Color, e2, c2);

        for(int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            triangulateEdgeStrip(e1, c1, e2, c2);
        }

        triangulateEdgeStrip(e2, c2, end, endCell.Color);
    }

    private void triangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        addTriangle(center, edge.v1, edge.v2);
        addTriangleColor(color);
        addTriangle(center, edge.v2, edge.v3);
        addTriangleColor(color);
        addTriangle(center, edge.v3, edge.v4);
        addTriangleColor(color);
    }

    private void triangulateEdgeStrip(
        EdgeVertices e1, Color c1,
        EdgeVertices e2, Color c2)
    {
        addQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        addQuadColor(c1, c2);
        addQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        addQuadColor(c1, c2);
        addQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        addQuadColor(c1, c2);
    }

    private void addTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }
    #endregion


    #region Private add color methods
    private void addTriangleColor(Color color)
    {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    private void addTriangleColor(Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    private void addTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    private void addQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        vertices.Add(Perturb(v4));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    private void addQuadColor(Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }

    private void addQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }
    #endregion
}
