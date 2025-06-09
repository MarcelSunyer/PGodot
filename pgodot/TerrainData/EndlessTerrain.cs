using Godot;
using System;
using System.Collections.Generic;

public partial class EndlessTerrain : Node3D
{
    [Export] public NodePath ViewerPath;
    [Export] public int ViewDistance = 5;
    [Export] public int ChunkSize = 32;
    [Export] public float UpdateThreshold = 25.0f;

    private Node3D _viewer;
    private Vector2 _viewerPosition;
    private Vector2 _lastViewerPosition;
    private float _sqrUpdateThreshold;
    private Dictionary<Vector2I, TerrainGenerator> _terrainChunks = new Dictionary<Vector2I, TerrainGenerator>();
    private HashSet<Vector2I> _activeChunks = new HashSet<Vector2I>();

    public override void _Ready()
    {
        _viewer = GetNode<Node3D>(ViewerPath);
        _sqrUpdateThreshold = UpdateThreshold * UpdateThreshold;
        UpdateChunks();
    }

    public override void _Process(double delta)
    {
        _viewerPosition = new Vector2(_viewer.Position.X, _viewer.Position.Z);

        if (_viewerPosition.DistanceSquaredTo(_lastViewerPosition) > _sqrUpdateThreshold)
        {
            _lastViewerPosition = _viewerPosition;
            UpdateChunks();
        }
    }

    private void UpdateChunks()
    {
        // Determine current chunk coordinates
        Vector2I currentChunkCoord = new Vector2I(
            (int)Mathf.Floor(_viewerPosition.X / ChunkSize),
            (int)Mathf.Floor(_viewerPosition.Y / ChunkSize)
        );

        // Track chunks that need to be removed
        HashSet<Vector2I> chunksToRemove = new HashSet<Vector2I>(_activeChunks);
        _activeChunks.Clear();

        // Create/update visible chunks
        for (int yOffset = -ViewDistance; yOffset <= ViewDistance; yOffset++)
        {
            for (int xOffset = -ViewDistance; xOffset <= ViewDistance; xOffset++)
            {
                Vector2I chunkCoord = new Vector2I(
                    currentChunkCoord.X + xOffset,
                    currentChunkCoord.Y + yOffset
                );

                _activeChunks.Add(chunkCoord);

                if (!_terrainChunks.ContainsKey(chunkCoord))
                {
                    CreateChunk(chunkCoord);
                }
                else
                {
                    chunksToRemove.Remove(chunkCoord);
                }
            }
        }

        // Remove out-of-range chunks
        foreach (Vector2I coord in chunksToRemove)
        {
            if (_terrainChunks.TryGetValue(coord, out TerrainGenerator chunk))
            {
                chunk.QueueFree();
                _terrainChunks.Remove(coord);
            }
        }
    }

    private void CreateChunk(Vector2I coord)
    {
        var chunk = new TerrainGenerator();

        // Configure chunk
        chunk.ChunkOffset = new Vector2(coord.X * ChunkSize, coord.Y * ChunkSize);
        chunk.Size = ChunkSize;
        chunk.Position = new Vector3(
            coord.X * ChunkSize,
            0,
            coord.Y * ChunkSize
        );

        // Copy terrain settings from first chunk if needed
        if (GetChildCount() > 0)
        {
            TerrainGenerator sample = GetChild<TerrainGenerator>(0);
            chunk.Height = sample.Height;
            chunk.Resolution = sample.Resolution;
            chunk.NoiseFrequency = sample.NoiseFrequency;
            chunk.Noise = sample.Noise.Duplicate() as FastNoiseLite;
        }

        AddChild(chunk);
        chunk.UpdateMesh();
        _terrainChunks[coord] = chunk;
    }
}