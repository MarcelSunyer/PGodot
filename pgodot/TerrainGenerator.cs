using Godot;
using System;

[Tool]
public partial class TerrainGenerator : MeshInstance3D
{
    public int _size = 32;
    public int _height = 32;
    public int _resolution = 1;

    public FastNoiseLite _noise;

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

    public override void _Ready()
    {
        // Activate processing in editor so setters can fire
        if (Engine.IsEditorHint())
            SetProcess(true);

        // Initialize noise if it's not set
        if (_noise == null)
        {
            _noise = new FastNoiseLite();
        }

        // Build initial mesh
        UpdateMesh();
    }

    public override void _Process(double delta)
    {
        // Only needed if you want something per-frame.
        // With setters above, you can leave this empty.
    }

    public float GetHeight(float x, float y)
    {
        return _noise.GetNoise2D(x, y) * _height;
    }

    public Vector3 GetNormal(float x, float y)
    {
        int epsilon = Mathf.Max(1, _size / (_resolution + 1)); // Ensure epsilon is at least 1

        float dx = (GetHeight(x + epsilon, y) - GetHeight(x - epsilon, y)) / (2.0f * epsilon);
        float dy = (GetHeight(x, y + epsilon) - GetHeight(x, y - epsilon)) / (2.0f * epsilon);

        Vector3 normal = new Vector3(-dx, 1.0f, -dy);
        return normal.Normalized();
    }

    public void UpdateMesh()
    {
        var planeMesh = new PlaneMesh
        {
            SubdivideDepth = Resolution,
            SubdivideWidth = Resolution,
            Size = new Vector2(Size, Size)
        };

        // Bake into an ArrayMesh
        var planeArrays = planeMesh.GetMeshArrays();
        var vertexArray = (PackedVector3Array)planeArrays[(int)ArrayMesh.ArrayType.Vertex]; 
        var normalArray = (PackedVector3Array)planeArrays[(int)ArrayMesh.ArrayType.Normal];
        var tangentArray = (PackedFloat32Array)planeArrays[(int)ArrayMesh.ArrayType.Tangent];

        for (int i = 0; i < vertexArray.size(); i++)
        {
            Vector3 vertex = vertexArray[i];
            Vector3 normal = Vector3.Up;
            Vector3 tangent = Vector3.Right;

            if(_noise != null)
            {
                vertex.Y = vertexArray[i];
                normal = GetNormal(vertex.X, vertex.Y);
                tangent = normal.Cross(Vector3.Up);
            }
            vertexArray[i] = vertex;
            normalArray[i] = normal;
            tangentArray[4 * i] = tangent.X;
            tangentArray[4 * i + 1] = tangent.Y;
            tangentArray[4 * i + 2] = tangent.Z;
        }

        var arrayMesh = new ArrayMesh();
        Godot.Collections.Array arrays = new Godot.Collections.Array();
        arrays.Add(vertexArray);
        arrays.Add(normalArray);
        arrays.Add(tangentArray); 
        arrays.Add(planeArrays[(int)ArrayMesh.ArrayType.TexUV]); 

        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        // Assign it
        Mesh = arrayMesh;
    }
}