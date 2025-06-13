#if TOOLS
using Godot;
using System;

[Tool]
public partial class ProceduralGeneration : EditorPlugin
{
    private PackedScene _mainPanelScene = GD.Load<PackedScene>("res://addons/ProceduralGeneration/Scene_UI/SceneEditableTerrain.tscn");
    private Control _mainPanelInstance;

    public override void _EnterTree()
    {
        _mainPanelInstance = _mainPanelScene.Instantiate<Control>();
        _mainPanelInstance.Name = "ProceduralGen"; // ← clave para que aparezca como pestaña
        GetEditorInterface().GetEditorMainScreen().AddChild(_mainPanelInstance);
        _MakeVisible(false);
    }

    public override void _ExitTree()
    {
        if (_mainPanelInstance != null)
        {
            _mainPanelInstance.QueueFree();
        }
    }

    public override bool _HasMainScreen()
    {
        return true;
    }

    public override void _MakeVisible(bool visible)
    {
        if (_mainPanelInstance != null)
        {
            _mainPanelInstance.Visible = visible;
        }
    }

    public override bool _Handles(GodotObject @object)
    {
        return @object.IsClass(_mainPanelScene.ResourcePath);
    }

    public override string _GetPluginName()
    {
        return "ProceduralGeneration Editor";
    }

    public override Texture2D _GetPluginIcon()
    {
        return GetEditorInterface().GetBaseControl().GetThemeIcon("Node", "EditorIcons");
    }
}
#endif