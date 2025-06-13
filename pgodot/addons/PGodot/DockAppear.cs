#if TOOLS
using Godot;
using System;

[Tool]
public partial class DockAppear : EditorPlugin
{
    private Control mainPanelInstance;

    public override void _EnterTree()
    {
        GD.Print("Plugin enabled - entering tree");

        // Load the scene at runtime
        var scene = GD.Load<PackedScene>("res://addons/PGodot/UI_Dock_PG.tscn");
        if (scene == null)
        {
            GD.PrintErr("Failed to load the plugin panel scene.");
            return;
        }

        // Instantiate and configure
        mainPanelInstance = (Control)scene.Instantiate();

        // Add to the LEFT dock area (Scene/Inspector zone)
        AddControlToDock(DockSlot.RightBl, mainPanelInstance);

        // Expand panel to fill
        mainPanelInstance.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        mainPanelInstance.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        // Optional: make your plugin appear active
        CallDeferred(nameof(FocusPluginPanel));
    }

    private void FocusPluginPanel()
    {
        if (mainPanelInstance != null)
        {
            // Grab focus if possible
            mainPanelInstance.GrabFocus();
            GD.Print("Plugin panel focused.");
        }
    }

    public override void _ExitTree()
    {
        if (mainPanelInstance != null)
        {
            RemoveControlFromDocks(mainPanelInstance);
            mainPanelInstance.QueueFree();
        }
    }

    public override string _GetPluginName() => "Main Dock Plugin";

    public override Texture2D _GetPluginIcon()
    {
        return EditorInterface.Singleton.GetEditorTheme().GetIcon("Node", "EditorIcons");
    }
}
#endif
