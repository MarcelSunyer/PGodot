#if TOOLS
using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class DockAppear : EditorPlugin
{
    private Control mainPanelInstance;
    private TerrainGenerator terrainGenerator;

    // UI Controls
    private Button btnUpdate;
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
        sliderSmoothness.Value = terrainGenerator.Smoothness;
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
                break;
            case "Octaves":
                terrainGenerator.Octaves = (int)value;
                break;
            case "Persistence":
                terrainGenerator.Persistence = (float)value;
                break;
            case "Lacunarity":
                terrainGenerator.Lacunarity = (float)value;
                break;
            case "Size":
                terrainGenerator.Size = (int)value;
                break;
            case "Height":
                terrainGenerator.Height = (int)value;
                break;
            case "Resolution":
                terrainGenerator.Resolution = (int)value;
                break;
            case "Flatness":
                terrainGenerator.Flatness = (float)value;
                break;
            case "NoiseMin":
                terrainGenerator.NoiseMin = (float)value;
                break;
            case "NoiseMax":
                terrainGenerator.NoiseMax = (float)value;
                break;
            case "Smoothness":
                terrainGenerator.Smoothness = (float)value;
                break;
            case "NoiseOffsetX":
                terrainGenerator.NoiseOffsetX = (float)value;
                break;
            case "NoiseOffsetY":
                terrainGenerator.NoiseOffsetY = (float)value;
                break;
            case "TextureScale":
                terrainGenerator.TextureScale = (float)value;
                break;
            case "Wireframe":
                // Interpreta 1.0 como true, 0.0 como false
                terrainGenerator.Wireframe = value > 0.5;
                break;
        }

        terrainGenerator.CallDeferred("UpdateMesh");
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
    }

    private void OnUpdatePressed()
    {
        terrainGenerator?.CallDeferred("UpdateMesh");
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