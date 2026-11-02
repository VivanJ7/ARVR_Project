
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class RollerCoasterEnvironment : MonoBehaviour
{
    [Header("TRACK")]
    public RollerCoasterBuilder track;

    [Header("TERRAIN")]
    [Range(129, 513)] public int terrainResolution = 257;
    public float terrainSize = 650f;
    public float terrainHeight = 100f;
    public float terrainMargin = 180f;
    public float trackClearance = 27f;

    [Header("ROLLER COASTER SUPPORTS")]
    public float supportSpacing = 12f;
    public float supportWidth = 7f;
    public float supportThickness = 0.65f;
    public float crossBraceThickness = 0.32f;

    [Header("SCENERY")]
    public int treeCount = 750;
    public int rockCount = 150;
    public int plantCount = 500;
    public int flowerCount = 120;

    [Header("MOUNTAINS")]
    public int mountainCount = 14;
    public float mountainDistance = 170f;

    [Header("PARK")]
    public bool createPaths = true;
    public bool createEntrance = true;
    public bool createAttractions = true;
    public bool createFences = true;

    [Header("LAKE")]
    public bool createLake = true;
    public Vector2 lakeSize = new Vector2(85f, 60f);

    [Header("GENERATION")]
    public int randomSeed = 42;
    public bool logMissingAssets = true;

    private const string PackRoot =
        "Assets/Pure Poly/Free Low Poly Nature Pack";

    private const string RootName = "GeneratedEnvironment";

    private Transform root;
    private Terrain terrain;
    private TerrainData terrainData;

    private readonly List<Vector3> route = new List<Vector3>();
    private Bounds routeBounds;

    private float worldSize;
    private float baseHeight;
    private float verticalSize;
    private Vector3 terrainOrigin;

    private Vector3 lakePosition;
    private float lakeLevel = 2.5f;

    private GameObject[] trees;
    private GameObject[] mountains;
    private GameObject[] rocks;
    private GameObject[] plants;
    private GameObject[] flowers;

    private Material supportMaterial;
    private Material pathMaterial;
    private Material waterMaterial;
    private Material darkMaterial;
    private Material lightMaterial;
    private Material fenceMaterial;

    private Texture2D grassTexture;
    private Texture2D dirtTexture;
    private Texture2D rockTexture;

    // =========================================================
    // GENERATION
    // =========================================================

    [ContextMenu("GENERATE COMPLETE AMUSEMENT PARK")]
    public void GenerateEnvironment()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Debug.LogWarning("Stop Play mode before generating.");
            return;
        }

        if (track == null)
        {
            Debug.LogError("Assign your RollerCoasterBuilder component.");
            return;
        }

        track.PrepareRuntimeData();

        if (!track.IsReady || track.TrackLength <= 0f)
        {
            Debug.LogError("The roller coaster track is not ready.");
            return;
        }

        Random.InitState(randomSeed);

        LoadAssets();
        SampleTrack();

        if (route.Count < 2)
        {
            Debug.LogError("Could not sample the roller coaster.");
            return;
        }

        ClearEnvironment();

        GameObject container = new GameObject(RootName);
        root = container.transform;
        root.SetParent(transform, true);
        root.position = Vector3.zero;
        root.rotation = Quaternion.identity;
        root.localScale = Vector3.one;

        CalculateBounds();
        SelectLakePosition();
        CreateMaterials();
        CreateTerrain();
        CreateLake();
        CreateMountains();
        CreateSupports();
        CreateScenery();

        if (createPaths)
        {
            CreateParkPaths();
            CreateParkDetails();
        }

        if (createFences)
            CreateParkFences();

        if (createEntrance)
            CreateEntrance();

        if (createAttractions)
            CreateAttractions();

        Debug.Log(
            "AMUSEMENT PARK GENERATED | Track: " +
            track.TrackLength.ToString("F1") +
            "m | Supports generated | Trees: " + treeCount
        );

        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("CLEAR GENERATED PARK")]
    public void ClearEnvironment()
    {
        Transform old = transform.Find(RootName);

        if (old != null)
            DestroyImmediate(old.gameObject);

        root = null;
        terrain = null;
        terrainData = null;
    }

    // =========================================================
    // ASSET LOADING
    // =========================================================

#if UNITY_EDITOR

    private void LoadAssets()
    {
        trees = LoadPrefabs(
            "PP_Birch_Tree_05",
            "PP_Birch_Tree_06",
            "PP_Tree_02",
            "PP_Tree_10"
        );

        mountains = LoadPrefabs(
            "PP_Forest_Mountain_Moss_01",
            "PP_Forest_Mountain_Moss_02"
        );

        rocks = LoadPrefabs(
            "PP_Rock_Moss_Grown_09",
            "PP_Rock_Moss_Grown_11",
            "PP_Rock_Pile_Forest_Moss_05",
            "PP_Rock_Pile_Forest_Moss_10"
        );

        plants = LoadPrefabs(
            "PP_Grass_11",
            "PP_Grass_15"
        );

        flowers = LoadPrefabs(
            "PP_Hyacinth_04",
            "PP_Sunflower_04",
            "PP_Daffodil_03"
        );
    }

    private GameObject[] LoadPrefabs(params string[] names)
    {
        List<GameObject> result = new List<GameObject>();

        foreach (string name in names)
        {
            string[] ids = AssetDatabase.FindAssets(
                name + " t:Prefab",
                new[] { PackRoot }
            );

            bool found = false;

            foreach (string id in ids)
            {
                string path = AssetDatabase.GUIDToAssetPath(id);

                if (!System.IO.Path.GetFileNameWithoutExtension(path)
                    .Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    result.Add(prefab);
                    found = true;
                    break;
                }
            }

            if (!found && logMissingAssets)
                Debug.LogWarning("Nature pack prefab not found: " + name);
        }

        return result.ToArray();
    }

#endif

    // =========================================================
    // TRACK SAMPLING
    // =========================================================

    private void SampleTrack()
    {
        route.Clear();

        int count = Mathf.Max(
            400,
            Mathf.CeilToInt(track.TrackLength / 2f)
        );

        for (int i = 0; i < count; i++)
        {
            float distance = track.TrackLength * i / count;

            if (track.GetTrackPose(
                distance,
                out Vector3 position,
                out Quaternion rotation))
            {
                route.Add(position);
            }
        }

        if (route.Count == 0)
            return;

        routeBounds = new Bounds(route[0], Vector3.zero);

        foreach (Vector3 point in route)
            routeBounds.Encapsulate(point);
    }

    private void CalculateBounds()
    {
        float required = Mathf.Max(
            routeBounds.size.x,
            routeBounds.size.z
        ) + terrainMargin * 2f;

        worldSize = Mathf.Max(terrainSize, required);

        baseHeight = Mathf.Min(-18f, routeBounds.min.y - 20f);

        verticalSize = Mathf.Max(
            terrainHeight,
            routeBounds.max.y - baseHeight + 35f
        );

        terrainOrigin = new Vector3(
            routeBounds.center.x - worldSize * 0.5f,
            baseHeight,
            routeBounds.center.z - worldSize * 0.5f
        );
    }

    private float RouteDistance(float x, float z)
    {
        float nearest = float.MaxValue;

        foreach (Vector3 p in route)
        {
            float dx = p.x - x;
            float dz = p.z - z;

            nearest = Mathf.Min(nearest, dx * dx + dz * dz);
        }

        return Mathf.Sqrt(nearest);
    }

    private float TrackHeight(float x, float z)
    {
        float nearest = float.MaxValue;
        float height = routeBounds.center.y;

        foreach (Vector3 p in route)
        {
            float dx = p.x - x;
            float dz = p.z - z;
            float d = dx * dx + dz * dz;

            if (d < nearest)
            {
                nearest = d;
                height = p.y;
            }
        }

        return height;
    }

    // =========================================================
    // MATERIALS AND TERRAIN TEXTURES
    // =========================================================

    private Material NewMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        Material m = new Material(shader);
        m.color = color;
        return m;
    }

    private void CreateMaterials()
    {
        supportMaterial = NewMaterial(
            new Color(0.16f, 0.19f, 0.23f)
        );

        pathMaterial = NewMaterial(
            new Color(0.65f, 0.59f, 0.46f)
        );

        waterMaterial = NewMaterial(
            new Color(0.035f, 0.30f, 0.43f)
        );

        darkMaterial = NewMaterial(
            new Color(0.12f, 0.14f, 0.18f)
        );

        lightMaterial = NewMaterial(
            new Color(0.95f, 0.83f, 0.55f)
        );

        fenceMaterial = NewMaterial(
            new Color(0.32f, 0.25f, 0.18f)
        );

        grassTexture = CreateNoiseTexture(
            new Color(0.19f, 0.34f, 0.12f),
            new Color(0.38f, 0.52f, 0.20f),
            256,
            12f
        );

        dirtTexture = CreateNoiseTexture(
            new Color(0.35f, 0.27f, 0.18f),
            new Color(0.56f, 0.45f, 0.30f),
            256,
            18f
        );

        rockTexture = CreateNoiseTexture(
            new Color(0.30f, 0.32f, 0.30f),
            new Color(0.53f, 0.55f, 0.50f),
            256,
            9f
        );
    }

    private Texture2D CreateNoiseTexture(
        Color a,
        Color b,
        int resolution,
        float frequency)
    {
        Texture2D tex = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGB24,
            true
        );

        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float n = Mathf.PerlinNoise(
                    x / (float)resolution * frequency,
                    y / (float)resolution * frequency
                );

                tex.SetPixel(x, y, Color.Lerp(a, b, n));
            }
        }

        tex.Apply(true);
        return tex;
    }

    private TerrainLayer MakeTerrainLayer(
        Texture2D texture,
        float tileSize)
    {
        TerrainLayer layer = new TerrainLayer();
        layer.diffuseTexture = texture;
        layer.tileSize = new Vector2(tileSize, tileSize);
        return layer;
    }

    // =========================================================
    // TERRAIN
    // =========================================================

    private void CreateTerrain()
    {
        terrainData = new TerrainData();
        terrainData.heightmapResolution = Mathf.Clamp(terrainResolution, 129, 513);
        terrainData.size = new Vector3(worldSize, verticalSize, worldSize);

        terrainData.terrainLayers = new TerrainLayer[]
        {
            MakeTerrainLayer(grassTexture, 24f),
            MakeTerrainLayer(dirtTexture, 18f),
            MakeTerrainLayer(rockTexture, 22f)
        };

        // Build heightmap.
        int heightResolution = terrainData.heightmapResolution;
        float[,] heights = new float[heightResolution, heightResolution];

        for (int z = 0; z < heightResolution; z++)
        {
            for (int x = 0; x < heightResolution; x++)
            {
                float wx = terrainOrigin.x + x / (float)(heightResolution - 1) * worldSize;
                float wz = terrainOrigin.z + z / (float)(heightResolution - 1) * worldSize;
                float h = GroundHeight(wx, wz);
                heights[z, x] = Mathf.Clamp01((h - baseHeight) / verticalSize);
            }
        }

        terrainData.SetHeights(0, 0, heights);

        // Paint terrain textures using Unity's actual alphamap resolution.
        int alphaResolution = terrainData.alphamapResolution;
        float[,,] alpha = new float[alphaResolution, alphaResolution, 3];

        for (int z = 0; z < alphaResolution; z++)
        {
            for (int x = 0; x < alphaResolution; x++)
            {
                float wx = terrainOrigin.x + x / (float)(alphaResolution - 1) * worldSize;
                float wz = terrainOrigin.z + z / (float)(alphaResolution - 1) * worldSize;

                float normalizedHeight = terrainData.GetInterpolatedHeight(
                    x / (float)(alphaResolution - 1),
                    z / (float)(alphaResolution - 1)
                ) / verticalSize;

                float noise = Mathf.PerlinNoise(wx * 0.025f, wz * 0.025f);
                float rock = Mathf.Clamp01((noise - 0.70f) * 2.2f + Mathf.Max(0f, normalizedHeight - 0.55f));
                float dirt = 0.10f + (1f - rock) * 0.13f;
                float grass = Mathf.Max(0.01f, 1f - dirt - rock);
                float total = grass + dirt + rock;

                alpha[z, x, 0] = grass / total;
                alpha[z, x, 1] = dirt / total;
                alpha[z, x, 2] = rock / total;
            }
        }

        terrainData.SetAlphamaps(0, 0, alpha);

        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "Park Terrain";
        terrainObject.transform.SetParent(root, true);
        terrainObject.transform.position = terrainOrigin;

        terrain = terrainObject.GetComponent<Terrain>();
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 5;
        terrain.basemapDistance = 1200f;

        Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (terrainShader != null)
            terrain.materialTemplate = new Material(terrainShader);
        else
            Debug.LogWarning("URP Terrain/Lit shader not found. Unity will use its default terrain material.");

        terrain.Flush();
        Debug.Log("Terrain generated | Heightmap: " + heightResolution + " | Alphamap: " + alphaResolution + " | Layers: " + terrainData.terrainLayers.Length);
    }

    private float GroundHeight(float x, float z)
    {
        // A global ceiling guarantees that terrain can never rise into any
        // section of the coaster, including vertically overlapping track runs.
        float safeCeiling = routeBounds.min.y - 8f;
        float safeBase = safeCeiling - 7f;

        // Gentle rolling terrain: low enough to preserve clearance, but not flat.
        float broad = Mathf.PerlinNoise((x + 500f) * 0.003f, (z + 200f) * 0.003f);
        float medium = Mathf.PerlinNoise((x - 100f) * 0.009f, (z + 350f) * 0.009f);
        float detail = Mathf.PerlinNoise(x * 0.025f, z * 0.025f);

        float hills = safeBase + broad * 3.2f + medium * 2.2f + detail * 0.8f;
        float height = Mathf.Min(hills, safeCeiling);

        // Keep the lake basin lower than the surrounding ground.
        if (createLake)
        {
            float lakeDistance = Vector2.Distance(
                new Vector2(x, z),
                new Vector2(lakePosition.x, lakePosition.z)
            );

            float basin = 1f - Mathf.Clamp01(lakeDistance / 110f);
            float lakeBed = safeBase - 1.5f;
            height = Mathf.Lerp(height, lakeBed, basin * basin);
        }

        // Final hard limit: never allow terrain to exceed the safe ceiling.
        return Mathf.Min(height, safeCeiling);
    }

    private float GetGroundY(float x, float z)
    {
        if (terrain == null)
            return GroundHeight(x, z);

        return terrain.SampleHeight(
            new Vector3(x, 0f, z)
        ) + terrain.transform.position.y;
    }

    // =========================================================
    // LAKE
    // =========================================================

    private void SelectLakePosition()
    {
        Vector3 c = routeBounds.center;

        float dx = routeBounds.extents.x + 100f;
        float dz = routeBounds.extents.z + 100f;

        Vector3[] candidates =
        {
            c + new Vector3(dx, 0, dz),
            c + new Vector3(-dx, 0, dz),
            c + new Vector3(dx, 0, -dz),
            c + new Vector3(-dx, 0, -dz),
            c + new Vector3(dx, 0, 0),
            c + new Vector3(-dx, 0, 0),
            c + new Vector3(0, 0, dz),
            c + new Vector3(0, 0, -dz)
        };

        float best = -1f;
        lakePosition = candidates[0];

        foreach (Vector3 candidate in candidates)
        {
            if (!InsideTerrain(candidate.x, candidate.z, 60f))
                continue;

            float d = RouteDistance(candidate.x, candidate.z);

            if (d > best)
            {
                best = d;
                lakePosition = candidate;
            }
        }
    }

    private void CreateLake()
    {
        if (!createLake)
            return;

        GameObject water =
            GameObject.CreatePrimitive(PrimitiveType.Plane);

        water.name = "Park Lake";
        water.transform.SetParent(root, true);

        lakeLevel = GetGroundY(
            lakePosition.x,
            lakePosition.z
        ) + 0.8f;

        water.transform.position = new Vector3(
            lakePosition.x,
            lakeLevel,
            lakePosition.z
        );

        water.transform.localScale = new Vector3(
            lakeSize.x / 10f,
            1f,
            lakeSize.y / 10f
        );

        water.GetComponent<Renderer>().sharedMaterial = waterMaterial;
        RemoveCollider(water);
    }

    // =========================================================
    // ELEVATED ROLLER COASTER SUPPORTS
    // =========================================================

    
private void CreateSupports()
{
    float spacing = Mathf.Max(10f, supportSpacing);
    float halfWidth = Mathf.Clamp(supportWidth, 4.5f, 6f) * 0.5f;
    float columnThickness = Mathf.Clamp(supportThickness, 0.28f, 0.42f);
    float braceThickness = Mathf.Clamp(crossBraceThickness, 0.16f, 0.23f);

    int count = Mathf.CeilToInt(track.TrackLength / spacing);

    for (int i = 0; i < count; i++)
    {
        float distance = Mathf.Min(
            i * spacing,
            track.TrackLength - 0.5f
        );

        if (!track.GetTrackPose(
            distance,
            out Vector3 position,
            out Quaternion rotation))
            continue;

        Vector3 right = rotation * Vector3.right;

        Vector3 leftTop = position - right * halfWidth;
        Vector3 rightTop = position + right * halfWidth;

        float leftGround = GetGroundY(leftTop.x, leftTop.z);
        float rightGround = GetGroundY(rightTop.x, rightTop.z);

        Vector3 leftBottom = new Vector3(
            leftTop.x, leftGround, leftTop.z
        );

        Vector3 rightBottom = new Vector3(
            rightTop.x, rightGround, rightTop.z
        );

        // Two slim steel columns.
        CreateBeam(
            "Steel Support Left",
            leftBottom,
            leftTop,
            columnThickness,
            supportMaterial
        );

        CreateBeam(
            "Steel Support Right",
            rightBottom,
            rightTop,
            columnThickness,
            supportMaterial
        );

        // Crossbeam directly beneath the track.
        Vector3 leftCap = leftTop - Vector3.up * 1.2f;
        Vector3 rightCap = rightTop - Vector3.up * 1.2f;

        CreateBeam(
            "Steel Support Crossbeam",
            leftCap,
            rightCap,
            columnThickness,
            supportMaterial
        );

        // Short knee braces, not full-height X structures.
        Vector3 leftKnee = Vector3.Lerp(
            leftBottom, leftTop, 0.72f
        );

        Vector3 rightKnee = Vector3.Lerp(
            rightBottom, rightTop, 0.72f
        );

        CreateBeam(
            "Left Knee Brace",
            leftKnee,
            leftCap,
            braceThickness,
            supportMaterial
        );

        CreateBeam(
            "Right Knee Brace",
            rightKnee,
            rightCap,
            braceThickness,
            supportMaterial
        );

        CreateFooting(leftBottom);
        CreateFooting(rightBottom);
    }

    Debug.Log("Slim elevated coaster supports generated: " + count);
}


    private void CreateFooting(Vector3 position)
    {
        GameObject footing =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        footing.name = "Concrete Support Footing";
        footing.transform.SetParent(root, true);
        footing.transform.position = position + Vector3.up * 0.35f;
        footing.transform.localScale = new Vector3(1.5f, 0.7f, 1.5f);
        footing.GetComponent<Renderer>().sharedMaterial = lightMaterial;
        RemoveCollider(footing);
    }

    // =========================================================
    // SCENERY
    // =========================================================

    
private void CreateScenery()
{
    Scatter(trees, treeCount, "Trees", 1.5f, 2.3f, 9f, true);

    Scatter(rocks, rockCount, "Rocks", 0.8f, 1.5f, 12f, true);

    Scatter(plants, plantCount, "Grass", 0.8f, 1.35f, 4f, false);

    Scatter(flowers, flowerCount, "Flowers", 0.8f, 1.4f, 7f, false);

    CreateSimpleBushes();
    CreateNearTrackLandscaping();
}


private void CreateNearTrackLandscaping()
{
    int placed = 0;
    int attempts = 0;

    while (placed < 180 && attempts < 1800)
    {
        attempts++;

        float distance = Random.Range(32f, 65f);
        float angle = Random.Range(0f, Mathf.PI * 2f);

        Vector3 center = routeBounds.center;

        float x = center.x + Mathf.Cos(angle) * distance;
        float z = center.z + Mathf.Sin(angle) * distance;

        if (!InsideTerrain(x, z, 15f))
            continue;

        if (RouteDistance(x, z) < trackClearance + 5f)
            continue;

        if (createLake &&
            Vector2.Distance(
                new Vector2(x, z),
                new Vector2(lakePosition.x, lakePosition.z)
            ) < 40f)
            continue;

        if (trees == null || trees.Length == 0)
            break;

        GameObject prefab = trees[
            Random.Range(0, trees.Length)
        ];

        GameObject obj = SpawnPrefab(
            prefab,
            "Ride Surrounding Tree",
            new Vector3(x, GetGroundY(x, z), z),
            Random.Range(1.2f, 1.8f)
        );

        if (obj != null)
            placed++;
    }

    Debug.Log("Additional ride landscaping: " + placed);
}


    private void Scatter(
        GameObject[] prefabs,
        int count,
        string label,
        float minScale,
        float maxScale,
        float spacing,
        bool avoidTrack)
    {
        if (prefabs == null || prefabs.Length == 0)
            return;

        List<Vector2> positions = new List<Vector2>();

        float minX = terrainOrigin.x + 12f;
        float minZ = terrainOrigin.z + 12f;
        float maxX = terrainOrigin.x + worldSize - 12f;
        float maxZ = terrainOrigin.z + worldSize - 12f;

        int attempts = Mathf.Max(count * 15, 3000);

        for (int i = 0; i < attempts && positions.Count < count; i++)
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);

            if (avoidTrack && RouteDistance(x, z) < trackClearance)
                continue;

            if (createLake &&
                Vector2.Distance(
                    new Vector2(x, z),
                    new Vector2(lakePosition.x, lakePosition.z)
                ) < Mathf.Max(lakeSize.x, lakeSize.y) * 0.65f)
                continue;

            float density = Mathf.PerlinNoise(
                x * 0.012f + randomSeed,
                z * 0.012f - randomSeed
            );

            if (label == "Trees" && density < 0.32f)
                continue;

            bool close = false;

            foreach (Vector2 p in positions)
            {
                if ((p - new Vector2(x, z)).sqrMagnitude <
                    spacing * spacing)
                {
                    close = true;
                    break;
                }
            }

            if (!close)
                positions.Add(new Vector2(x, z));
        }

        int placed = 0;

        foreach (Vector2 p in positions)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];

            GameObject obj = SpawnPrefab(
                prefab,
                label,
                new Vector3(p.x, GetGroundY(p.x, p.y), p.y),
                Random.Range(minScale, maxScale)
            );

            if (obj != null)
                placed++;
        }

        Debug.Log(label + " placed: " + placed + "/" + count);
    }

    private GameObject SpawnPrefab(
        GameObject prefab,
        string label,
        Vector3 position,
        float scale)
    {
        if (prefab == null)
            return null;

        GameObject obj;

#if UNITY_EDITOR
        obj = PrefabUtility.InstantiatePrefab(prefab, root) as GameObject;

        if (obj == null)
            obj = Instantiate(prefab, root);
#else
        obj = Instantiate(prefab, root);
#endif

        obj.name = label + " - " + prefab.name;
        obj.transform.position = position;
        obj.transform.localScale = prefab.transform.localScale * scale;
        obj.transform.rotation = Quaternion.Euler(
            0f,
            Random.Range(0f, 360f),
            0f
        );

        RemoveChildColliders(obj);
        return obj;
    }

    private void CreateSimpleBushes()
    {
        for (int i = 0; i < 130; i++)
        {
            float x = Random.Range(
                terrainOrigin.x + 15f,
                terrainOrigin.x + worldSize - 15f
            );

            float z = Random.Range(
                terrainOrigin.z + 15f,
                terrainOrigin.z + worldSize - 15f
            );

            if (RouteDistance(x, z) < trackClearance + 8f)
                continue;

            GameObject bush =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);

            bush.name = "Park Bush";
            bush.transform.SetParent(root, true);
            bush.transform.position = new Vector3(
                x,
                GetGroundY(x, z) + 0.55f,
                z
            );

            float s = Random.Range(1.1f, 2.2f);
            bush.transform.localScale = new Vector3(s, s * 0.65f, s);

            bush.GetComponent<Renderer>().sharedMaterial =
                NewMaterial(new Color(0.13f, 0.32f, 0.10f));

            RemoveCollider(bush);
        }
    }

    // =========================================================
    // MOUNTAINS
    // =========================================================

    private void CreateMountains()
    {
        if (mountains == null || mountains.Length == 0)
            return;

        Vector3 center = routeBounds.center;
        int placed = 0;
        int attempts = 0;

        while (placed < mountainCount &&
               attempts < mountainCount * 30)
        {
            attempts++;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(
                mountainDistance,
                mountainDistance + 100f
            );

            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;

            if (!InsideTerrain(x, z, 35f))
                continue;

            if (RouteDistance(x, z) < 100f)
                continue;

            GameObject prefab = mountains[
                Random.Range(0, mountains.Length)
            ];

            GameObject obj = SpawnPrefab(
                prefab,
                "Distant Mountain",
                new Vector3(x, GetGroundY(x, z), z),
                Random.Range(2f, 3.3f)
            );

            if (obj != null)
                placed++;
        }

        Debug.Log("Mountains placed: " + placed);
    }

    // =========================================================
    // PATHS
    // =========================================================

    private void CreateParkPaths()
    {
        Vector3 c = routeBounds.center;

        Vector3[] points =
        {
            new Vector3(c.x - 175f, 0f, c.z - 145f),
            new Vector3(c.x - 100f, 0f, c.z - 155f),
            new Vector3(c.x, 0f, c.z - 155f),
            new Vector3(c.x + 100f, 0f, c.z - 140f),
            new Vector3(c.x + 160f, 0f, c.z - 65f),
            new Vector3(c.x + 145f, 0f, c.z + 70f),
            new Vector3(c.x + 60f, 0f, c.z + 150f),
            new Vector3(c.x - 80f, 0f, c.z + 155f),
            new Vector3(c.x - 160f, 0f, c.z + 75f)
        };

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];

            if (RouteDistance((a.x + b.x) * 0.5f,
                (a.z + b.z) * 0.5f) < trackClearance + 15f)
                continue;

            a.y = GetGroundY(a.x, a.z) + 0.15f;
            b.y = GetGroundY(b.x, b.z) + 0.15f;

            CreateBeam(
                "Park Walking Path",
                a,
                b,
                5f,
                pathMaterial,
                true
            );
        }
    }

    // =========================================================
    // PARK FURNITURE AND PLAZA
    // =========================================================

    private void CreateParkDetails()
    {
        Vector3 c = routeBounds.center;

        // Entrance plaza: a broad, visible paved gathering area.
        Vector3 plaza = new Vector3(
            c.x,
            GetGroundY(c.x, c.z - routeBounds.extents.z - 45f) + 0.12f,
            c.z - routeBounds.extents.z - 45f
        );
        CreateCylinder("Entrance Plaza", plaza, 15f, 0.24f, pathMaterial);

        // Benches and lamps arranged along the public park perimeter.
        Vector2[] furniturePoints =
        {
            new Vector2(c.x - 120f, c.z - 150f),
            new Vector2(c.x - 35f, c.z - 165f),
            new Vector2(c.x + 75f, c.z - 145f),
            new Vector2(c.x + 155f, c.z - 65f),
            new Vector2(c.x + 145f, c.z + 85f),
            new Vector2(c.x + 35f, c.z + 155f),
            new Vector2(c.x - 95f, c.z + 145f),
            new Vector2(c.x - 160f, c.z + 55f)
        };

        for (int i = 0; i < furniturePoints.Length; i++)
        {
            float x = furniturePoints[i].x;
            float z = furniturePoints[i].y;
            if (!InsideTerrain(x, z, 8f) || RouteDistance(x, z) < trackClearance + 18f)
                continue;

            float y = GetGroundY(x, z);
            Vector3 basePos = new Vector3(x, y, z);
            CreateLampPost(basePos);
            CreateBench(basePos + new Vector3(4f, 0f, 2f));
        }

        // Small landscaped islands to make the paths feel designed.
        for (int i = 0; i < 10; i++)
        {
            float angle = i * Mathf.PI * 2f / 10f;
            float x = c.x + Mathf.Cos(angle) * 105f;
            float z = c.z + Mathf.Sin(angle) * 105f;
            if (!InsideTerrain(x, z, 12f) || RouteDistance(x, z) < trackClearance + 22f)
                continue;

            CreateCylinder("Landscaped Flower Island",
                new Vector3(x, GetGroundY(x, z) + 0.12f, z),
                3.5f, 0.24f, lightMaterial);
        }
    }

    private void CreateLampPost(Vector3 ground)
    {
        Material pole = supportMaterial;
        Material glow = lightMaterial;
        CreateCylinder("Park Lamp Pole", ground + Vector3.up * 3f, 0.12f, 6f, pole);
        CreateBox("Park Lamp Arm", ground + new Vector3(0.45f, 5.7f, 0f),
            new Vector3(1f, 0.12f, 0.12f), pole);
        CreateCylinder("Park Lamp Light", ground + new Vector3(0.85f, 5.55f, 0f),
            0.32f, 0.22f, glow);
    }

    private void CreateBench(Vector3 ground)
    {
        CreateBox("Park Bench Seat", ground + new Vector3(0f, 0.65f, 0f),
            new Vector3(2.8f, 0.18f, 0.85f), lightMaterial);
        CreateBox("Park Bench Back", ground + new Vector3(0f, 1.15f, 0.36f),
            new Vector3(2.8f, 0.9f, 0.16f), fenceMaterial);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateBox("Park Bench Leg", ground + new Vector3(side * 1.05f, 0.32f, 0f),
                new Vector3(0.16f, 0.65f, 0.65f), supportMaterial);
        }
    }

    // =========================================================
    // FENCES
    // =========================================================

    private void CreateParkFences()
    {
        Vector3 c = routeBounds.center;

        float radiusX = routeBounds.extents.x + 35f;
        float radiusZ = routeBounds.extents.z + 35f;

        int segments = 100;

        Vector3 previous = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;

            Vector3 p = new Vector3(
                c.x + Mathf.Cos(angle) * radiusX,
                0f,
                c.z + Mathf.Sin(angle) * radiusZ
            );

            p.y = GetGroundY(p.x, p.z);

            if (i > 0)
            {
                CreateBeam(
                    "Park Boundary Fence",
                    previous + Vector3.up * 1.2f,
                    p + Vector3.up * 1.2f,
                    0.22f,
                    fenceMaterial
                );
            }

            previous = p;
        }
    }

    // =========================================================
    // ENTRANCE
    // =========================================================

    private void CreateEntrance()
    {
        Vector3 c = routeBounds.center;

        Vector3 position = new Vector3(
            c.x,
            0f,
            c.z - routeBounds.extents.z - 65f
        );

        position.y = GetGroundY(position.x, position.z);

        CreateBox(
            "Entrance Left Pillar",
            position + new Vector3(-8f, 4f, 0f),
            new Vector3(2f, 8f, 2f),
            darkMaterial
        );

        CreateBox(
            "Entrance Right Pillar",
            position + new Vector3(8f, 4f, 0f),
            new Vector3(2f, 8f, 2f),
            darkMaterial
        );

        CreateBox(
            "Amusement Park Entrance Sign",
            position + new Vector3(0f, 8f, 0f),
            new Vector3(18f, 2.5f, 2.5f),
            lightMaterial
        );

        CreateBox(
            "Entrance Roof",
            position + new Vector3(0f, 10f, 0f),
            new Vector3(21f, 0.8f, 4f),
            supportMaterial
        );

        GameObject signText = new GameObject("Entrance Sign Text");
        signText.transform.SetParent(root, true);
        signText.transform.position = position + new Vector3(0f, 8f, -1.35f);
        signText.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        TextMesh textMesh = signText.AddComponent<TextMesh>();
        textMesh.text = "THRILL PARK";
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 72;
        textMesh.characterSize = 0.12f;
        textMesh.color = new Color(0.12f, 0.16f, 0.20f);
        textMesh.fontStyle = FontStyle.Bold;
    }

    // =========================================================
    // ATTRACTIONS
    // =========================================================

    private void CreateAttractions()
    {
        Vector3 c = routeBounds.center;

        CreateFerrisWheel(
            new Vector3(
                c.x + routeBounds.extents.x + 55f,
                0f,
                c.z + 45f
            )
        );

        CreateCarousel(
            new Vector3(
                c.x - routeBounds.extents.x - 45f,
                0f,
                c.z + 15f
            )
        );

        CreateFoodStall(
            new Vector3(
                c.x + 30f,
                0f,
                c.z - routeBounds.extents.z - 45f
            )
        );
    }

    private void CreateFerrisWheel(Vector3 position)
    {
        position.y = GetGroundY(position.x, position.z);

        float radius = 19f;
        Vector3 center = position + Vector3.up * 24f;

        // Main support legs.
        CreateBeam(
            "Ferris Wheel Support",
            position + new Vector3(-5f, 0f, 0f),
            center,
            0.8f,
            supportMaterial
        );

        CreateBeam(
            "Ferris Wheel Support",
            position + new Vector3(5f, 0f, 0f),
            center,
            0.8f,
            supportMaterial
        );

        // Outer wheel represented by connected segments.
        int segments = 24;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * Mathf.PI * 2f / segments;
            float a2 = (i + 1) * Mathf.PI * 2f / segments;

            Vector3 p1 = center + new Vector3(
                Mathf.Cos(a1) * radius,
                Mathf.Sin(a1) * radius,
                0f
            );

            Vector3 p2 = center + new Vector3(
                Mathf.Cos(a2) * radius,
                Mathf.Sin(a2) * radius,
                0f
            );

            CreateBeam(
                "Ferris Wheel Rim",
                p1,
                p2,
                0.52f,
                lightMaterial
            );

            Vector3 cabin = center + new Vector3(
                Mathf.Cos(a1) * radius,
                Mathf.Sin(a1) * radius,
                0f
            );

            CreateBox(
                "Ferris Wheel Cabin",
                cabin + Vector3.down * 1.2f,
                new Vector3(2.0f, 2.2f, 2.0f),
                NewMaterial(Color.HSVToRGB(i / (float)segments, 0.8f, 0.95f))
            );

            CreateBeam(
                "Ferris Wheel Spoke",
                center,
                p1,
                0.22f,
                supportMaterial
            );
        }

        CreateCylinder(
            "Ferris Wheel Axle",
            center,
            1.6f,
            3.5f,
            lightMaterial
        );
    }

    private void CreateCarousel(Vector3 position)
    {
        position.y = GetGroundY(position.x, position.z);

        CreateCylinder(
            "Carousel Platform",
            position + Vector3.up * 0.6f,
            8f,
            1.2f,
            lightMaterial
        );

        CreateCylinder(
            "Carousel Roof",
            position + Vector3.up * 9f,
            7.5f,
            0.8f,
            NewMaterial(new Color(0.85f, 0.25f, 0.18f))
        );

        CreateCylinder(
            "Carousel Central Pole",
            position + Vector3.up * 5f,
            0.45f,
            9f,
            lightMaterial
        );

        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;

            Vector3 horsePosition = position + new Vector3(
                Mathf.Cos(a) * 5f,
                3.2f,
                Mathf.Sin(a) * 5f
            );

            CreateCylinder(
                "Carousel Decorative Pole",
                horsePosition,
                0.16f,
                5f,
                supportMaterial
            );
        }
    }

    private void CreateFoodStall(Vector3 position)
    {
        position.y = GetGroundY(position.x, position.z);

        CreateBox(
            "Food Stall Building",
            position + Vector3.up * 2f,
            new Vector3(9f, 4.5f, 6f),
            NewMaterial(new Color(0.91f, 0.72f, 0.42f))
        );

        CreateBox(
            "Food Stall Roof",
            position + Vector3.up * 4.4f,
            new Vector3(10f, 0.9f, 7f),
            NewMaterial(new Color(0.75f, 0.16f, 0.12f))
        );

        CreateBox(
            "Food Stall Counter",
            position + new Vector3(0f, 1f, -2.6f),
            new Vector3(6f, 1.5f, 0.5f),
            lightMaterial
        );
    }

    // =========================================================
    // PRIMITIVE HELPERS
    // =========================================================

    private void CreateBeam(
        string name,
        Vector3 start,
        Vector3 end,
        float thickness,
        Material material,
        bool flat = false)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        if (length < 0.02f)
            return;

        GameObject beam =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        beam.name = name;
        beam.transform.SetParent(root, true);
        beam.transform.position = (start + end) * 0.5f;

        beam.transform.rotation = Quaternion.FromToRotation(
            Vector3.up,
            direction.normalized
        );

        beam.transform.localScale = flat
            ? new Vector3(thickness, 0.18f, 1f)
            : new Vector3(thickness, length, thickness);

        if (flat)
            beam.transform.localScale = new Vector3(thickness, 0.18f, length);

        beam.GetComponent<Renderer>().sharedMaterial = material;
        RemoveCollider(beam);
    }

    private void CreateBox(
        string name,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        GameObject obj =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        obj.name = name;
        obj.transform.SetParent(root, true);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        RemoveCollider(obj);
    }

    private void CreateCylinder(
        string name,
        Vector3 position,
        float radius,
        float height,
        Material material)
    {
        GameObject obj =
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        obj.name = name;
        obj.transform.SetParent(root, true);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(
            radius * 2f,
            height * 0.5f,
            radius * 2f
        );

        obj.GetComponent<Renderer>().sharedMaterial = material;
        RemoveCollider(obj);
    }

    private void RemoveCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();

        if (col != null)
            DestroyImmediate(col);
    }

    private void RemoveChildColliders(GameObject obj)
    {
        foreach (Collider col in obj.GetComponentsInChildren<Collider>(true))
            DestroyImmediate(col);
    }

    private bool InsideTerrain(float x, float z, float margin)
    {
        return
            x > terrainOrigin.x + margin &&
            x < terrainOrigin.x + worldSize - margin &&
            z > terrainOrigin.z + margin &&
            z < terrainOrigin.z + worldSize - margin;
    }
}
