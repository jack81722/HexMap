using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    public static HexMapCamera instance;

    public HexGrid grid;
    private Transform swivel, stick;
    public float stickMinZoom, stickMaxZoom;
    public float swivelMinZoom, swivelMaxZoom;
    public float moveSpeedMinZoom, moveSpeedMaxZoom;
    public float rotationAngle;
    public float rotationSpeed = 180f;
    private float zoom = 1f;

    private void Awake()
    {
        instance = this;
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    public static bool Locked
    {
        set { instance.enabled = !value; }
    }

    private void Start()
    {
        adjustZoom(0);
        adjustPosition(0, 0);
    }

    private void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
            adjustZoom(zoomDelta);

        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0)
            adjustRotation(rotationDelta);

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0 || zDelta != 0f)
            adjustPosition(xDelta, zDelta);

    }

    private void adjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    private void adjustPosition(float xDelta, float zDelta)
    {
        Vector3 direction = 
            transform.localRotation *
            new Vector3(xDelta, 0f, zDelta).normalized;
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        float distance =
            Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) * 
            damping * Time.deltaTime;

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = clampPosition(position);
    }

    private Vector3 clampPosition(Vector3 position)
    {
        float xMax = 
            (grid.cellCountX - 0.5f) * 
            (2f * HexMetrics.InnerRadius);
        position.x = Mathf.Clamp(position.x, 0f, xMax);

        float zMax = 
            (grid.cellCountZ - 1) * 
            (1.5f * HexMetrics.OuterRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);
        return position;
    }

    private void adjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        if (rotationAngle < 0f)
            rotationAngle += 360f;
        else if (rotationAngle >= 360f)
            rotationAngle -= 360f;
        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
    }

    public static void ValidatePosition()
    {
        instance.adjustPosition(0f, 0f);
    }
}
