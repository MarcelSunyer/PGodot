using Godot;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        if (Player == null)
        {
            GD.Print("Player not found");
            // Refresh the file system to make the new scene visible in the editor
            EditorInterface.Singleton.GetResourceFilesystem().Scan();
        }
       
    }

    public override void _Process(double delta)
    {
        if (Player != null)
        {
            _playerPosition = new Vector2(Player.Position.X, Player.Position.Z);
        }
        else{
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
        Vector2 viewerPos = new Vector2(Player.Position.X, Player.Position.Z);
        float distance = chunkPosition.DistanceTo(viewerPos);
        return distance <= ViewDistance;
    }

    private void CreateNewChunk(Vector2 coord, Vector2 position)
    {
        // 1. Crear nuevo chunk duplicando el template (si existe)
        TerrainGenerator newChunk = TerrainTemplate != null ? 
            (TerrainGenerator)TerrainTemplate.Duplicate() : 
            new TerrainGenerator();

        // 2. Añadir al árbol de escena primero (importante para inicialización)
        AddChild(newChunk);

        // 3. Configurar propiedades esenciales
        newChunk.Name = $"TerrainChunk_{coord.X}_{coord.Y}";
        newChunk.Position = new Vector3(position.X, 0, position.Y);
        
        // 4. Configurar noise específico si se usa template
        if (NoiseTemplate != null)
        {
            newChunk.Noise = (FastNoiseLite)NoiseTemplate.Duplicate();
        }

        // 5. Inicializar con parámetros actualizados
        newChunk.Initialize(position, ChunkSize);
        
        // 6. Forzar actualización de parámetros
        if (TerrainTemplate != null)
        {
            CopyAllParametersToChunk(newChunk, true);
        }

        // 7. Actualizar físicas y visibilidad
        newChunk.UpdateCollisions();
        newChunk.Visible = IsChunkVisible(position);

        _terrainChunks.Add(coord, newChunk);
        _lastVisibleChunks.Add(newChunk);
    }
    private void CopyAllParametersToChunk(TerrainGenerator chunk, bool forceUpdate = false)
    {
        if (TerrainTemplate == null) return;

        // Copiar todos los parámetros exportables
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

        // Copiar recursos (importante hacer Duplicate() para evitar compartir referencias)
        chunk.Gradient = TerrainTemplate.Gradient != null ?
            (Gradient)TerrainTemplate.Gradient.Duplicate() : null;

        chunk.HeightCurve = TerrainTemplate.HeightCurve != null ?
            (Curve)TerrainTemplate.HeightCurve.Duplicate() : null;

        // Configuración especial para el noise
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
