using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    public HexFeatureCollection[] urbanCollections, farmCollections, plantCollections;
    Transform container;

    public HexMesh walls;

    public void Clear()
    {
        if (container)
            Destroy(container.gameObject);
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
        walls.Clear();
    }

    public void Apply()
    {
        walls.Apply();
    }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        HexHash hash = HexMetrics.SampleHashGrid(position);
        Transform prefab = pickPrefab(
            urbanCollections, cell.UrbanLevel, hash.a, hash.d);
        Transform otherPrefab = pickPrefab(
            farmCollections, cell.FarmLevel, hash.b, hash.d);
        float usedHash = hash.a;
        if (prefab)
        {
            if(otherPrefab && hash.b < hash.a)
            {
                prefab = otherPrefab;
                usedHash = hash.b;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
            usedHash = hash.b;
        }
        otherPrefab = pickPrefab(plantCollections, cell.PlantLevel, hash.c, hash.d);
        if (prefab)
        {
            if (otherPrefab && hash.c < usedHash)
                prefab = otherPrefab;
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
        }
        else
        {
            return;
        }
        Transform instance = Instantiate(prefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
        instance.SetParent(container, false);
    }

    private Transform pickPrefab(
        HexFeatureCollection[] collection,
        int level, float hash, float choice)
    {
        if(level > 0)
        {
            float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for(int i = 0; i < thresholds.Length; i++)
            {
                if (hash < thresholds[i])
                    return collection[i].Pick(choice);
            }
        }
        return null;
    }

    public void AddWall(
        EdgeVertices near, HexCell nearCell,
        EdgeVertices far, HexCell farCell,
        bool hasRiver, bool hasRoad)
    {
        if(nearCell.Walled != farCell.Walled &&
            !nearCell.IsUnderwater && !farCell.IsUnderwater &&
            nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff)
        {
            addWallSegment(near.v1, far.v1, near.v2, far.v2);
            if (hasRiver || hasRoad)
            {
                addWallCap(near.v2, far.v2);
                addWallCap(far.v4, near.v4);
            }
            else
            { 
                addWallSegment(near.v2, far.v2, near.v3, far.v3);
                addWallSegment(near.v3, far.v3, near.v4, far.v4);
            }
            addWallSegment(near.v4, far.v4, near.v5, far.v5);
        }
    }

    public void AddWall(
        Vector3 c1, HexCell cell1,
        Vector3 c2, HexCell cell2,
        Vector3 c3, HexCell cell3)
    {
        if (cell1.Walled)
        {
            if (cell2.Walled)
            {
                if (!cell3.Walled)
                {
                    addWallSegment(c3, cell3, c1, cell1, c2, cell2);
                }
            }
            else if (cell3.Walled)
            {
                addWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
            else
            {
                addWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
        }
        else if (cell2.Walled)
        {
            if (cell3.Walled)
            {
                addWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
            else
            {
                addWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
        }
        else if (cell3.Walled)
        {
            addWallSegment(c3, cell3, c1, cell1, c2, cell2);
        }
    }

    private void addWallSegment(
        Vector3 pivot, HexCell pivotCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        if (pivotCell.IsUnderwater)
            return;

        bool hasLeftWall = !leftCell.IsUnderwater &&
            pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
        bool hasRightWall = !rightCell.IsUnderwater &&
            pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

        if (hasLeftWall)
        {
            if (hasRightWall)
            {
                addWallSegment(pivot, left, pivot, right);
            }
            else if(leftCell.Elevation < rightCell.Elevation)
            {
                addWallWedge(pivot, left, right);
            }
            else
            {
                addWallCap(pivot, left);
            }
        }
        else if (hasRightWall)
        {
            if (rightCell.Elevation < leftCell.Elevation)
            {
                addWallWedge(right, pivot, left);
            }
            else
            {
                addWallCap(right, pivot);
            }
        }
    }

    private void addWallSegment(
        Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight)
    {
        nearLeft = HexMetrics.Perturb(nearLeft);
        farLeft = HexMetrics.Perturb(farLeft);
        nearRight = HexMetrics.Perturb(nearRight);
        farRight = HexMetrics.Perturb(farRight);

        Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
        Vector3 right = HexMetrics.WallLerp(nearRight, farRight);
        float leftTop = left.y + HexMetrics.WallHeight;
        float rightTop = right.y + HexMetrics.WallHeight;

        Vector3 leftThicknessOffset =
            HexMetrics.WallThicknessOffset(nearLeft, farLeft);
        Vector3 rightThicknessOffset =
            HexMetrics.WallThicknessOffset(nearRight, farRight);

        Vector3 v1, v2, v3, v4;
        v1 = v3 = left - leftThicknessOffset;
        v2 = v4 = right - rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v1, v2, v3, v4);

        Vector3 t1 = v3, t2 = v4;

        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v2, v1, v4, v3);

        walls.AddQuadUnperturbed(t1, t2, v3, v4);
    }

    private void addWallCap(Vector3 near, Vector3 far)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = center.y + HexMetrics.WallHeight;
        walls.AddQuadUnperturbed(v1, v2, v3, v4);
    }

    private void addWallWedge(Vector3 near, Vector3 far, Vector3 point)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);
        point = HexMetrics.Perturb(point);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        Vector3 pointTop = point;
        point.y = center.y;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = pointTop.y = center.y + HexMetrics.WallHeight;

        walls.AddQuadUnperturbed(v1, point, v3, pointTop);
        walls.AddQuadUnperturbed(point, v2, pointTop, v4);
        walls.AddTriangleUnperturbed(pointTop, v3, v4);
    }
}
