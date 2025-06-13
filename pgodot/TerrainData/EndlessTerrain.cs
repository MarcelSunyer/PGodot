using Godot;
using System.Collections.Generic;

public partial class EndlessTerrain : Node3D
{
    [Export] public float frequency;
    [Export] public Node3D Player;
    [Export] public int ChunkSize = 32;
    [Export] public int ViewDistance = 450;
    [Export] public FastNoiseLite NoiseTemplate;
    [Export] public Curve HeightCurveTemplate;
    [Export] public TerrainGenerator TerrainTemplate;

    private int _chunksVisibleInViewDst;
    private Dictionary<Vector2, TerrainGenerator> _terrainChunks = new();
    private List<TerrainGenerator> _lastVisibleChunks = new();
    private Vector2 _playerPosition;

    public override void _Ready()
    {
        frequency = frequency / 1000;
        _chunksVisibleInViewDst = Mathf.RoundToInt(ViewDistance / ChunkSize);
    }

    public override void _Process(double delta)
    {
        _playerPosition = new Vector2(Player.Position.X, Player.Position.Z);
        UpdateVisibleChunks();
    }

    public void UpdateAllChunks()
    {
        foreach (var chunk in _terrainChunks.Values)
        {
            if (TerrainTemplate != null)
            {
                CopyAllParametersToChunk(chunk);
                chunk.ConfigureNoise();
            }
            chunk.UpdateMesh();
        }
    }

    private void UpdateVisibleChunks()
    {
        foreach (var chunk in _lastVisibleChunks)
        {
            chunk.Visible = false;
        }
        _lastVisibleChunks.Clear();

        int currentChunkX = Mathf.RoundToInt(_playerPosition.X / ChunkSize);
        int currentChunkY = Mathf.RoundToInt(_playerPosition.Y / ChunkSize);

        for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++)
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
        return distance <= ViewDistance;
    }

    private void CreateNewChunk(Vector2 coord, Vector2 position)
    {
        TerrainGenerator newChunk;
        if (TerrainTemplate != null)
        {
            // Duplicate the template to inherit all properties
            newChunk = TerrainTemplate.Duplicate() as TerrainGenerator;
        }
        else
        {
            newChunk = new TerrainGenerator();
        }

        AddChild(newChunk);

        // Apply noise template if specified
        if (NoiseTemplate != null)
        {
            newChunk.Noise = NoiseTemplate.Duplicate() as FastNoiseLite;
        }

        // Apply settings from template if needed
        if (TerrainTemplate != null)
        {
            CopyAllParametersToChunk(newChunk);
        }

        newChunk.ConfigureNoise();
        newChunk.Initialize(position, ChunkSize);
        newChunk.Position = new Vector3(position.X, 0, position.Y);
        newChunk.Name = $"TerrainChunk_{coord.X}_{coord.Y}";
        newChunk.UpdateCollisions();

        _terrainChunks.Add(coord, newChunk);
        _lastVisibleChunks.Add(newChunk);
    }

    private void CopyAllParametersToChunk(TerrainGenerator chunk)
    {
        if (TerrainTemplate == null) return;

        // Copy terrain parameters
        chunk.Flatness = TerrainTemplate.Flatness;
        chunk.Height = TerrainTemplate.Height;
        chunk.Resolution = TerrainTemplate.Resolution;
        chunk.NoiseFrequency = TerrainTemplate.NoiseFrequency;
        chunk.NoiseMin = TerrainTemplate.NoiseMin;
        chunk.NoiseMax = TerrainTemplate.NoiseMax;
        chunk.Wireframe = TerrainTemplate.Wireframe;
        chunk.TextureScale = TerrainTemplate.TextureScale;
        chunk.Gradient = TerrainTemplate.Gradient;
        chunk.HeightCurve = TerrainTemplate.HeightCurve;
        chunk.Octaves = TerrainTemplate.Octaves;
        chunk.Persistence = TerrainTemplate.Persistence;
        chunk.Lacunarity = TerrainTemplate.Lacunarity;
        chunk.NoiseOffsetX = TerrainTemplate.NoiseOffsetX;
        chunk.NoiseOffsetY = TerrainTemplate.NoiseOffsetY;
        chunk.Smoothness = TerrainTemplate.Smoothness;

        // Copy noise parameters
        if (TerrainTemplate.Noise != null && chunk.Noise != null)
        {
            chunk.Noise.Frequency = TerrainTemplate.Noise.Frequency;
            chunk.Noise.FractalOctaves = TerrainTemplate.Noise.FractalOctaves;
            chunk.Noise.FractalGain = TerrainTemplate.Noise.FractalGain;
            chunk.Noise.FractalLacunarity = TerrainTemplate.Noise.FractalLacunarity;
            chunk.Noise.NoiseType = TerrainTemplate.Noise.NoiseType;
        }
    }
}