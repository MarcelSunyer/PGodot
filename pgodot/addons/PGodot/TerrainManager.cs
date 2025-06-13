using Godot;

public partial class TerrainManager : Node
{
    public static TerrainManager Instance { get; private set; }

    public TerrainGenerator CurrentTerrain { get; set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public void UpdateTerrain()
    {
        if (CurrentTerrain != null)
        {
            CurrentTerrain.UpdateMesh();
            GD.Print("Terrain updated via TerrainManager");
        }
        else
        {
            GD.PrintErr("No terrain assigned to TerrainManager");
        }
    }
}