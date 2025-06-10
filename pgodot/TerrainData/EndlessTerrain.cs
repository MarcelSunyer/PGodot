using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class EndlessTerrain : Node3D
{
    [Export] public NodePath ViewerPath;
    [Export] public int ViewDistance = 8;
    [Export] public float UpdateThreshold = 10.0f;
    [Export] public int ChunkSize = 64;

    // Variable que controla cuántos chunks se generan al inicio
    [Export] public int InitialSpawnRadius = 1; // 1 = solo el chunk donde está el jugador

    [Export] public int Height = 32;
    [Export] public int Resolution = 1;
    [Export] public float NoiseFrequency = 0.1f;
    [Export] public FastNoiseLite Noise;

    [Export] public GradientTexture1D GradientTexture;
    [Export] public ShaderMaterial TerrainShaderMaterial;

    private Node3D _viewer;
    private Vector2 _viewerPosition;
    private Vector2 _lastViewerPosition;
    private float _sqrUpdateThreshold;
    private Dictionary<Vector2I, TerrainGenerator> _terrainChunks = new Dictionary<Vector2I, TerrainGenerator>();
    private HashSet<Vector2I> _activeChunks = new HashSet<Vector2I>();

    int amountChunks;
    public override void _Ready()
    {
        _viewer = GetNode<Node3D>(ViewerPath);
        _sqrUpdateThreshold = UpdateThreshold * UpdateThreshold;
        _viewerPosition = new Vector2(_viewer.Position.X, _viewer.Position.Z);
        _lastViewerPosition = _viewerPosition;

        // Generar chunks iniciales
        SpawnInitialChunks();
    }

    private void SpawnInitialChunks()
    {
        Vector2I currentChunkCoord = new Vector2I(
            (int)Mathf.Floor(_viewerPosition.X / ChunkSize),
            (int)Mathf.Floor(_viewerPosition.Y / ChunkSize)
        );

        // Generar chunks en un radio alrededor del jugador
        for (int y = -InitialSpawnRadius; y <= InitialSpawnRadius; y++)
        {
            for (int x = -InitialSpawnRadius; x <= InitialSpawnRadius; x++)
            {
                // Esto crea un área circular en lugar de cuadrada
                if (x * x + y * y <= InitialSpawnRadius * InitialSpawnRadius)
                {
                    Vector2I chunkCoord = new Vector2I(
                        currentChunkCoord.X + x,
                        currentChunkCoord.Y + y
                    );

                    if (!_terrainChunks.ContainsKey(chunkCoord))
                    {
                        CreateChunk(chunkCoord);
                    }
                }
            }
        }
    }

    // Resto del código permanece igual...
    public override void _Process(double delta)
    {
        _viewerPosition = new Vector2(_viewer.Position.X, _viewer.Position.Z);

        if ((_viewerPosition - _lastViewerPosition).LengthSquared() > _sqrUpdateThreshold)
        {
            _lastViewerPosition = _viewerPosition;
            UpdateChunks();
        }
    }

    private void UpdateChunks()
    {
        HashSet<Vector2I> chunksToRemove = new HashSet<Vector2I>(_activeChunks);
        _activeChunks.Clear();

        Vector2I currentChunkCoord = new Vector2I(
            (int)Mathf.Floor(_viewerPosition.X / ChunkSize),
            (int)Mathf.Floor(_viewerPosition.Y / ChunkSize)
        );

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
        amountChunks = amountChunks + 1;
        var chunk = new TerrainGenerator();

        chunk.ChunkOffset = new Vector2(coord.X * ChunkSize, coord.Y * ChunkSize);
        chunk.Size = ChunkSize;
        chunk.Height = Height;
        chunk.Resolution = Resolution;
        chunk.NoiseFrequency = NoiseFrequency;

        if (Noise != null)
        {
            chunk.Noise = Noise.Duplicate() as FastNoiseLite;
        }

        chunk.Position = new Vector3(coord.X * ChunkSize, 0, coord.Y * ChunkSize);
        AddChild(chunk);
        chunk.UpdateMesh();
        _terrainChunks[coord] = chunk;

        if (chunk.Mesh is ArrayMesh arrayMesh && TerrainShaderMaterial != null)
        {
            var material = TerrainShaderMaterial.Duplicate() as ShaderMaterial;
            arrayMesh.SurfaceSetMaterial(0, material);

            if (GradientTexture != null)
            {
                material.SetShaderParameter("gradient_tex", GradientTexture);
            }
            material.SetShaderParameter("height_max", Height);
        }
    }
}