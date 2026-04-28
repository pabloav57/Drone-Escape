using UnityEngine;

public class EnvironmentStyler : MonoBehaviour
{
    public Color skyColor = new Color(0.45f, 0.58f, 0.72f, 1f);
    public Color fogColor = new Color(0.56f, 0.62f, 0.64f, 1f);
    public float fogDensity = 0.008f;
    public Color groundBaseColor = new Color(0.33f, 0.42f, 0.35f, 1f);
    public Color groundStripeColor = new Color(0.42f, 0.48f, 0.39f, 1f);
    public Color sunlightColor = new Color(1f, 0.89f, 0.72f, 1f);
    public float sunlightIntensity = 1.45f;
    public float groundScale = 90f;
    public float groundFollowStep = 180f;
    public int scorePerBiome = 80;

    private Material groundMaterial;
    private Transform groundTransform;
    private Transform followTarget;
    private Vector3 groundBasePosition;
    private GameController gameController;
    private int currentBiomeIndex = -1;

    private readonly GroundBiome[] biomes =
    {
        new GroundBiome("Verde", new Color(0.33f, 0.42f, 0.35f, 1f), new Color(0.42f, 0.48f, 0.39f, 1f), new Color(0.45f, 0.58f, 0.72f, 1f), new Color(0.56f, 0.62f, 0.64f, 1f), false),
        new GroundBiome("Carretera", new Color(0.11f, 0.13f, 0.14f, 1f), new Color(0.62f, 0.58f, 0.42f, 1f), new Color(0.35f, 0.45f, 0.55f, 1f), new Color(0.38f, 0.43f, 0.45f, 1f), true),
        new GroundBiome("Desierto", new Color(0.56f, 0.46f, 0.30f, 1f), new Color(0.72f, 0.62f, 0.42f, 1f), new Color(0.58f, 0.56f, 0.48f, 1f), new Color(0.64f, 0.58f, 0.46f, 1f), false),
        new GroundBiome("Nocturno", new Color(0.08f, 0.13f, 0.17f, 1f), new Color(0.16f, 0.21f, 0.24f, 1f), new Color(0.16f, 0.22f, 0.31f, 1f), new Color(0.18f, 0.22f, 0.27f, 1f), true)
    };

    void Start()
    {
        ApplyAtmosphere();
        StyleCamera();
        StyleSunlight();
        StyleGround();
        gameController = FindAnyObjectByType<GameController>();
        ApplyBiome(0);
    }

    void LateUpdate()
    {
        KeepGroundUnderPlayer();
        UpdateBiomeProgress();
    }

    private void ApplyAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.44f, 0.52f, 0.62f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.31f, 0.35f, 0.38f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.2f, 0.18f, 1f);
        RenderSettings.ambientIntensity = 0.85f;
    }

    private void StyleCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = skyColor;
    }

    private void StyleSunlight()
    {
        Light sun = FindAnyObjectByType<Light>();
        if (sun == null)
        {
            return;
        }

        sun.color = sunlightColor;
        sun.intensity = sunlightIntensity;
        sun.transform.rotation = Quaternion.Euler(34f, -35f, 0f);
    }

    private void StyleGround()
    {
        GameObject terrainObject = GameObject.Find("Terrain");
        if (terrainObject == null)
        {
            return;
        }

        Renderer terrainRenderer = terrainObject.GetComponent<Renderer>();
        if (terrainRenderer == null)
        {
            return;
        }

        groundTransform = terrainObject.transform;
        groundBasePosition = groundTransform.position;
        groundTransform.localScale = new Vector3(groundScale, 1f, groundScale);
        ResolveFollowTarget();

        groundMaterial = new Material(FindLitShader());
        groundMaterial.name = "Runtime_Ground_Runway";
        groundMaterial.color = groundBaseColor;
        groundMaterial.mainTexture = CreateGroundTexture(groundBaseColor, groundStripeColor, false);
        groundMaterial.mainTextureScale = new Vector2(groundScale * 0.55f, groundScale * 0.55f);
        terrainRenderer.material = groundMaterial;
    }

    private void UpdateBiomeProgress()
    {
        if (gameController == null)
        {
            gameController = FindAnyObjectByType<GameController>();
            if (gameController == null)
            {
                return;
            }
        }

        int biomeIndex = Mathf.Abs(gameController.CurrentScore / Mathf.Max(1, scorePerBiome)) % biomes.Length;
        if (biomeIndex != currentBiomeIndex)
        {
            ApplyBiome(biomeIndex);
        }
    }

    private void ApplyBiome(int biomeIndex)
    {
        if (biomes == null || biomes.Length == 0)
        {
            return;
        }

        GroundBiome biome = biomes[Mathf.Clamp(biomeIndex, 0, biomes.Length - 1)];
        currentBiomeIndex = biomeIndex;
        groundBaseColor = biome.baseColor;
        groundStripeColor = biome.detailColor;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = biome.skyColor;
        }

        RenderSettings.fogColor = biome.fogColor;
        RenderSettings.ambientSkyColor = Color.Lerp(biome.skyColor, Color.white, 0.15f);
        RenderSettings.ambientEquatorColor = Color.Lerp(biome.fogColor, Color.black, 0.2f);

        if (groundMaterial != null)
        {
            groundMaterial.color = biome.baseColor;
            groundMaterial.mainTexture = CreateGroundTexture(biome.baseColor, biome.detailColor, biome.roadLines);
        }
    }

    private void KeepGroundUnderPlayer()
    {
        if (groundTransform == null)
        {
            return;
        }

        if (followTarget == null)
        {
            ResolveFollowTarget();
            if (followTarget == null)
            {
                return;
            }
        }

        float snappedX = Mathf.Round(followTarget.position.x / groundFollowStep) * groundFollowStep;
        float snappedZ = Mathf.Round(followTarget.position.z / groundFollowStep) * groundFollowStep;
        groundTransform.position = new Vector3(snappedX, groundBasePosition.y, snappedZ);
    }

    private void ResolveFollowTarget()
    {
        GameObject drone = GameObject.FindGameObjectWithTag("Drone");
        if (drone == null)
        {
            drone = GameObject.Find("Drone Primary");
        }

        followTarget = drone != null ? drone.transform : null;
    }

    private Texture2D CreateGroundTexture(Color baseColor, Color detailColor, bool roadLines)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.2f;
                float centerDistance = Mathf.Abs(x - (size * 0.5f));
                bool centerLine = roadLines && centerDistance < 1.4f && y % 18 < 10;
                bool sideLine = roadLines && centerDistance > 26f && centerDistance < 28f;
                bool softBand = !roadLines && centerDistance < 8f;
                Color color = Color.Lerp(baseColor, detailColor, noise + (softBand ? 0.08f : 0f));

                if (centerLine || sideLine)
                {
                    color = detailColor;
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private Shader FindLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Standard");
        return shader != null ? shader : Shader.Find("Sprites/Default");
    }

    private struct GroundBiome
    {
        public readonly string name;
        public readonly Color baseColor;
        public readonly Color detailColor;
        public readonly Color skyColor;
        public readonly Color fogColor;
        public readonly bool roadLines;

        public GroundBiome(string name, Color baseColor, Color detailColor, Color skyColor, Color fogColor, bool roadLines)
        {
            this.name = name;
            this.baseColor = baseColor;
            this.detailColor = detailColor;
            this.skyColor = skyColor;
            this.fogColor = fogColor;
            this.roadLines = roadLines;
        }
    }
}
