using Godot;
using System;
using System.Collections.Generic;

public partial class EndlessTerrain : Node
{
    [Export]
    public TerrainGenerator TerrainGenerator;
    [Export]
    public Node3D Player; 
    [Export]
    public int ChunkSize = 32; // Debe coincidir con el Size del TerrainGenerator
    [Export]
    public int LoadDistance = 3; // Radio de chunks a cargar

    private Dictionary<Vector2I, TerrainGenerator> _activeChunks = new();
    private Vector2I _lastPlayerChunkPos;

    public override void _Ready()
    {
        if (Player == null)
            GD.PrintErr("¡Jugador no asignado!");

        UpdateTerrainChunks();
    }

    public override void _Process(double delta)
    {
        Vector2I currentChunkPos = GetCurrentChunkPosition();
        if (currentChunkPos != _lastPlayerChunkPos)
        {
            UpdateTerrainChunks();
            _lastPlayerChunkPos = currentChunkPos;
        }
    }

    private Vector2I GetCurrentChunkPosition()
    {
        Vector3 playerPos = Player.GlobalPosition;
        return new Vector2I(
            Mathf.FloorToInt(playerPos.X / ChunkSize),
            Mathf.FloorToInt(playerPos.Z / ChunkSize)
        );
    }

    private void UpdateTerrainChunks()
    {
        Vector2I currentChunk = GetCurrentChunkPosition();
        HashSet<Vector2I> chunksToKeep = new();

        // Generar nuevos chunks alrededor
        for (int xOffset = -LoadDistance; xOffset <= LoadDistance; xOffset++)
        {
            for (int yOffset = -LoadDistance; yOffset <= LoadDistance; yOffset++)
            {
                Vector2I chunkCoord = new(currentChunk.X + xOffset, currentChunk.Y + yOffset);
                chunksToKeep.Add(chunkCoord);

                if (!_activeChunks.ContainsKey(chunkCoord))
                {
                    CreateChunk(chunkCoord);
                }
            }
        }

        // Eliminar chunks lejanos
        List<Vector2I> chunksToRemove = new();
        foreach (var chunk in _activeChunks.Keys)
        {
            if (!chunksToKeep.Contains(chunk))
            {
                chunksToRemove.Add(chunk);
            }
        }

        foreach (var chunk in chunksToRemove)
        {
            _activeChunks[chunk].QueueFree();
            _activeChunks.Remove(chunk);
        }
    }

    private void CreateChunk(Vector2I chunkCoord)
    {
        // Instanciar nuevo chunk
        TerrainGenerator newChunk = (TerrainGenerator)TerrainGenerator.Duplicate();
        newChunk.Size = ChunkSize;
        newChunk.Noise = TerrainGenerator.Noise;
        newChunk.Height = TerrainGenerator.Height;
        newChunk.Resolution = TerrainGenerator.Resolution;

        // Posicionar el chunk
        Vector3 position = new Vector3(
            chunkCoord.X * ChunkSize,
            0,
            chunkCoord.Y * ChunkSize
        );

        newChunk.GlobalPosition = position;
        AddChild(newChunk);
        _activeChunks.Add(chunkCoord, newChunk);
        newChunk.UpdateMesh();
    }
}