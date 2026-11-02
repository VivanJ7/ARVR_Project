using UnityEngine;

public class CubeController : MonoBehaviour
{
    [Header("Axis Motor Objects")]
    public Transform xAxisMotor;
    public Transform yAxisMotor;
    public Transform zAxisMotor;

    [Header("Rotation Settings")]
    public float rotationSpeed = 70f;
    public float maxAngle = 50f;
    // Starting orientations
    private Vector3 xStartRotation;
    private Vector3 yStartRotation;
    private Vector3 zStartRotation;

    // Current motion angles
    private float xAngle = 0f;
    private float yAngle = 0f;
    private float zAngle = 0f;

    // Reset state
    private bool resetting = false;

    void Start()
    {
        xStartRotation = xAxisMotor.localEulerAngles;
        yStartRotation = yAxisMotor.localEulerAngles;
        zStartRotation = zAxisMotor.localEulerAngles;
    }

    void Update()
    {
        // ==========================================
        // RESET TO 0,0,0
        // Press R
        // ==========================================

        if (Input.GetKeyDown(KeyCode.R))
        {
            resetting = true;
        }

        if (resetting)
        {
            ResetToCenter();
        }
        else
        {
            ManualControl();
        }

        ApplyRotation();
    }

    // ==========================================
    // MANUAL CONTROL
    // ==========================================

    void ManualControl()
    {
        // X AXIS
        if (Input.GetKey(KeyCode.UpArrow))
            xAngle += rotationSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.DownArrow))
            xAngle -= rotationSpeed * Time.deltaTime;

        // Y AXIS
        if (Input.GetKey(KeyCode.RightArrow))
            yAngle += rotationSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftArrow))
            yAngle -= rotationSpeed * Time.deltaTime;

        // Z AXIS
        if (Input.GetKey(KeyCode.I))
            zAngle += rotationSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.K))
            zAngle -= rotationSpeed * Time.deltaTime;

        // Limit movement
        xAngle = Mathf.Clamp(xAngle, -maxAngle, maxAngle);
        yAngle = Mathf.Clamp(yAngle, -maxAngle, maxAngle);
        zAngle = Mathf.Clamp(zAngle, -maxAngle, maxAngle);
    }

    // ==========================================
    // RESET TO CENTER
    // ==========================================

    void ResetToCenter()
    {
        xAngle = Mathf.MoveTowards(
            xAngle,
            0f,
            rotationSpeed * Time.deltaTime
        );

        yAngle = Mathf.MoveTowards(
            yAngle,
            0f,
            rotationSpeed * Time.deltaTime
        );

        zAngle = Mathf.MoveTowards(
            zAngle,
            0f,
            rotationSpeed * Time.deltaTime
        );

        // Stop resetting once everything reaches zero
        if (Mathf.Approximately(xAngle, 0f) &&
            Mathf.Approximately(yAngle, 0f) &&
            Mathf.Approximately(zAngle, 0f))
        {
            xAngle = 0f;
            yAngle = 0f;
            zAngle = 0f;

            resetting = false;
        }
    }

    // ==========================================
    // APPLY ROTATION
    // ==========================================

    void ApplyRotation()
    {
        xAxisMotor.localRotation =
            Quaternion.Euler(
                xStartRotation + new Vector3(xAngle, 0f, 0f)
            );

        yAxisMotor.localRotation =
            Quaternion.Euler(
                yStartRotation + new Vector3(0f, yAngle, 0f)
            );

        zAxisMotor.localRotation =
            Quaternion.Euler(
                zStartRotation + new Vector3(0f, 0f, zAngle)
            );
    }

    // ==========================================
    // VALUES SENT TO ESP32
    // ==========================================

    public float XAngle => xAngle;
    public float YAngle => yAngle;
    public float ZAngle => zAngle;
}