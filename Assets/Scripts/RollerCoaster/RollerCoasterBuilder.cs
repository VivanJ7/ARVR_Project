
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RollerCoasterBuilder : MonoBehaviour
{
    [Header("Track Resolution")]
    [Range(8, 24)]
    public int subdivisions = 16;

    [Header("Track Dimensions")]
    public float trackWidth = 2.4f;
    public float railThickness = 0.24f;
    public float sleeperThickness = 0.16f;
    public int sleeperInterval = 2;
    public int supportInterval = 8;

    [Header("Track Colours")]
    public Color railColor = new Color(0.32f, 0.40f, 0.52f);
    public Color supportColor = new Color(0.16f, 0.19f, 0.24f);
    public Color sleeperColor = new Color(0.08f, 0.10f, 0.14f);

    private readonly List<Vector3> points = new List<Vector3>();
    private readonly List<float> banks = new List<float>();
    private readonly List<bool> liftSections = new List<bool>();
    private readonly List<float> cumulative = new List<float>();

    private Transform geometryRoot;

    private Material railMaterial;
    private Material supportMaterial;
    private Material sleeperMaterial;

    public float TrackLength { get; private set; }
    public bool IsReady { get; private set; }

    private struct TrackPoint
    {
        public Vector3 position;
        public float bank;
        public bool lift;

        public TrackPoint(Vector3 p, float b = 0f, bool l = false)
        {
            position = p;
            bank = b;
            lift = l;
        }
    }

    // =====================================================
    // COMPLETE CLOSED ROLLER COASTER ROUTE
    // =====================================================

    private readonly TrackPoint[] path =
    {
        // 1. LONG STRAIGHT
        new TrackPoint(new Vector3(-110, 2, -80)),
        new TrackPoint(new Vector3(-60, 2, -80)),
        new TrackPoint(new Vector3(0, 2, -80)),
        new TrackPoint(new Vector3(60, 2, -80)),

        // 2. HORIZONTAL RIGHT TURN
        new TrackPoint(new Vector3(105, 2, -65), 5),
        new TrackPoint(new Vector3(125, 2, -25), 10),
        new TrackPoint(new Vector3(120, 4, 20), 15),

        // 3. LARGE BANKED TURN
        new TrackPoint(new Vector3(95, 8, 60), 28),
        new TrackPoint(new Vector3(55, 12, 85), 35),
        new TrackPoint(new Vector3(10, 18, 90), 25),

        // 4. BANKED CLIMB
        new TrackPoint(new Vector3(-35, 26, 75), 15, true),
        new TrackPoint(new Vector3(-75, 40, 45), 5, true),

        // 5. CREST
        new TrackPoint(new Vector3(-105, 48, 5)),

        // 6. STEEP FIRST DROP
        new TrackPoint(new Vector3(-95, 10, -30)),

        // 7. SECOND HILL
        new TrackPoint(new Vector3(-60, 18, -5), -12),
        new TrackPoint(new Vector3(-20, 7, 20), -20),

        // 8. BANKED CURVE WITH ELEVATION CHANGE
        new TrackPoint(new Vector3(25, 12, 45), -30),
        new TrackPoint(new Vector3(65, 7, 25), -25),

        // 9. RETURNING CURVES
        new TrackPoint(new Vector3(75, 2, -15), -10),
        new TrackPoint(new Vector3(45, 2, -45)),
        new TrackPoint(new Vector3(-10, 2, -45)),
        new TrackPoint(new Vector3(-65, 2, -45)),
        new TrackPoint(new Vector3(-105, 2, -55))
    };

    // =====================================================
    // EDITOR GENERATION
    // =====================================================

    [ContextMenu("Generate Final Roller Coaster")]
    public void GenerateTrackInEditor()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Stop Play mode before generating.");
            return;
        }

        ClearGeneratedGeometry();

        BuildTrackData();
        CreateMaterials();

        if (!IsReady)
        {
            Debug.LogError("Track data generation failed.");
            return;
        }

        geometryRoot =
            new GameObject("GeneratedTrackGeometry").transform;

        geometryRoot.SetParent(transform, false);

        geometryRoot.localPosition = Vector3.zero;
        geometryRoot.localRotation = Quaternion.identity;
        geometryRoot.localScale = Vector3.one;

        BuildGeometry();

        Debug.Log(
            "FINAL ROLLER COASTER GENERATED. Length: " +
            TrackLength.ToString("F1") + " metres."
        );
    }

    [ContextMenu("Clear Generated Roller Coaster")]
    public void ClearTrackInEditor()
    {
        if (Application.isPlaying)
            return;

        ClearGeneratedGeometry();

        points.Clear();
        banks.Clear();
        liftSections.Clear();
        cumulative.Clear();

        TrackLength = 0f;
        IsReady = false;
    }

    // =====================================================
    // RUNTIME INITIALIZATION
    // =====================================================

    private void Awake()
    {
        if (Application.isPlaying)
            PrepareRuntimeData();
    }

    public void PrepareRuntimeData()
    {
        // Validate every internal collection, not just IsReady.
        if (!HasValidTrackData())
        {
            IsReady = false;
            BuildTrackData();
        }
    }

    private bool HasValidTrackData()
    {
        return
            IsReady &&
            points.Count > 1 &&
            banks.Count == points.Count &&
            liftSections.Count == points.Count &&
            cumulative.Count == points.Count + 1 &&
            TrackLength > 0.01f &&
            !float.IsNaN(TrackLength) &&
            !float.IsInfinity(TrackLength);
    }

    // =====================================================
    // SPLINE GENERATION
    // =====================================================

    private void BuildTrackData()
    {
        IsReady = false;

        points.Clear();
        banks.Clear();
        liftSections.Clear();
        cumulative.Clear();

        TrackLength = 0f;

        int count = path.Length;
        int steps = Mathf.Clamp(subdivisions, 8, 24);

        if (count < 4)
        {
            Debug.LogError("At least four track control points are required.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            TrackPoint p0 = path[(i - 1 + count) % count];
            TrackPoint p1 = path[i];
            TrackPoint p2 = path[(i + 1) % count];
            TrackPoint p3 = path[(i + 2) % count];

            for (int j = 0; j < steps; j++)
            {
                float t = j / (float)steps;

                Vector3 position = CatmullRom(
                    p0.position,
                    p1.position,
                    p2.position,
                    p3.position,
                    t
                );

                points.Add(position);

                banks.Add(
                    Mathf.Lerp(p1.bank, p2.bank, t)
                );

                liftSections.Add(p1.lift || p2.lift);
            }
        }

        if (points.Count < 2)
        {
            Debug.LogError("Insufficient generated track points.");
            return;
        }

        cumulative.Add(0f);

        for (int i = 0; i < points.Count; i++)
        {
            int next = (i + 1) % points.Count;

            float segmentLength = Vector3.Distance(
                points[i],
                points[next]
            );

            TrackLength += segmentLength;
            cumulative.Add(TrackLength);
        }

        IsReady =
            points.Count > 1 &&
            cumulative.Count == points.Count + 1 &&
            banks.Count == points.Count &&
            liftSections.Count == points.Count &&
            TrackLength > 0.01f &&
            !float.IsNaN(TrackLength) &&
            !float.IsInfinity(TrackLength);

        if (!IsReady)
        {
            Debug.LogError("Generated track data failed validation.");
            TrackLength = 0f;
        }
    }

    // =====================================================
    // TRACK POSITION AND ORIENTATION
    // RETURNS WORLD-SPACE DATA
    // =====================================================

    public bool GetTrackPose(
        float distance,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        // Protect against empty or inconsistent internal data.
        if (!HasValidTrackData())
            return false;

        if (points.Count == 0)
            return false;

        distance = Mathf.Repeat(distance, TrackLength);

        int index = FindSegment(distance);

        if (index < 0 || index >= points.Count)
            return false;

        // Safe because points.Count has already been validated.
        int next = (index + 1) % points.Count;

        if (next < 0 || next >= points.Count)
            return false;

        if (index + 1 >= cumulative.Count)
            return false;

        float segmentLength =
            cumulative[index + 1] - cumulative[index];

        float t = segmentLength > 0.0001f
            ? (distance - cumulative[index]) / segmentLength
            : 0f;

        t = Mathf.Clamp01(t);

        Vector3 localPosition = Vector3.Lerp(
            points[index],
            points[next],
            t
        );

        Vector3 tangent =
            (points[next] - points[index]).normalized;

        if (tangent.sqrMagnitude < 0.0001f)
            return false;

        Vector3 referenceUp =
            Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.95f
            ? Vector3.forward
            : Vector3.up;

        Vector3 right =
            Vector3.Cross(referenceUp, tangent).normalized;

        Vector3 up =
            Vector3.Cross(tangent, right).normalized;

        float bank = Mathf.Lerp(
            banks[index],
            banks[next],
            t
        );

        up = Quaternion.AngleAxis(bank, tangent) * up;

        Quaternion localRotation =
            Quaternion.LookRotation(tangent, up);

        // Convert to world coordinates exactly once.
        position = transform.TransformPoint(localPosition);
        rotation = transform.rotation * localRotation;

        return true;
    }

    // =====================================================
    // FIVE MOTION OUTPUTS
    // =====================================================

    public bool GetMotionAngles(
        float distance,
        out float pitch,
        out float roll,
        out float yaw)
    {
        pitch = 0f;
        roll = 0f;
        yaw = 0f;

        if (!GetTrackPose(
            distance,
            out _,
            out Quaternion rotation))
            return false;

        Vector3 forward = rotation * Vector3.forward;

        pitch = Mathf.Atan2(
            forward.y,
            Mathf.Sqrt(
                forward.x * forward.x +
                forward.z * forward.z
            )
        ) * Mathf.Rad2Deg;

        roll = GetBankAtDistance(distance);

        float heading = Mathf.Atan2(
            forward.x,
            forward.z
        ) * Mathf.Rad2Deg;

        if (!GetTrackPose(
            0f,
            out _,
            out Quaternion startRotation))
            return false;

        Vector3 startForward =
            startRotation * Vector3.forward;

        float startHeading = Mathf.Atan2(
            startForward.x,
            startForward.z
        ) * Mathf.Rad2Deg;

        yaw = Mathf.DeltaAngle(startHeading, heading);

        return true;
    }

    // =====================================================
    // BANKING
    // =====================================================

    private float GetBankAtDistance(float distance)
    {
        if (!HasValidTrackData())
            return 0f;

        distance = Mathf.Repeat(distance, TrackLength);

        int i = FindSegment(distance);

        if (i < 0 || i >= points.Count)
            return 0f;

        int next = (i + 1) % points.Count;

        float length =
            cumulative[i + 1] - cumulative[i];

        float t = length > 0.0001f
            ? (distance - cumulative[i]) / length
            : 0f;

        return Mathf.Lerp(banks[i], banks[next], t);
    }

    // =====================================================
    // LIFT HILL DETECTION
    // =====================================================

    public bool IsLiftSection(float distance)
    {
        if (!HasValidTrackData())
            return false;

        distance = Mathf.Repeat(distance, TrackLength);

        int index = FindSegment(distance);

        if (index < 0 || index >= liftSections.Count)
            return false;

        return liftSections[index];
    }

    // =====================================================
    // SAFE BINARY SEARCH
    // =====================================================

    private int FindSegment(float distance)
    {
        if (points.Count < 2 ||
            cumulative.Count != points.Count + 1)
            return -1;

        int low = 0;
        int high = points.Count - 1;

        while (low < high)
        {
            int middle = (low + high + 1) / 2;

            if (cumulative[middle] <= distance)
                low = middle;
            else
                high = middle - 1;
        }

        return Mathf.Clamp(low, 0, points.Count - 1);
    }

    // =====================================================
    // TRACK GEOMETRY
    // =====================================================

    private void BuildGeometry()
    {
        if (points.Count < 2)
            return;

        for (int i = 0; i < points.Count; i++)
        {
            int next = (i + 1) % points.Count;

            GetFrame(i, out Vector3 left, out Vector3 right);

            GetFrame(
                next,
                out Vector3 nextLeft,
                out Vector3 nextRight
            );

            CreateBeam(
                "LeftRail",
                left,
                nextLeft,
                railThickness,
                railMaterial
            );

            CreateBeam(
                "RightRail",
                right,
                nextRight,
                railThickness,
                railMaterial
            );

            if (i % Mathf.Max(1, sleeperInterval) == 0)
            {
                CreateBeam(
                    "Sleeper",
                    left,
                    right,
                    sleeperThickness,
                    sleeperMaterial
                );
            }

            if (i % Mathf.Max(1, supportInterval) == 0)
            {
                Vector3 groundLeft =
                    new Vector3(left.x, 0.15f, left.z);

                Vector3 groundRight =
                    new Vector3(right.x, 0.15f, right.z);

                CreateBeam(
                    "Support",
                    groundLeft,
                    left,
                    0.30f,
                    supportMaterial
                );

                CreateBeam(
                    "Support",
                    groundRight,
                    right,
                    0.30f,
                    supportMaterial
                );
            }
        }
    }

    // =====================================================
    // TRACK FRAME
    // =====================================================

    private void GetFrame(
        int index,
        out Vector3 left,
        out Vector3 right)
    {
        int next = (index + 1) % points.Count;

        Vector3 tangent =
            (points[next] - points[index]).normalized;

        Vector3 referenceUp =
            Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.95f
            ? Vector3.forward
            : Vector3.up;

        Vector3 side =
            Vector3.Cross(referenceUp, tangent).normalized;

        Vector3 up =
            Vector3.Cross(tangent, side).normalized;

        up = Quaternion.AngleAxis(
            banks[index],
            tangent
        ) * up;

        Quaternion frame =
            Quaternion.LookRotation(tangent, up);

        left = points[index] +
            frame * Vector3.left * (trackWidth * 0.5f);

        right = points[index] +
            frame * Vector3.right * (trackWidth * 0.5f);
    }

    // =====================================================
    // CREATE TRACK BEAMS
    // =====================================================

    private void CreateBeam(
        string objectName,
        Vector3 start,
        Vector3 end,
        float thickness,
        Material material)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        if (length < 0.001f)
            return;

        GameObject beam =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        beam.name = objectName;

        beam.transform.SetParent(geometryRoot, false);

        beam.transform.localPosition =
            (start + end) * 0.5f;

        beam.transform.localRotation =
            Quaternion.FromToRotation(
                Vector3.up,
                direction.normalized
            );

        beam.transform.localScale =
            new Vector3(thickness, length, thickness);

        Renderer renderer = beam.GetComponent<Renderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider col = beam.GetComponent<Collider>();

        if (col != null)
            DestroyImmediate(col);
    }

    // =====================================================
    // MATERIALS
    // =====================================================

    private void CreateMaterials()
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        railMaterial = new Material(shader);
        supportMaterial = new Material(shader);
        sleeperMaterial = new Material(shader);

        railMaterial.color = railColor;
        supportMaterial.color = supportColor;
        sleeperMaterial.color = sleeperColor;
    }

    // =====================================================
    // CLEAR OLD GENERATED OBJECTS
    // =====================================================

    private void ClearGeneratedGeometry()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (child.name == "GeneratedTrackGeometry" ||
                child.name == "GeneratedTrack" ||
                child.name == "TrackPart")
            {
                DestroyImmediate(child.gameObject);
            }
        }

        if (railMaterial != null)
            DestroyImmediate(railMaterial);

        if (supportMaterial != null)
            DestroyImmediate(supportMaterial);

        if (sleeperMaterial != null)
            DestroyImmediate(sleeperMaterial);

        railMaterial = null;
        supportMaterial = null;
        sleeperMaterial = null;
        geometryRoot = null;
    }

    // =====================================================
    // CATMULL-ROM SPLINE
    // =====================================================

    private static Vector3 CatmullRom(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 +
             4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 -
             3f * p2 + p3) * t3
        );
    }
}
