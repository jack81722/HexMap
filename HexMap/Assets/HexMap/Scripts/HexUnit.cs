﻿using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Collections;

public class HexUnit : MonoBehaviour
{
    public static HexUnit unitPrefab;

    private HexCell location;
    public HexCell Location
    {
        get { return location; }
        set
        {
            if (location)
                location.Unit = null;
            location = value;
            value.Unit = this;
            transform.localPosition = value.Position;
        }
    }

    private float orientation;
    public float Orientation
    {
        get { return orientation; }
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    public void ValidateLocation()
    {
        transform.localPosition = location.Position;
    }

    public bool IsValidDestination(HexCell cell)
    {
        return !cell.IsUnderwater && !cell.Unit;
    }

    private List<HexCell> pathToTravel;
    const float travelSpeed = 4f;
    const float rotationSpeed = 180f;

    private void OnEnable()
    {
        if (location)
            transform.localPosition = location.Position;
    }

    public void Die()
    {
        location.Unit = null;
        Destroy(gameObject);
    }

    public void Travel(List<HexCell> path)
    {
        Location = path[path.Count - 1];
        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    public void Save(BinaryWriter writer)
    {
        location.coordinate.Save(writer);
        writer.Write(orientation);
    }

    public static void Load(BinaryReader reader, HexGrid grid)
    {
        HexCoordinate coordinate = HexCoordinate.Load(reader);
        float orientation = reader.ReadSingle();
        grid.AddUnit(
            Instantiate(unitPrefab), grid.GetCell(coordinate), orientation);
    }

    private IEnumerator LookAt(Vector3 point)
    {
        point.y = transform.localPosition.y;
        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation =
            Quaternion.LookRotation(point - transform.localPosition);
        float angle = Quaternion.Angle(fromRotation, toRotation);
        if (angle != 0f)
        {
            float speed = rotationSpeed / angle;

            for (float t = Time.deltaTime * speed; t < 1; t += Time.deltaTime * speed)
            {
                transform.localRotation =
                    Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }
        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
    }

    private IEnumerator TravelPath()
    {
        Vector3 a, b, c = pathToTravel[0].Position;
        transform.localPosition = c;
        yield return LookAt(pathToTravel[1].Position);
        float t = Time.deltaTime * travelSpeed;
        for(int i = 1; i < pathToTravel.Count; i++)
        {
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + pathToTravel[i].Position) * 0.5f;
            for(; t < 1f; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            t -= 1f;
        }
        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (; t < 1f; t += Time.deltaTime * travelSpeed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;
            transform.localRotation = Quaternion.LookRotation(d);
            yield return null;
        }
        transform.localPosition = location.Position;
        orientation = transform.localRotation.eulerAngles.y;

        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;
    }
}
