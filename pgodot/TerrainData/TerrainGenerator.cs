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
            _gradientDirty = true;
            _baseGridDirty = true; // Forzar regeneración de malla base
            UpdateMesh();
        }
    }

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

    [ExportGroup("Terrain Settings")]
    [Export(PropertyHint.Range, "0.1,10,0.1")]
    public float Flatness { get; set; } = 1.0f;
    [Export(PropertyHint.Range, "1,1000,1")]
    public int Size { get; set; } = 32;
    [Export(PropertyHint.Range, "1,1000,1")]
    public int Height { get; set; } = 32;
    [Export(PropertyHint.Range, "1,256,1")]
    public int Resolution { get; set; } = 4;

    private Curve _heightCurve;
    [Export]
    public Curve HeightCurve
    {
        get => _heightCurve;
        set
        {
            if (_heightCurve == value) return;
            _heightCurve = value;
            if (Engine.IsEditorHint())
                UpdateMesh();
        }
    }

    private Gradient _gradient;
    [Export]
    public Gradient Gradient
    {
        get => _gradient;
        set
        {
            if (_gradient == value) return;
            _gradient = value;
            _gradientDirty = true;
            if (Engine.IsEditorHint())
                UpdateMesh();
        }
    }

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

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float NoiseMin { get; set; } = 0f;
    [Export(PropertyHint.Range, "0,100,0.1")]
    public float NoiseMax { get; set; } = 100f;
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Smoothness { get; set; } = 0.5f;
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

    [ExportGroup("Visual Settings")]
    [Export] public bool Wireframe { get; set; } = false;
    [Export(PropertyHint.Range, "0.1,10,0.1")]
    public float TextureScale { get; set; } = 1.0f;

    private Vector2 _chunkPosition;
    private Texture2D _gradientTexture;
    private Vector3[] _baseVertices;
    private int[] _indices;
    private Vector2[] _uvs;
    private bool _baseGridDirty = true;
    private ShaderMaterial _cachedMaterial;
    private bool _gradientDirty = true;
    private bool _collisionsCreated = false;

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            if (_noise != null && _noise.IsConnected("changed", Callable.From(UpdateMesh)))
                _noise.Disconnect("changed", Callable.From(UpdateMesh));
        }
    }

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

    private void RegenerateBaseGrid()
    {
        var planeMesh = new PlaneMesh
        {
            Size = new Vector2(Size, Size),
            SubdivideDepth = Resolution,
            SubdivideWidth = Resolution
        };

        var meshArrays = planeMesh.GetMeshArrays();
        _baseVertices = (Vector3[])meshArrays[(int)ArrayMesh.ArrayType.Vertex];
        _indices = (int[])meshArrays[(int)ArrayMesh.ArrayType.Index];
        _uvs = (Vector2[])meshArrays[(int)ArrayMesh.ArrayType.TexUV];
        _baseGridDirty = false;
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
        noiseValue = (noiseValue + 1f) * 0.5f;
        noiseValue = Mathf.Lerp(NoiseMin / 100f, NoiseMax / 100f, noiseValue);
        noiseValue = Mathf.Max(0.1f, noiseValue);
        noiseValue = Mathf.Pow(noiseValue, 1f / Flatness);

        if (HeightCurve != null)
            noiseValue = HeightCurve.Sample(noiseValue);

        return noiseValue * Height;
    }

    public void Initialize(Vector2 position, int size)
    {
        _chunkPosition = position;
        Size = size;
        _baseGridDirty = true;
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
        _gradientDirty = true;
    }

    public void UpdateMesh()
    {
        if (_noise == null) ConfigureNoise();
        if (_baseGridDirty) RegenerateBaseGrid();

        Vector3[] currentVertices = new Vector3[_baseVertices.Length];
        for (int i = 0; i < _baseVertices.Length; i++)
        {
            Vector3 vertex = _baseVertices[i];
            vertex.Y = GetHeight(vertex.X, vertex.Z);
            currentVertices[i] = vertex;
        }

        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        for (int i = 0; i < currentVertices.Length; i++)
        {
            surfaceTool.SetUV(_uvs[i]);
            surfaceTool.AddVertex(currentVertices[i]);
        }

        for (int i = 0; i < _indices.Length; i++)
        {
            surfaceTool.AddIndex(_indices[i]);
        }

        surfaceTool.GenerateNormals();

        if (_cachedMaterial == null)
        {
            var shader = GD.Load<Shader>("res://TerrainData/terrain.gdshader");
            _cachedMaterial = new ShaderMaterial { Shader = shader };
        }

        if (Gradient != null && _gradientDirty)
        {
            // Liberar textura anterior si existe
            if (_gradientTexture != null)
            {
                _gradientTexture.Dispose();
            }
            _gradientTexture = GradientToTexture(Gradient);
            _gradientDirty = false;
        }

        _cachedMaterial.SetShaderParameter("height", Height);
        _cachedMaterial.SetShaderParameter("gradient_tex", _gradientTexture);
        _cachedMaterial.SetShaderParameter("texture_scale", TextureScale);

        surfaceTool.SetMaterial(_cachedMaterial);
        Mesh = surfaceTool.Commit();

        if (Engine.IsEditorHint() || !_collisionsCreated)
        {
            UpdateCollisions();
            _collisionsCreated = true;
            GD.Print("UpdateTerrain called");
        }
    }

    public void UpdateCollisions()
    {
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