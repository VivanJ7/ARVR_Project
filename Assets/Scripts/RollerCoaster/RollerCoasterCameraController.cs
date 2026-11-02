
using UnityEngine;

[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
public class RollerCoasterCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform cart;

    [Header("Camera")]
    public KeyCode switchKey = KeyCode.C;
    public bool startInRiderView = true;

    [Header("Eye Position")]
    public float headRadius = 0.17f;
    public float distanceInFrontOfFace = 0.025f;
    public float eyeHeightAdjustment = 0.02f;

    [Header("Rider")]
    public bool createRider = true;

    private Transform cartBody;
    private Transform riderRoot;
    private Transform riderHead;

    private Vector3 overviewPosition;
    private Quaternion overviewRotation;

    private bool riderView;
    private bool initialized;

    void Awake()
    {
        overviewPosition = transform.position;
        overviewRotation = transform.rotation;
        riderView = startInRiderView;
    }

    void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            riderView = !riderView;
            Debug.Log(riderView
                ? "FIRST-PERSON RIDER VIEW"
                : "OVERVIEW CAMERA");
        }
    }

    void LateUpdate()
    {
        // The cart is created at runtime, so keep searching until found.
        if (cart == null)
        {
            GameObject cartObject =
                GameObject.Find("RollerCoasterCart");

            if (cartObject == null)
                return;

            cart = cartObject.transform;
        }

        if (cartBody == null)
        {
            cartBody = cart.Find("CartBody");

            if (cartBody == null)
                return;
        }

        if (!initialized)
        {
            if (createRider)
                CreateRider();

            initialized = true;
        }

        if (riderHead == null)
            return;

        if (riderView)
        {
            // Position the camera just outside the front of the head.
            Vector3 forward = riderHead.forward.normalized;
            Vector3 up = cart.up;

            Vector3 eyePosition =
                riderHead.position
                + forward * (headRadius + distanceInFrontOfFace)
                + up * eyeHeightAdjustment;

            Quaternion eyeRotation =
                Quaternion.LookRotation(forward, up);

            // Direct assignment: no interpolation from the overview camera.
            transform.SetPositionAndRotation(
                eyePosition,
                eyeRotation
            );
        }
        else
        {
            // Restore the original overview camera position.
            transform.SetPositionAndRotation(
                overviewPosition,
                overviewRotation
            );
        }
    }

    // =====================================================
    // RIDER CREATION
    // =====================================================

    void CreateRider()
    {
        Transform existing =
            cartBody.Find("FrontSeatRider");

        if (existing != null)
        {
            riderRoot = existing;
            riderHead = existing.Find("Head");

            Debug.Log("Using existing front seat rider.");
            return;
        }

        GameObject rider =
            new GameObject("FrontSeatRider");

        riderRoot = rider.transform;

        riderRoot.SetParent(cartBody, false);
        riderRoot.localPosition = Vector3.zero;
        riderRoot.localRotation = Quaternion.identity;
        riderRoot.localScale = Vector3.one;

        float seatZ = 0.55f;

        Material shirt = MakeMaterial(
            new Color(0.08f, 0.22f, 0.65f)
        );

        Material pants = MakeMaterial(
            new Color(0.12f, 0.13f, 0.16f)
        );

        Material skin = MakeMaterial(
            new Color(0.78f, 0.55f, 0.38f)
        );

        Material hair = MakeMaterial(
            new Color(0.08f, 0.055f, 0.035f)
        );

        Material shoes = MakeMaterial(
            new Color(0.035f, 0.035f, 0.04f)
        );

        MakePart("Torso", PrimitiveType.Capsule,
            new Vector3(0f, 1.12f, seatZ - 0.12f),
            new Vector3(0.48f, 0.43f, 0.30f), shirt);

        MakePart("Neck", PrimitiveType.Cylinder,
            new Vector3(0f, 1.56f, seatZ),
            new Vector3(0.12f, 0.12f, 0.12f), skin);

        MakePart("Head", PrimitiveType.Sphere,
            new Vector3(0f, 1.82f, seatZ + 0.07f),
            new Vector3(0.34f, 0.40f, 0.34f), skin);

        MakePart("Hair", PrimitiveType.Sphere,
            new Vector3(0f, 1.98f, seatZ + 0.04f),
            new Vector3(0.35f, 0.20f, 0.35f), hair);

        MakePart("LeftThigh", PrimitiveType.Capsule,
            new Vector3(-0.14f, 0.76f, seatZ + 0.18f),
            new Vector3(0.19f, 0.35f, 0.19f), pants);

        MakePart("RightThigh", PrimitiveType.Capsule,
            new Vector3(0.14f, 0.76f, seatZ + 0.18f),
            new Vector3(0.19f, 0.35f, 0.19f), pants);

        MakePart("LeftLeg", PrimitiveType.Capsule,
            new Vector3(-0.14f, 0.40f, seatZ + 0.40f),
            new Vector3(0.15f, 0.30f, 0.15f), pants);

        MakePart("RightLeg", PrimitiveType.Capsule,
            new Vector3(0.14f, 0.40f, seatZ + 0.40f),
            new Vector3(0.15f, 0.30f, 0.15f), pants);

        MakePart("LeftShoe", PrimitiveType.Cube,
            new Vector3(-0.14f, 0.13f, seatZ + 0.48f),
            new Vector3(0.19f, 0.13f, 0.30f), shoes);

        MakePart("RightShoe", PrimitiveType.Cube,
            new Vector3(0.14f, 0.13f, seatZ + 0.48f),
            new Vector3(0.19f, 0.13f, 0.30f), shoes);

        MakePart("LeftArm", PrimitiveType.Capsule,
            new Vector3(-0.32f, 1.15f, seatZ + 0.12f),
            new Vector3(0.13f, 0.34f, 0.13f), shirt);

        MakePart("RightArm", PrimitiveType.Capsule,
            new Vector3(0.32f, 1.15f, seatZ + 0.12f),
            new Vector3(0.13f, 0.34f, 0.13f), shirt);

        MakePart("LeftHand", PrimitiveType.Sphere,
            new Vector3(-0.32f, 0.82f, seatZ + 0.18f),
            new Vector3(0.13f, 0.13f, 0.13f), skin);

        MakePart("RightHand", PrimitiveType.Sphere,
            new Vector3(0.32f, 0.82f, seatZ + 0.18f),
            new Vector3(0.13f, 0.13f, 0.13f), skin);

        riderHead = riderRoot.Find("Head");

        Debug.Log("Front seat rider created.");
    }

    // =====================================================
    // BODY PART CREATION
    // =====================================================

    void MakePart(
        string partName,
        PrimitiveType type,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject part =
            GameObject.CreatePrimitive(type);

        part.name = partName;

        part.transform.SetParent(riderRoot, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider col = part.GetComponent<Collider>();

        if (col != null)
            Destroy(col);
    }

    Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find(
            "Universal Render Pipeline/Lit"
        );

        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = color;

        return material;
    }
}
