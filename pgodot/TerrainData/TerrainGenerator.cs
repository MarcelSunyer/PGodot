using Godot;
using System;

[Tool]
public partial class TerrainGenerator : MeshInstance3D
{
    [ExportToolButton("Update Parameters")]
    public Callable ClickMeButton => Callable.From(UpdateTerrain);
    private void UpdateTerrain()
    {
        if (Engine.IsEditorHint())
        {
            UpdateMesh();
        }
    }

    // Original noise properties preserved
    private FastNoiseLite _noise;
    [Export]
    public FastNoiseLite Noise
    {
        get => _noise;
        set
        {
            if (_noise == value) return;

            if (_noise != null && _noise.IsConnected("changed", Callable.From(UpdateMesh)))
                _noise.Disconnect("changed", Callable.From(UpdateMesh));

            _noise = value;
            ConfigureNoise();

            if (_noise != null && !_noise.IsConnected("changed", Callable.From(UpdateMesh)))
                _noise.Changed += UpdateMesh;

            if (Engine.IsEditorHint())
                UpdateMesh();
        }
    }

    // Terrain parameters
    [ExportGroup("Terrain Settings")]
 
    [Export(PropertyHint.Range, "0.1,10,0.1")]
    public float Flatness { get; set; } = 1.0f;
    [Export(PropertyHint.Range, "1,1000,1")]
    public int Size { get; set; } = 32;

    [Export(PropertyHint.Range, "1,1000,1")]
    public int Height { get; set; } = 32;

    [Export(PropertyHint.Range, "1,256,1")]
    public int Resolution { get; set; } = 4;

    [Export] public Curve HeightCurve { get; set; }
    [Export] public Gradient Gradient { get; set; }

    [ExportGroup("Noise Settings")]
    private float _noiseFrequency = 0.05f;
    [Export(PropertyHint.None, "Frequency of the noise")]
    public float NoiseFrequency
    {
        get => _noiseFrequency;
        set
        {
            if (_noiseFrequency == value) return;
            _noiseFrequency = value;
            ConfigureNoise();
            if (Engine.IsEditorHint()) UpdateMesh();
        }
    }

    // Add these new properties for noise range control
    [Export(PropertyHint.Range, "0,100,0.1")]
    public float NoiseMin { get; set; } = 0f;

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float NoiseMax { get; set; } = 100f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Smoothness { get; set; } = 0.5f; // 0 = no smoothing, 1 = full smoothing
    private int _octaves = 4;


    [Export(PropertyHint.None, "Number of octaves")]
    public int Octaves
    {
        get => _octaves;
        set
        {
            if (_octaves == value) return;
            _octaves = value;
            ConfigureNoise();
            if (Engine.IsEditorHint()) UpdateMesh();
        }
    }

    private float _persistence = 0.5f;
    [Export(PropertyHint.None, "Persistence of the noise")]
    public float Persistence
    {
        get => _persistence;
        set
        {
            if (_persistence == value) return;
            _persistence = value;
            ConfigureNoise();
            if (Engine.IsEditorHint()) UpdateMesh();
        }
    }

    private float _lacunarity = 2.0f;
    [Export(PropertyHint.None, "Lacunarity of the noise")]
    public float Lacunarity
    {
        get => _lacunarity;
        set
        {
            if (_lacunarity == value) return;
            _lacunarity = value;
            ConfigureNoise();
            if (Engine.IsEditorHint()) UpdateMesh();
        }
    }

    private float _noiseOffsetX = 0f;
    [Export(PropertyHint.None, "X offset of the noise")]
    public float NoiseOffsetX
    {
        get => _noiseOffsetX;
        set
        {
            if (_noiseOffsetX == value) return;
            _noiseOffsetX = value;
            if (Engine.IsEditorHint()) UpdateMesh();
        }
    }

    private float _noiseOffsetY = 0f;
    [Export(PropertyHint.None, "Y offset of the noise")]
    public float NoiseOffsetY
    {
        get => _noiseOffsetY;
        set
        {
            if (_noiseOffsetY == value) return;
            _noiseOffsetY = value;
            if (Engine.IsEditorHint()) UpdateMesh();
        }
    }

    // Visual settings
    [ExportGroup("Visual Settings")]
    [Export] public bool Wireframe { get; set; } = false;
    [Export(PropertyHint.Range, "0.1,10,0.1")]
    public float TextureScale { get; set; } = 1.0f;

    private Vector2 _chunkPosition;
    private Texture2D _gradientTexture;

    public void ConfigureNoise()
    {
        if (_noise == null)
        {
            _noise = new FastNoiseLite();
            _noise.Changed += UpdateMesh;
        }

        _noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        _noise.Frequency = NoiseFrequency;
        _noise.FractalOctaves = Octaves;
        _noise.FractalGain = Persistence;
        _noise.FractalLacunarity = Lacunarity;
    }

    public override void _Ready()
    {
        this.SetProcess(false);
    }
    private Texture2D GradientToTexture(Gradient gradient)
    {
        if (gradient == null) return null;

        var image = Image.Create(256, 1, false, Image.Format.Rgbaf);

        for (int x = 0; x < 256; x++)
        {
            float t = x / 255f;
            Color color = gradient.Sample(t);
            image.SetPixel(x, 0, color);
        }

        return ImageTexture.CreateFromImage(image);
    }

    private float GetHeight(float x, float z)
    {
        if (_noise == null) return 0f;

        float worldX = x + _chunkPosition.X + NoiseOffsetX;
        float worldZ = z + _chunkPosition.Y + NoiseOffsetY;

        float noiseValue = _noise.GetNoise2D(worldX, worldZ);

        // Remap noise from [-1, 1] to [0, 1] first
        noiseValue = (noiseValue + 1f) * 0.5f;

        // Apply min/max range
        noiseValue = Mathf.Lerp(NoiseMin / 100f, NoiseMax / 100f, noiseValue);

        // Ensure noiseValue is non-negative (terrain starts at 0)
        noiseValue = Mathf.Max(0.1f, noiseValue);

        // Apply flatness (lower values = flatter, higher = more mountainous)
        noiseValue = Mathf.Pow(noiseValue, 1f / Flatness);

        if (HeightCurve != null)
            noiseValue = HeightCurve.Sample(noiseValue);

        return noiseValue * Height;
    }

    private Vector3 GetNormal(float x, float z)
    {
        float epsilon = Mathf.Max(1, Size / (Resolution + 1));
        float dx = (GetHeight(x + epsilon, z) - GetHeight(x - epsilon, z)) / (2.0f * epsilon);
        float dz = (GetHeight(x, z + epsilon) - GetHeight(x, z - epsilon)) / (2.0f * epsilon);

        return new Vector3(-dx, 1.0f, -dz).Normalized();
    }

    public void Initialize(Vector2 position, int size)
    {
        _chunkPosition = position;
        Size = size;
        ConfigureNoise();
        UpdateMesh();
    }
    public void CopySettingsFrom(TerrainGenerator other)
    {
        Flatness = other.Flatness;
        Height = other.Height;
        Resolution = other.Resolution;
        NoiseFrequency = other.NoiseFrequency;
        NoiseMin = other.NoiseMin;
        NoiseMax = other.NoiseMax;
        Wireframe = other.Wireframe;
        TextureScale = other.TextureScale;
        Gradient = other.Gradient;
        HeightCurve = other.HeightCurve;
        Octaves = other.Octaves;
        Persistence = other.Persistence;
        Lacunarity = other.Lacunarity;
        NoiseOffsetX = other.NoiseOffsetX;
        NoiseOffsetY = other.NoiseOffsetY;
    }
    public void UpdateMesh()
    {
        if (_noise == null) ConfigureNoise();

        var planeMesh = new PlaneMesh
        {
            Size = new Vector2(Size, Size),
            SubdivideDepth = Resolution,
            SubdivideWidth = Resolution
        };

        var meshArrays = planeMesh.GetMeshArrays();
        Vector3[] vertices = (Vector3[])meshArrays[(int)ArrayMesh.ArrayType.Vertex];
        Vector3[] normals = new Vector3[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            vertex.Y = GetHeight(vertex.X, vertex.Z);
            normals[i] = GetNormal(vertex.X, vertex.Z);
            vertices[i] = vertex;
        }

        meshArrays[(int)ArrayMesh.ArrayType.Vertex] = vertices;
        meshArrays[(int)ArrayMesh.ArrayType.Normal] = normals;

        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, meshArrays);

        if (Wireframe)
        {
            var wireframeMesh = new ArrayMesh();
            wireframeMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, meshArrays);
            Mesh = wireframeMesh;
        }
        else
        {
            Mesh = arrayMesh;
        }

        // Create material
        var shader = GD.Load<Shader>("res://TerrainData/terrain.gdshader");
        var shaderMaterial = new ShaderMaterial { Shader = shader };

        _gradientTexture = GradientToTexture(Gradient);
        shaderMaterial.SetShaderParameter("height", Height);
        shaderMaterial.SetShaderParameter("gradient_tex", _gradientTexture);
        shaderMaterial.SetShaderParameter("texture_scale", TextureScale);

        MaterialOverride = shaderMaterial;

        // Update collisions
        UpdateCollisions();
    }

    private void UpdateCollisions()
    {
        // Remove old collisions
        foreach (Node child in GetChildren())
        {
            if (child is StaticBody3D || child is CollisionShape3D)
            {
                RemoveChild(child);
                child.QueueFree();
            }
        }

        CreateTrimeshCollision();
    }
}