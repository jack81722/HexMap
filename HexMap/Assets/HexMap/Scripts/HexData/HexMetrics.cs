using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HexMetrics
{
    #region Basic hex factors
    public const float OuterRadius = 10f;
    public const float InnerRadius = OuterRadius * 0.866025404f;

    public static Vector3[] corners =
    {
        new Vector3(0f, 0f, OuterRadius),
        new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(0f, 0f, -OuterRadius),
        new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius)
    };

    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return corners[(int)direction];
    }

    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return corners[((int)direction + 1) % corners.Length];
    }

    public static Vector3 GetBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[((int)direction + 1) % corners.Length]) * BlendFactor;
    }
    #endregion

    #region Color factors
    public const float SolidFactor = 0.8f;
    public const float BlendFactor = 1f - SolidFactor;

    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return corners[(int)direction] * SolidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        return corners[((int)direction + 1) % corners.Length] * SolidFactor;
    }
    #endregion

    #region Elevation factors
    public const float ElevationStep = 3f;
    public const int TerracesPerSlop = 2;
    public const int TerraceSteps = TerracesPerSlop * 2 + 1;
    public const float HorizontalTerraceStepSize = 1f / TerraceSteps;
    public const float VerticalTerraceStepSize = 1f / (TerracesPerSlop + 1);

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
            return HexEdgeType.Flat;
        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1)
            return HexEdgeType.Slop;
        return HexEdgeType.Cliff;
    }

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HorizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * VerticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HorizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }
    #endregion

    #region Irregularity
    public static Texture2D NoiseSource;
    public const float NoiseScale = 0.003f;
    public const float CellPerturbStrength = 4f;
    public const float ElevationPerturbStrength = 1.5f;

    public static Vector4 SampleNoise(Vector3 position)
    {
        return NoiseSource.GetPixelBilinear(
            position.x * NoiseScale,
            position.z * NoiseScale);
    }
    #endregion

    public const int ChunkSizeX = 5, ChunkSizeZ = 5;


}
