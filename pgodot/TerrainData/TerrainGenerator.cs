using Godot;
using System;

[Tool]
public partial class TerrainGenerator : MeshInstance3D
{
    private int _size = 32;
    private int _height = 32;
    private int _resolution = 4;
    private FastNoiseLite _noise;
    private Curve _heightCurve;
    private Vector2 _chunkPosition;

    [Export] public float NoiseFrequency { get; set; } = 0.05f;
    [Export] public int Octaves { get; set; } = 4;
    [Export(PropertyHint.Range, "0,1")] public float Persistence { get; set; } = 0.5f;
    [Export] public float Lacunarity { get; set; } = 2.0f;

    [Export] 
    public Curve HeightCurve
    {
        get => _heightCurve;
        set
        {
            _heightCurve = value;
            if (Engine.IsEditorHint())
                UpdateMesh();
        }
    }

    [Export(PropertyHint.Range, "0,1000,0.1")]
    public int Height
    {
        get => _height;
        set
        {
            _height = value;
            if (Engine.IsEditorHint())
                UpdateMesh();
        }
    }

    [Export(PropertyHint.Range, "0,1000,0.1")]
    public int Size
    {
        get => _size;
        set
        {
            _size = value;
            if (Engine.IsEditorHint())
                UpdateMesh();
        }
    }

    [Export(PropertyHint.Range, "0,256,1")]
    public int Resolution
    {
        get => _resolution;
        set
        {
            _resolution = value;
            if (Engine.IsEditorHint())
                UpdateMesh();
        }
    }

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

            if (Engine.IsEditorHint()) 
                UpdateMesh();
            
            if (_noise != null && !_noise.IsConnected("changed", Callable.From(UpdateMesh)))
                _noise.Changed += UpdateMesh;
        }
    }

    public void ConfigureNoise()
    {
        if (_noise == null) return;
        
        _noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        _noise.FractalOctaves = Octaves;
        _noise.FractalGain = Persistence;
        _noise.FractalLacunarity = Lacunarity;
        _noise.Frequency = NoiseFrequency;
    }

    public void Initialize(Vector2 position, int size)
    {
        _chunkPosition = position;
        Size = size;
        UpdateMesh();
    }

    private float GetHeight(float x, float z)
    {
        if (_noise == null) return 0f;

        float worldX = x + _chunkPosition.X;
        float worldZ = z + _chunkPosition.Y;
        
        float noiseValue = _noise.GetNoise2D(worldX, worldZ);
        noiseValue = (noiseValue + 1) * 0.5f; // Convert to 0-1 range
        
        if (_heightCurve != null)
            noiseValue = _heightCurve.Sample(noiseValue);
        
        return noiseValue * _height;
    }

    private Vector3 GetNormal(float x, float z)
    {
        float epsilon = Mathf.Max(1, _size / (_resolution + 1));
        float dx = (GetHeight(x + epsilon, z) - GetHeight(x - epsilon, z)) / (2.0f * epsilon);
        float dz = (GetHeight(x, z + epsilon) - GetHeight(x, z - epsilon)) / (2.0f * epsilon);

        return new Vector3(-dx, 1.0f, -dz).Normalized();
    }

    public void UpdateMesh()
    {
        if (_noise == null) return;

        var planeMesh = new PlaneMesh {
            Size = new Vector2(_size, _size),
            SubdivideDepth = _resolution,
            SubdivideWidth = _resolution
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
        Mesh = arrayMesh;

        // Clear old collisions
        foreach (Node child in GetChildren())
        {
            if (child is StaticBody3D)
            {
                RemoveChild(child);
                child.QueueFree();
            }
        }

        CreateTrimeshCollision();
    }
}