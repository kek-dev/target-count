using Dalamud.Configuration;
using System;
using System.Numerics;

namespace UnpleasantPersonPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public float TextSize { get; set; } = 200f;
    public Vector2 CounterPosition { get; set; } = new Vector2(10, 10);
    public bool HideWhenZero { get; set; } = false;
    public bool DropShadow { get; set; } = true;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
