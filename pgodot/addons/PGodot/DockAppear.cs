﻿#if TOOLS
using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class DockAppear : EditorPlugin
{
    private Control mainPanelInstance;
    private TerrainGenerator terrainGenerator;
    private EndlessTerrain endlessTerrain;

    // UI Controls
    private Button btnUpdate;
    private Button btnExportScene;
    private HSlider sliderFrequency;
    private HSlider sliderOctaves;
    private HSlider sliderPersistence;
    private HSlider sliderLacunarity;
    private HSlider sliderSize;
    private HSlider sliderHeight;
    private HSlider sliderResolution;
    private HSlider sliderFlatness;
    private HSlider sliderNoiseMin;
    private HSlider sliderNoiseMax;
    private HSlider sliderSmoothness;
    private HSlider sliderOffsetX;
    private HSlider sliderOffsetY;
    private HSlider sliderTextureScale;
    private CheckBox checkWireframe;
    private Button btnEditGradient;

    public override void _EnterTree()
    {
        mainPanelInstance = new Control();
        mainPanelInstance.Name = "TerrainDock";
        CreateUI();
        AddControlToDock(DockSlot.RightBl, mainPanelInstance);
        FindTerrainGenerator();
        FindEndlessTerrain();
    }

    private void FindEndlessTerrain()
    {
        var editorInterface = EditorInterface.Singleton;
        var editedSceneRoot = editorInterface.GetEditedSceneRoot();

        if (editedSceneRoot != null)
        {
            var terrains = new List<EndlessTerrain>();
            FindEndlessTerrainsRecursive(editedSceneRoot, terrains);

            if (terrains.Count > 0)
            {
                endlessTerrain = terrains[0];
                GD.Print("EndlessTerrain found: " + endlessTerrain.Name);
            }
            else
            {
                GD.Print("No EndlessTerrain found in scene");
            }
        }
    }

    private void FindEndlessTerrainsRecursive(Node node, List<EndlessTerrain> terrains)
    {
        if (node is EndlessTerrain terrain)
        {
            terrains.Add(terrain);
        }

        foreach (Node child in node.GetChildren())
        {
            FindEndlessTerrainsRecursive(child, terrains);
        }
    }

    private void CreateUI()
    {
        var vbox = new VBoxContainer();
        vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        mainPanelInstance.AddChild(vbox);

        // Update Button
        btnUpdate = new Button
        {
            Text = "Update Terrain",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(btnUpdate);

        // Export Scene Button
        btnExportScene = new Button
        {
            Text = "Export as Scene",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(btnExportScene);
        vbox.AddChild(new HSeparator());

        // Control Groups
        AddControlGroup(vbox, "NOISE SETTINGS",
            CreateSlider("Frequency", 0, 20, 0.2f, out sliderFrequency),
            CreateSlider("Octaves", 1, 16, 1, out sliderOctaves),
            CreateSlider("Persistence", 0, 1, 0.01f, out sliderPersistence),
            CreateSlider("Lacunarity", 0, 4, 0.1f, out sliderLacunarity),
            CreateSlider("Noise Min", 0, 100, 0.1f, out sliderNoiseMin),
            CreateSlider("Noise Max", 0, 100, 0.1f, out sliderNoiseMax),
            CreateSlider("Smoothness", 0, 1, 0.01f, out sliderSmoothness),
            CreateSlider("Offset X", -1000, 1000, 1, out sliderOffsetX),
            CreateSlider("Offset Y", -1000, 1000, 1, out sliderOffsetY)
        );

        AddControlGroup(vbox, "TERRAIN SETTINGS",
            CreateSlider("Size", 1, 1000, 1, out sliderSize),
            CreateSlider("Height", 1, 1000, 1, out sliderHeight),
            CreateSlider("Resolution", 1, 256, 1, out sliderResolution),
            CreateSlider("Flatness", 0.1f, 10, 0.1f, out sliderFlatness)
        );

        AddControlGroup(vbox, "VISUAL SETTINGS",
            CreateSlider("Texture Scale", 0.1f, 10, 0.1f, out sliderTextureScale)
        );

        // Gradient Editor Button
        var gradientGroup = new VBoxContainer();
        var gradientLabel = new Label
        {
            Text = "TERRAIN COLOR GRADIENT",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        gradientGroup.AddChild(gradientLabel);

        btnEditGradient = new Button
        {
            Text = "Edit Gradient",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        gradientGroup.AddChild(btnEditGradient);
        vbox.AddChild(gradientGroup);
        vbox.AddChild(new HSeparator());

        // Wireframe Checkbox
        var wireframeContainer = new HBoxContainer();
        wireframeContainer.AddChild(new Label
        {
            Text = "Wireframe",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(100, 0)
        });
        checkWireframe = new CheckBox();
        wireframeContainer.AddChild(checkWireframe);
        vbox.AddChild(wireframeContainer);

        ConnectSignals();
    }

    private void AddControlGroup(VBoxContainer vbox, string groupName, params Control[] controls)
    {
        var groupLabel = new Label
        {
            Text = groupName,
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(groupLabel);

        foreach (var control in controls)
        {
            vbox.AddChild(control);
        }

        vbox.AddChild(new HSeparator());
    }

    private Control CreateSlider(string label, float min, float max, float step, out HSlider slider)
    {
        var container = new HBoxContainer();

        var labelControl = new Label
        {
            Text = label,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(100, 0)
        };

        slider = new HSlider
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        container.AddChild(labelControl);
        container.AddChild(slider);

        return container;
    }

    private void ConnectSignals()
    {
        btnUpdate.Pressed += OnUpdatePressed;
        btnEditGradient.Pressed += OnEditGradientPressed;
        btnExportScene.Pressed += OnExportScenePressed;

        sliderFrequency.ValueChanged += (value) => UpdateTerrainProperty("NoiseFrequency", value);
        sliderOctaves.ValueChanged += (value) => UpdateTerrainProperty("Octaves", value);
        sliderPersistence.ValueChanged += (value) => UpdateTerrainProperty("Persistence", value);
        sliderLacunarity.ValueChanged += (value) => UpdateTerrainProperty("Lacunarity", value);
        sliderSize.ValueChanged += (value) => UpdateTerrainProperty("Size", value);
        sliderHeight.ValueChanged += (value) => UpdateTerrainProperty("Height", value);
        sliderResolution.ValueChanged += (value) => UpdateTerrainProperty("Resolution", value);
        sliderFlatness.ValueChanged += (value) => UpdateTerrainProperty("Flatness", value);
        sliderNoiseMin.ValueChanged += (value) => UpdateTerrainProperty("NoiseMin", value);
        sliderNoiseMax.ValueChanged += (value) => UpdateTerrainProperty("NoiseMax", value);
        sliderSmoothness.ValueChanged += (value) => UpdateTerrainProperty("Smoothness", value);
        sliderOffsetX.ValueChanged += (value) => UpdateTerrainProperty("NoiseOffsetX", value);
        sliderOffsetY.ValueChanged += (value) => UpdateTerrainProperty("NoiseOffsetY", value);
        sliderTextureScale.ValueChanged += (value) => UpdateTerrainProperty("TextureScale", value);

        checkWireframe.Toggled += (pressed) =>
        {
            UpdateTerrainProperty("Wireframe", pressed ? 1.0 : 0.0);
        };
    }

    private void FindTerrainGenerator()
    {
        var editorInterface = EditorInterface.Singleton;
        var editedSceneRoot = editorInterface.GetEditedSceneRoot();

        if (editedSceneRoot != null)
        {
            var generators = new List<TerrainGenerator>();
            FindGeneratorsRecursive(editedSceneRoot, generators);

            if (generators.Count > 0)
            {
                terrainGenerator = generators[0];
                UpdateUIFromTerrain();
                GD.Print("TerrainGenerator found: " + terrainGenerator.Name);
            }
            else
            {
                GD.Print("Creating new TerrainGenerator");
                terrainGenerator = new TerrainGenerator { Name = "TerrainGenerator" };
                editedSceneRoot.AddChild(terrainGenerator);
                terrainGenerator.Owner = editedSceneRoot;
                UpdateUIFromTerrain();
            }
        }
        else
        {
            GD.PrintErr("No scene root found");
        }
    }

    private void FindGeneratorsRecursive(Node node, List<TerrainGenerator> generators)
    {
        if (node is TerrainGenerator generator)
        {
            generators.Add(generator);
        }

        foreach (Node child in node.GetChildren())
        {
            FindGeneratorsRecursive(child, generators);
        }
    }

    private void UpdateUIFromTerrain()
    {
        if (terrainGenerator == null)
        {
            GD.PrintErr("No TerrainGenerator assigned");
            return;
        }

        GD.Print("Updating UI from TerrainGenerator");

        sliderFrequency.Value = terrainGenerator.NoiseFrequency * 1000;
        sliderOctaves.Value = terrainGenerator.Octaves;
        sliderPersistence.Value = terrainGenerator.Persistence;
        sliderLacunarity.Value = terrainGenerator.Lacunarity;
        sliderSize.Value = terrainGenerator.Size;
        sliderHeight.Value = terrainGenerator.Height;
        sliderResolution.Value = terrainGenerator.Resolution;
        sliderFlatness.Value = terrainGenerator.Flatness;
        sliderNoiseMin.Value = terrainGenerator.NoiseMin;
        sliderNoiseMax.Value = terrainGenerator.NoiseMax;
        sliderOffsetX.Value = terrainGenerator.NoiseOffsetX;
        sliderOffsetY.Value = terrainGenerator.NoiseOffsetY;
        sliderTextureScale.Value = terrainGenerator.TextureScale;
        checkWireframe.ButtonPressed = terrainGenerator.Wireframe;
    }

    private void UpdateTerrainProperty(string propertyName, double value)
    {
        if (terrainGenerator == null)
        {
            GD.PrintErr("No TerrainGenerator to update");
            return;
        }

        GD.Print($"Updating property: {propertyName} = {value}");

        switch (propertyName)
        {
            case "NoiseFrequency":
                terrainGenerator.NoiseFrequency = (float)value / 1000;
                if (endlessTerrain != null && endlessTerrain.NoiseTemplate != null)
                    endlessTerrain.NoiseTemplate.Frequency = (float)value / 1000;
                break;
            case "Octaves":
                terrainGenerator.Octaves = (int)value;
                if (endlessTerrain != null && endlessTerrain.NoiseTemplate != null)
                    endlessTerrain.NoiseTemplate.FractalOctaves = (int)value;
                break;
            case "Persistence":
                terrainGenerator.Persistence = (float)value;
                if (endlessTerrain != null && endlessTerrain.NoiseTemplate != null)
                    endlessTerrain.NoiseTemplate.FractalGain = (float)value;
                break;
            case "Lacunarity":
                terrainGenerator.Lacunarity = (float)value;
                if (endlessTerrain != null && endlessTerrain.NoiseTemplate != null)
                    endlessTerrain.NoiseTemplate.FractalLacunarity = (float)value;
                break;
            case "Size":
                terrainGenerator.Size = (int)value;
                break;
            case "Height":
                terrainGenerator.Height = (int)value;
                if (endlessTerrain != null && endlessTerrain.TerrainTemplate != null)
                    endlessTerrain.TerrainTemplate.Height = (int)value;
                break;
            case "Resolution":
                terrainGenerator.Resolution = (int)value;
                break;
            case "Flatness":
                terrainGenerator.Flatness = (float)value;
                if (endlessTerrain != null && endlessTerrain.TerrainTemplate != null)
                    endlessTerrain.TerrainTemplate.Flatness = (float)value;
                break;
            case "NoiseMin":
                terrainGenerator.NoiseMin = (float)value;
                if (endlessTerrain != null && endlessTerrain.TerrainTemplate != null)
                    endlessTerrain.TerrainTemplate.NoiseMin = (float)value;
                break;
            case "NoiseMax":
                terrainGenerator.NoiseMax = (float)value;
                if (endlessTerrain != null && endlessTerrain.TerrainTemplate != null)
                    endlessTerrain.TerrainTemplate.NoiseMax = (float)value;
                break;
            case "NoiseOffsetX":
                terrainGenerator.NoiseOffsetX = (float)value;
                if (endlessTerrain != null && endlessTerrain.TerrainTemplate != null)
                    endlessTerrain.TerrainTemplate.NoiseOffsetX = (float)value;
                break;
            case "NoiseOffsetY":
                terrainGenerator.NoiseOffsetY = (float)value;
                if (endlessTerrain != null && endlessTerrain.TerrainTemplate != null)
                    endlessTerrain.TerrainTemplate.NoiseOffsetY = (float)value;
                break;
            case "TextureScale":
                terrainGenerator.TextureScale = (float)value;
                if (endlessTerrain != null && endlessTerrain.TerrainTemplate != null)
                    endlessTerrain.TerrainTemplate.TextureScale = (float)value;
                break;
            case "Wireframe":
                terrainGenerator.Wireframe = value > 0.5;
                if (endlessTerrain != null && endlessTerrain.TerrainTemplate != null)
                    endlessTerrain.TerrainTemplate.Wireframe = value > 0.5;
                break;
        }

        terrainGenerator.CallDeferred("UpdateMesh");

        if (endlessTerrain != null)
        {
            endlessTerrain.CallDeferred("UpdateAllChunks");
        }
    }

    private void OnEditGradientPressed()
    {
        if (terrainGenerator != null && terrainGenerator.Gradient != null)
        {
            EditorInterface.Singleton.InspectObject(terrainGenerator.Gradient);

            if (!terrainGenerator.Gradient.IsConnected("changed", Callable.From(OnGradientChanged)))
            {
                terrainGenerator.Gradient.Changed += OnGradientChanged;
            }
        }
    }

    private void OnGradientChanged()
    {
        terrainGenerator?.CallDeferred("MarkGradientDirty");
        if (endlessTerrain != null && endlessTerrain.TerrainTemplate != null)
        {
            endlessTerrain.TerrainTemplate.Gradient = terrainGenerator.Gradient;
            endlessTerrain.CallDeferred("UpdateAllChunks");
        }
    }

    private void OnUpdatePressed()
    {
        terrainGenerator?.CallDeferred("UpdateMesh");
        if (endlessTerrain != null)
        {
            endlessTerrain.CallDeferred("UpdateAllChunks");
        }
    }

    private void OnExportScenePressed()
    {
        var editorInterface = EditorInterface.Singleton;
        var editedSceneRoot = editorInterface.GetEditedSceneRoot();

        if (editedSceneRoot == null)
        {
            GD.PrintErr("No scene is currently open");
            return;
        }

        // Create a root node for the exported scene
        Node exportRoot = new Node3D();
        exportRoot.Name = "ExportedTerrainScene";

        // Find and duplicate the TerrainGenerator
        Node terrainGeneratorNode = editedSceneRoot.FindChild("TerrainGenerator", true, false) as Node;
        if (terrainGeneratorNode != null)
        {
            Node terrainCopy = terrainGeneratorNode.Duplicate();
            exportRoot.AddChild(terrainCopy);
            terrainCopy.Owner = exportRoot;
        }
        else
        {
            GD.PrintErr("TerrainGenerator node not found in the scene");
        }

        // Find and duplicate the EndlessTerrain
        Node endlessTerrainNode = editedSceneRoot.FindChild("EndlessTerrain", true, false) as Node;
        if (endlessTerrainNode != null)
        {
            Node endlessTerrainCopy = endlessTerrainNode.Duplicate();
            exportRoot.AddChild(endlessTerrainCopy);
            endlessTerrainCopy.Owner = exportRoot;
        }
        else
        {
            GD.Print("EndlessTerrain node not found in the scene - exporting without endless terrain");
        }

        // Find and duplicate the Player
        Node playerNode = editedSceneRoot.FindChild("Player", true, false) as Node;
        if (playerNode != null)
        {
            Node playerCopy = playerNode.Duplicate();
            exportRoot.AddChild(playerCopy);
            playerCopy.Owner = exportRoot;
        }
        else
        {
            GD.Print("Player node not found in the scene - exporting without player");
        }

        // Specify export path
        string exportPath = "res://addons/PGodot/TerrainsGenerated/";
        string sceneName = $"TerrainExport_{DateTime.Now:yyyyMMdd_HHmmss}.tscn";

        // Create directory if it doesn't exist
        if (!DirAccess.DirExistsAbsolute(exportPath))
        {
            DirAccess.MakeDirRecursiveAbsolute(exportPath);
        }

        // Create and save the new scene
        PackedScene packedScene = new PackedScene();
        packedScene.Pack(exportRoot);

        Error error = ResourceSaver.Save(packedScene, exportPath + sceneName);

        if (error == Error.Ok)
        {
            GD.Print($"Scene exported successfully to: {exportPath + sceneName}");
            // Refresh the file system to make the new scene visible in the editor
            EditorInterface.Singleton.GetResourceFilesystem().Scan();
        }
        else
        {
            GD.PrintErr($"Failed to export scene. Error: {error}");
        }

        // Clean up
        exportRoot.QueueFree();
    }
    public override void _ExitTree()
    {
        if (terrainGenerator != null && terrainGenerator.Gradient != null)
        {
            if (terrainGenerator.Gradient.IsConnected("changed", Callable.From(OnGradientChanged)))
            {
                terrainGenerator.Gradient.Disconnect("changed", Callable.From(OnGradientChanged));
            }
        }

        if (mainPanelInstance != null)
        {
            RemoveControlFromDocks(mainPanelInstance);
            mainPanelInstance.QueueFree();
        }
    }

    public override string _GetPluginName() => "Terrain Generator";

    public override Texture2D _GetPluginIcon()
    {
        return EditorInterface.Singleton.GetEditorTheme().GetIcon("GridMap", "EditorIcons");
    }
}
#endif