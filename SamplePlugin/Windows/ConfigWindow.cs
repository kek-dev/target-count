using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace UnpleasantPersonPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin) : base("Target Count Configuration")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(400, 180);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.configuration = plugin.Configuration;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var textSize = configuration.TextSize;
        if (ImGui.SliderFloat("Text Size (takes some time to update)", ref textSize, 12.0f, 300.0f, "%.0f"))
        {
            configuration.TextSize = textSize;
            configuration.Save();
            plugin.BuildFonts();
        }

        ImGui.Spacing();
        
        var hideWhenZero = configuration.HideWhenZero;
        if (ImGui.Checkbox("Hide when count is 0", ref hideWhenZero))
        {
            configuration.HideWhenZero = hideWhenZero;
            configuration.Save();
        }

        ImGui.Spacing();
        
        var dropShadow = configuration.DropShadow;
        if (ImGui.Checkbox("Drop shadow", ref dropShadow))
        {
            configuration.DropShadow = dropShadow;
            configuration.Save();
        }

        ImGui.Spacing();
        ImGui.TextWrapped("To reposition the counter, hold your mouse on it and drag.");

        ImGui.Spacing();
    }
}
