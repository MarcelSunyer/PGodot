using Godot;
using System.Collections.Generic;

public partial class EndlessTerrain : Node3D
{
    [Export(PropertyHint.Range, "1,10,1")]
    public int RenderLayers = 3;

    [Export] public int ChunkSize = 32;
    [Export] public Node3D Player;    
    [Export] public FastNoiseLite NoiseTemplate;
    [Export] public Curve HeightCurveTemplate;
    [Export] public TerrainGenerator TerrainTemplate;



    private Dictionary<Vector2, TerrainGenerator> _terrainChunks = new();
    private List<TerrainGenerator> _lastVisibleChunks = new();
    private Vector2 _playerPosition;

    public override void _Process(double delta)
    {


        _playerPosition = new Vector2(Player.Position.X, Player.Position.Z);
        UpdateVisibleChunks();

    }

    private void UpdateVisibleChunks()
    {
        // Hide previous chunks
        foreach (var chunk in _lastVisibleChunks)
        {
            chunk.Visible = false;
        }
        _lastVisibleChunks.Clear();

        int currentChunkX = Mathf.RoundToInt(_playerPosition.X / ChunkSize);
        int currentChunkY = Mathf.RoundToInt(_playerPosition.Y / ChunkSize);

        // Update chunks around player
        for (int yOffset = -RenderLayers; yOffset <= RenderLayers; yOffset++)
        {
            for (int xOffset = -RenderLayers; xOffset <= RenderLayers; xOffset++)
            { 
                Vector2 chunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);
                Vector2 chunkWorldPos = chunkCoord * ChunkSize;

                if (_terrainChunks.TryGetValue(chunkCoord, out TerrainGenerator chunk))
                {
                    chunk.Visible = IsChunkVisible(chunkWorldPos);
                    if (chunk.Visible) _lastVisibleChunks.Add(chunk);
                }
                else
                {
                    CreateNewChunk(chunkCoord, chunkWorldPos);
                }
            }
        }
    }

    private bool IsChunkVisible(Vector2 chunkPosition)
    {
        Vector2 viewerPos = new Vector2(Player.Position.X, Player.Position.Z);
        float distance = chunkPosition.DistanceTo(viewerPos);
        float maxDist = (RenderLayers + 0.5f) * ChunkSize;
        return distance <= maxDist;

    }

    private void CreateNewChunk(Vector2 coord, Vector2 position)
    {
        var newChunk = new TerrainGenerator();

        // Copiar parámetros desde el template
        newChunk.CopySettingsFrom(TerrainTemplate);

        // Asignar una copia del noise (importante)
        newChunk.Noise = NoiseTemplate.Duplicate() as FastNoiseLite;

        // Inicializar
        newChunk.ConfigureNoise();
        newChunk.Initialize(position, ChunkSize);
        newChunk.Position = new Vector3(position.X, 0, position.Y);
        newChunk.Name = $"TerrainChunk_{coord.X}_{coord.Y}";

        AddChild(newChunk);
        _terrainChunks.Add(coord, newChunk);
        _lastVisibleChunks.Add(newChunk);
    }
}