using Godot;
using System;

public partial class UpdateTerrain : Button
{
    private TerrainGenerator _terrainGenerator;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Connect the button's pressed signal to our handler
        this.Pressed += OnButtonPressed;

        // Initialize the terrain generator reference
        FindTerrainGenerator();
    }

    private void FindTerrainGenerator()
    {
        // Try to find the TerrainGenerator in the scene tree
        _terrainGenerator = GetTree().CurrentScene.FindChild("TerrainGenerator") as TerrainGenerator;

        // Alternative search if the above doesn't work:
        if (_terrainGenerator == null)
        {
            foreach (Node node in GetTree().GetNodesInGroup("terrain"))
            {
                if (node is TerrainGenerator terrain)
                {
                    _terrainGenerator = terrain;
                    break;
                }
            }
        }

        if (_terrainGenerator == null)
        {
            GD.PrintErr("TerrainGenerator not found in scene!");
        }
    }

    private void OnButtonPressed()
    {
        if (_terrainGenerator != null)
        {
            _terrainGenerator.UpdateMesh();
            GD.Print("Terrain updated via button press");

            // If you want to force collision updates as well:
            if (Engine.IsEditorHint())
            {
                _terrainGenerator.UpdateCollisions();
            }
        }
        else
        {
            GD.PrintErr("Cannot update terrain - TerrainGenerator reference is null");
            // Try to find it again if we failed before
            FindTerrainGenerator();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // Not used in this implementation
    }
}