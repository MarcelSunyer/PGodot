using Godot;
using System;


[Tool]
public partial class TerrainGenerator : MeshInstance3D
{
    public int _size = 32;
    public int _height = 32;
    public int _resolution = 1;

    public FastNoiseLite _noise;

    public CollisionShape3D _collisionShape;
    [Export] public float NoiseFrequency { get; set; } = 0.1f;

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
    [Export] 
    public float Frequency { get; set; } = 0.1f;

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
    [Export]
    public FastNoiseLite Noise
    {
        get => _noise;
        set
        {
            if (_noise == value)
                return;

            if (_noise != null && _noise.IsConnected("changed", Callable.From(UpdateMesh)))
            {
                _noise.Disconnect("changed", Callable.From(UpdateMesh));
            }

            _noise = value;

            if (Engine.IsEditorHint())
            {
                UpdateMesh();
            }

            if (_noise != null && !_noise.IsConnected("changed", Callable.From(UpdateMesh)))
            {
                _noise.Changed += UpdateMesh; 

            }
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

    public float GetHeight(float x, float z)
    {
        if (_noise == null)
            return 0f;

        // Escala las coordenadas y aplica el ruido
        return _noise.GetNoise2D(x * NoiseFrequency, z * NoiseFrequency) * _height;
    }

    public Vector3 GetNormal(float x, float y)
    {
        int epsilon = Mathf.Max(1, _size / (_resolution + 1)); 

        float dx = (GetHeight(x + epsilon, y) - GetHeight(x - epsilon, y)) / (2.0f * epsilon);
        float dy = (GetHeight(x, y + epsilon) - GetHeight(x, y - epsilon)) / (2.0f * epsilon);

        Vector3 normal = new Vector3(-dx, 1.0f, -dy);
        return normal.Normalized();
    }
    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            UpdateMesh(); // Forzar actualización en el editor
    }
    public void UpdateMesh()
    {
        if (_noise == null)
        {
            GD.PrintErr("¡FastNoiseLite no está asignado!");
            return;
        }

        var planeMesh = new PlaneMesh
        {
            Size = new Vector2(_size, _size),
            SubdivideDepth = _resolution,
            SubdivideWidth = _resolution
        };

        var meshArrays = planeMesh.GetMeshArrays();
        Vector3[] vertices = (Vector3[])meshArrays[(int)ArrayMesh.ArrayType.Vertex];

        // Modificar la altura de los vértices
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            vertex.Y = GetHeight(vertex.X, vertex.Z);
            vertices[i] = vertex;
        }

        // Actualizar el array de vértices
        meshArrays[(int)ArrayMesh.ArrayType.Vertex] = vertices;

        // Crear el nuevo mesh
        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, meshArrays);
        Mesh = arrayMesh;

        foreach (Node child in GetChildren())
        {
            if (child is StaticBody3D)
                RemoveChild(child); // Quitar del árbol de nodos

            // También opcionalmente liberar el nodo
            child.QueueFree();
        }

        // Crear nueva colisión
        this.CreateTrimeshCollision();
    }

}
