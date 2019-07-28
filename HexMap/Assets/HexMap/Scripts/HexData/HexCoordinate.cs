using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HexCoordinate
{
    [UnityEngine.SerializeField]
    private int x;
    [UnityEngine.SerializeField]
    private int z;

    public int X { get { return x; } }
    public int Y { get { return -X - Z; } }
    public int Z { get { return z; } }

    public HexCoordinate(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static HexCoordinate FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinate(x - z / 2, z);
    }

    public override string ToString()
    {
        return string.Format("({0}, {1}, {2})", X, Y, Z);
    }

    public string ToStringOnSeparateLine()
    {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }

    public static HexCoordinate FromPosition(Vector3 position)
    {
        float x = position.x / (HexMetrics.InnerRadius * 2f);
        float y = -x;

        float offset = position.z / (HexMetrics.OuterRadius * 3f);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if(iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinate(iX, iZ);
    }

    public int DistanceTo(HexCoordinate other)
    {
        return 
            ((x < other.x ? other.x - x : x - other.x) +
            (Y < other.Y ? other.Y - Y : Y - other.Y) +
            (z < other.z ? other.z - z : z - other.z)) / 2;
    }
}
