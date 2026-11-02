
using UnityEngine;

public class RollerCoasterCartController : MonoBehaviour
{
    [Header("Track Connection")]
    public RollerCoasterBuilder track;

    [Header("Ride Physics")]
    public float entrySpeed = 18f;
    public float gravity = 9.81f;
    public float minimumSpeed = 8f;
    public float maximumSpeed = 55f;

    [Header("Lift Hill")]
    public float liftSpeed = 5.5f;

    [Header("Acceleration and Braking")]
    public float accelerationRate = 28f;
    public float brakingRate = 16f;

    [Header("Turn Behaviour")]
    public float turnLookAhead = 10f;
    public float maximumTurnAngle = 60f;

    [Range(0f, 0.5f)]
    public float turnSlowdown = 0.12f;

    [Header("Cart Settings")]
    public float cartHeight = 0.85f;

    [Header("Cart Appearance")]
    public Color bodyColor = new Color(0.85f, 0.12f, 0.08f);
    public Color seatColor = new Color(0.12f, 0.12f, 0.15f);
    public Color wheelColor = new Color(0.06f, 0.06f, 0.06f);

    [Header("Ride Controls")]
    public bool automaticRide = true;

    [Header("Live Ride Data")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float distanceTravelled;

    private Transform cartBody;
    private bool visualsCreated = false;
    private float startingHeight;

    public float CurrentSpeed => currentSpeed;
    public float DistanceTravelled => distanceTravelled;

    // INITIALIZATION

    void Start()
    {
        if (track == null)
        {
            Debug.LogError("ROLLER COASTER TRACK NOT ASSIGNED!");
            return;
        }

        Vector3 startPosition;
        Quaternion startRotation;

        if (!track.GetTrackPose(
            0f,
            out startPosition,
            out startRotation))
        {
            Debug.LogError("TRACK DATA NOT AVAILABLE!");
            return;
        }

        startingHeight = startPosition.y;
        currentSpeed = entrySpeed;
        distanceTravelled = 0f;

        CreateCart();
        UpdateCartPosition();

        Debug.Log("THRILL RIDE INITIALIZED!");
    }

    // MAIN RIDE LOOP

    void Update()
    {
        if (track == null || !visualsCreated)
            return;

        if (track.TrackLength <= 0f)
            return;

        if (automaticRide)
        {
            UpdateRidePhysics();

            distanceTravelled +=
                currentSpeed * Time.deltaTime;

            distanceTravelled = Mathf.Repeat(
                distanceTravelled,
                track.TrackLength
            );
        }

        UpdateCartPosition();
    }

    // SPEED AND PHYSICS

    void UpdateRidePhysics()
    {
        Vector3 position;
        Quaternion rotation;

        if (!track.GetTrackPose(
            distanceTravelled,
            out position,
            out rotation))
            return;

        // HEIGHT-BASED SPEED

        float heightDifference =
            startingHeight - position.y;

        float energySpeed = Mathf.Sqrt(
            Mathf.Max(
                0f,
                entrySpeed * entrySpeed +
                2f * gravity * heightDifference
            )
        );

        energySpeed = Mathf.Clamp(
            energySpeed,
            minimumSpeed,
            maximumSpeed
        );

        // DETECT UPHILL CLIMB

        Vector3 forwardDirection =
            track.transform.TransformDirection(
                rotation * Vector3.forward
            ).normalized;

        bool climbing = forwardDirection.y > 0.12f;

        float targetSpeed = energySpeed;

        // SLOW LIFT-HILL CLIMB

        if (climbing)
        {
            targetSpeed = Mathf.Min(
                targetSpeed,
                liftSpeed
            );
        }

        // LOOK AHEAD FOR TURNS

        Vector3 futurePosition;
        Quaternion futureRotation;

        float futureDistance =
            distanceTravelled + turnLookAhead;

        if (track.GetTrackPose(
            futureDistance,
            out futurePosition,
            out futureRotation))
        {
            Vector3 currentForward =
                rotation * Vector3.forward;

            Vector3 futureForward =
                futureRotation * Vector3.forward;

            float turnAngle = Vector3.Angle(
                currentForward,
                futureForward
            );

            float turnFactor = 1f -
                Mathf.Clamp01(
                    turnAngle / maximumTurnAngle
                ) * turnSlowdown;

            targetSpeed *= turnFactor;
        }

        targetSpeed = Mathf.Clamp(
            targetSpeed,
            minimumSpeed,
            maximumSpeed
        );

        // ALLOW SLOW LIFT-HILL SPEED

        if (climbing)
        {
            targetSpeed = Mathf.Min(
                targetSpeed,
                liftSpeed
            );
        }

        // SEPARATE ACCELERATION AND BRAKING

        float movementRate =
            targetSpeed > currentSpeed
            ? accelerationRate
            : brakingRate;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            movementRate * Time.deltaTime
        );
    }

    // FOLLOW TRACK POSITION AND ROTATION

    void UpdateCartPosition()
    {
        Vector3 localPosition;
        Quaternion localRotation;

        if (!track.GetTrackPose(
            distanceTravelled,
            out localPosition,
            out localRotation))
            return;

        Quaternion worldRotation =
            track.transform.rotation * localRotation;

        Vector3 worldPosition =
            track.transform.TransformPoint(localPosition);

        worldPosition +=
            worldRotation * Vector3.up * cartHeight;

        transform.position = worldPosition;

        // Automatic pitch, roll and yaw

        transform.rotation = worldRotation;
    }

    // CREATE CART

    void CreateCart()
    {
        if (cartBody == null)
        {
            Transform existing =
                transform.Find("CartBody");

            if (existing != null)
            {
                cartBody = existing;
            }
            else
            {
                GameObject body =
                    new GameObject("CartBody");

                cartBody = body.transform;
                cartBody.SetParent(transform, false);
            }
        }

        CreatePart(
            "MainBody",
            new Vector3(0, 0.35f, 0),
            new Vector3(1.8f, 0.55f, 2.6f),
            bodyColor,
            PrimitiveType.Cube
        );

        CreatePart(
            "FrontPanel",
            new Vector3(0, 0.45f, 1.1f),
            new Vector3(1.65f, 0.45f, 0.35f),
            bodyColor,
            PrimitiveType.Cube
        );

        CreateSeat(-0.55f);
        CreateSeat(0.55f);

        CreateWheel(-0.88f, -0.05f, -0.8f);
        CreateWheel(0.88f, -0.05f, -0.8f);
        CreateWheel(-0.88f, -0.05f, 0.8f);
        CreateWheel(0.88f, -0.05f, 0.8f);

        CreatePart(
            "SafetyBar",
            new Vector3(0, 0.95f, 0),
            new Vector3(1.45f, 0.12f, 0.16f),
            seatColor,
            PrimitiveType.Cube
        );

        visualsCreated = true;

        Debug.Log("ROLLER COASTER CART CREATED!");
    }

    // SEATS

    void CreateSeat(float zPosition)
    {
        CreatePart(
            "SeatBase",
            new Vector3(0, 0.68f, zPosition),
            new Vector3(0.72f, 0.18f, 0.7f),
            seatColor,
            PrimitiveType.Cube
        );

        CreatePart(
            "SeatBack",
            new Vector3(0, 1.05f, zPosition - 0.28f),
            new Vector3(0.72f, 0.7f, 0.15f),
            seatColor,
            PrimitiveType.Cube
        );
    }

    // WHEELS

    void CreateWheel(float x, float y, float z)
    {
        GameObject wheel =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

        wheel.name = "CartWheel";

        wheel.transform.SetParent(cartBody, false);

        wheel.transform.localPosition =
            new Vector3(x, y, z);

        wheel.transform.localRotation =
            Quaternion.Euler(0, 0, 90);

        wheel.transform.localScale =
            new Vector3(0.32f, 0.16f, 0.32f);

        SetAppearance(wheel, wheelColor);
        RemoveCollider(wheel);
    }

    // CART PARTS

    void CreatePart(
        string partName,
        Vector3 position,
        Vector3 scale,
        Color color,
        PrimitiveType type)
    {
        GameObject part =
            GameObject.CreatePrimitive(type);

        part.name = partName;

        part.transform.SetParent(cartBody, false);

        part.transform.localPosition = position;
        part.transform.localScale = scale;

        SetAppearance(part, color);
        RemoveCollider(part);
    }

    // MATERIALS

    void SetAppearance(GameObject obj, Color color)
    {
        Renderer renderer =
            obj.GetComponent<Renderer>();

        if (renderer != null)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.color = color;

            renderer.material = material;
        }
    }

    // REMOVE COLLIDERS

    void RemoveCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();

        if (col != null)
            Destroy(col);
    }
}
