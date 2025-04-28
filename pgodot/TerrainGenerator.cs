using Godot;
using System;

[Tool]
public partial class TerrainGenerator : MeshInstance3D
{
    private float _size = 32f;
    private int _resolution = 1;

    [Export(PropertyHint.Range, "0,1000,0.1")]
    public float Size
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

    public override void _Ready()
    {
        // Activate processing in editor so setters can fire
        if (Engine.IsEditorHint())
            SetProcess(true);

        // Build initial mesh
        UpdateMesh();
    }

    public override void _Process(double delta)
    {
        // Only needed if you want something per-frame.
        // With setters above, you can leave this empty.
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        var planeMesh = new PlaneMesh
        {
            SubdivideDepth = Resolution,
            SubdivideWidth = Resolution,
            Size = new Vector2(Size, Size)
        };

        // Bake into an ArrayMesh
        var arrays = planeMesh.GetMeshArrays();
        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        // Assign it
        Mesh = arrayMesh;
    }
}
