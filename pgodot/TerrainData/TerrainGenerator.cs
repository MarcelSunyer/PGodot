using Godot;
using System;

[Tool]
public partial class TerrainGenerator : MeshInstance3D
{
    public static TerrainManager TerrainManager { get; set; }

    [ExportToolButton("Update Parameters")]
    public Callable ClickMeButton => Callable.From(UpdateTerrain);
    private void UpdateTerrain()
    {
        if (Engine.IsEditorHint())
        {
            _gradientDirty = true;
            _baseGridDirty = true;
            UpdateMesh();
        }
    }

    private FastNoiseLite _noise;
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

    private float _noiseFrequency = 10;
    public float NoiseFrequency
    {
        get => _noiseFrequency / 1000;
        set
        {
            float newValue = value * 1000;
            if (Mathf.Abs(_noiseFrequency - newValue) < 0.001f) return;
            _noiseFrequency = newValue;
            ConfigureNoise();
            QueueUpdate();
        }
    }

    private float _flatness = 1.0f;
    public float Flatness
    {
        get => _flatness;
        set
        {
            if (Mathf.Abs(_flatness - value) < 0.001f) return;
            _flatness = value;
            QueueUpdate();
        }
    }

    private int _size = 32;
    public int Size
    {
        get => _size;
        set
        {
            if (_size == value) return;
            _size = value;
            _baseGridDirty = true;
            QueueUpdate();
        }
    }

    private int _height = 32;
    public int Height
    {
        get => _height;
        set
        {
            if (_height == value) return;
            _height = value;
            QueueUpdate();
        }
    }

    private int _resolution = 4;
    public int Resolution
    {
        get => _resolution;
        set
        {
            if (_resolution == value) return;
            _resolution = value;
            _baseGridDirty = true;
            QueueUpdate();
        }
    }

    private Curve _heightCurve;
    public Curve HeightCurve
    {
        get => _heightCurve;
        set
        {
            if (_heightCurve == value) return;
            _heightCurve = value;
            QueueUpdate();
        }
    }

    private Gradient _gradient;
    public Gradient Gradient
    {
        get => _gradient;
        set
        {
            if (_gradient == value) return;
            _gradient = value;
            _gradientDirty = true;
            QueueUpdate();
        }
    }

    private float _noiseMin = 0f;
    public float NoiseMin
    {
        get => _noiseMin;
        set
        {
            if (Mathf.Abs(_noiseMin - value) < 0.001f) return;
            _noiseMin = value;
            QueueUpdate();
        }
    }

    private float _noiseMax = 100f;
    public float NoiseMax
    {
        get => _noiseMax;
        set
        {
            if (Mathf.Abs(_noiseMax - value) < 0.001f) return;
            _noiseMax = value;
            QueueUpdate();
        }
    }

    private float _smoothness = 0.5f;
    public float Smoothness
    {
        get => _smoothness;
        set
        {
            if (Mathf.Abs(_smoothness - value) < 0.001f) return;
            _smoothness = value;
            QueueUpdate();
        }
    }

    private int _octaves = 4;
    public int Octaves
    {
        get => _octaves;
        set
        {
            if (_octaves == value) return;
            _octaves = value;
            ConfigureNoise();
            QueueUpdate();
        }
    }

    private float _persistence = 0.5f;
    public float Persistence
    {
        get => _persistence;
        set
        {
            if (Mathf.Abs(_persistence - value) < 0.001f) return;
            _persistence = value;
            ConfigureNoise();
            QueueUpdate();
        }
    }

    private float _lacunarity = 2.0f;
    public float Lacunarity
    {
        get => _lacunarity;
        set
        {
            if (Mathf.Abs(_lacunarity - value) < 0.001f) return;
            _lacunarity = value;
            ConfigureNoise();
            QueueUpdate();
        }
    }

    private float _noiseOffsetX = 0f;
    public float NoiseOffsetX
    {
        get => _noiseOffsetX;
        set
        {
            if (Mathf.Abs(_noiseOffsetX - value) < 0.001f) return;
            _noiseOffsetX = value;
            QueueUpdate();
        }
    }

    private float _noiseOffsetY = 0f;
    public float NoiseOffsetY
    {
        get => _noiseOffsetY;
        set
        {
            if (Mathf.Abs(_noiseOffsetY - value) < 0.001f) return;
            _noiseOffsetY = value;
            QueueUpdate();
        }
    }

    private bool _wireframe = false;
    public bool Wireframe
    {
        get => _wireframe;
        set
        {
            if (_wireframe == value) return;
            _wireframe = value;
            QueueUpdate();
        }
    }

    private float _textureScale = 1.0f;
    public float TextureScale
    {
        get => _textureScale;
        set
        {
            if (Mathf.Abs(_textureScale - value) < 0.001f) return;
            _textureScale = value;
            QueueUpdate();
        }
    }

    private Vector2 _chunkPosition;
    private Texture2D _gradientTexture;
    private Vector3[] _baseVertices;
    private int[] _indices;
    private Vector2[] _uvs;
    private bool _baseGridDirty = true;
    private ShaderMaterial _cachedMaterial;
    private bool _gradientDirty = true;
    private bool _collisionsCreated = false;

    public override void _Ready()
    {
        if (TerrainManager.Instance != null)
        {
            TerrainManager.Instance.CurrentTerrain = this;
        }
        this.Visible = false;
        UpdateMesh();
    }
    
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
            Godot.Color color = gradient.Sample(t);
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
    
    public void SetGradient(Gradient newGradient)
    {
        if (newGradient != null)
        {
            Gradient = newGradient;
            _gradientDirty = true;
            QueueUpdate();
        }
    }

    private void QueueUpdate()
    {
        if (Engine.IsEditorHint())
        {
            CallDeferred("UpdateMesh");
        }
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