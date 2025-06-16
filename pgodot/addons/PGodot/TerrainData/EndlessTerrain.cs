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

        // Find Player by name if not set in inspector
        if (Player == null)
        {
            Player = GetTree().CurrentScene.FindChild("Reference", true) as Node3D;

            if (Player == null)
            {
                GD.PrintErr("Reference node not found in scene, setting the reference on 0,0,0!");
            }
        }

        // Find TerrainGenerator under node named "node1"
        if (TerrainTemplate == null)
        {
            var node1 = GetTree().CurrentScene.FindChild("TerrainGenerator", true);
            if (node1 != null)
            {
                foreach (Node child in node1.GetChildren())
                {
                    if (child is TerrainGenerator terrainGen)
                    {
                        TerrainTemplate = terrainGen;
                        break;
                    }
                }
            }

            if (TerrainTemplate == null)
            {
                GD.PrintErr("TerrainGenerator template not found under TerrainGenerator node!");
            }
        }

        // Editor-only filesystem refresh
        RefreshEditorFilesystem();
    }

    private void RefreshEditorFilesystem()
    {
#if TOOLS
        if (Engine.IsEditorHint())
        {
            try
            {
                EditorInterface.Singleton?.GetResourceFilesystem()?.Scan();
            }
            catch (System.Exception e)
            {
                GD.Print("Editor filesystem refresh skipped (not in editor): ", e.Message);
            }
        }
#endif
    }

    public override void _Process(double delta)
    {
        if (Player != null)
        {
            _playerPosition = new Vector2(Player.Position.X, Player.Position.Z);
        }
        else
        {
            _playerPosition = new Vector2(0, 0);
        }
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
        if (Player == null) return false;
        Vector2 viewerPos = new Vector2(Player.Position.X, Player.Position.Z);
        float distance = chunkPosition.DistanceTo(viewerPos);
        return distance <= ViewDistance;
    }

    private void CreateNewChunk(Vector2 coord, Vector2 position)
    {
        TerrainGenerator newChunk = TerrainTemplate != null ?
            (TerrainGenerator)TerrainTemplate.Duplicate() :
            new TerrainGenerator();

        AddChild(newChunk);

        newChunk.Name = $"TerrainChunk_{coord.X}_{coord.Y}";
        newChunk.Position = new Vector3(position.X, 0, position.Y);

        if (NoiseTemplate != null)
        {
            newChunk.Noise = (FastNoiseLite)NoiseTemplate.Duplicate();
        }

        newChunk.Initialize(position, ChunkSize);

        if (TerrainTemplate != null)
        {
            CopyAllParametersToChunk(newChunk, true);
        }

        newChunk.UpdateCollisions();
        newChunk.Visible = IsChunkVisible(position);

        _terrainChunks.Add(coord, newChunk);
        _lastVisibleChunks.Add(newChunk);
    }

    private void CopyAllParametersToChunk(TerrainGenerator chunk, bool forceUpdate = false)
    {
        if (TerrainTemplate == null) return;

        chunk.Flatness = TerrainTemplate.Flatness;
        chunk.Height = TerrainTemplate.Height;
        chunk.Resolution = TerrainTemplate.Resolution;
        chunk.NoiseFrequency = TerrainTemplate.NoiseFrequency;
        chunk.NoiseMin = TerrainTemplate.NoiseMin;
        chunk.NoiseMax = TerrainTemplate.NoiseMax;
        chunk.Wireframe = TerrainTemplate.Wireframe;
        chunk.TextureScale = TerrainTemplate.TextureScale;
        chunk.Octaves = TerrainTemplate.Octaves;
        chunk.Persistence = TerrainTemplate.Persistence;
        chunk.Lacunarity = TerrainTemplate.Lacunarity;
        chunk.NoiseOffsetX = TerrainTemplate.NoiseOffsetX;
        chunk.NoiseOffsetY = TerrainTemplate.NoiseOffsetY;

        chunk.Gradient = TerrainTemplate.Gradient != null ?
            (Gradient)TerrainTemplate.Gradient.Duplicate() : null;

        chunk.HeightCurve = TerrainTemplate.HeightCurve != null ?
            (Curve)TerrainTemplate.HeightCurve.Duplicate() : null;

        if (TerrainTemplate.Noise != null && chunk.Noise != null)
        {
            chunk.Noise.Frequency = TerrainTemplate.Noise.Frequency;
            chunk.Noise.FractalOctaves = TerrainTemplate.Noise.FractalOctaves;
            chunk.Noise.FractalGain = TerrainTemplate.Noise.FractalGain;
            chunk.Noise.FractalLacunarity = TerrainTemplate.Noise.FractalLacunarity;
            chunk.Noise.NoiseType = TerrainTemplate.Noise.NoiseType;
        }

        if (forceUpdate)
        {
            chunk.ConfigureNoise();
            chunk.UpdateMesh();
        }
    }
}