using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using System.Numerics;
using System.Diagnostics;
using SamplePlugin.Windows;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("SamplePlugin");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    
    private int cachedTargetCount = 0;
    private Stopwatch updateTimer = Stopwatch.StartNew();
    private const int UpdateIntervalMs = 100;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // You might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw += DrawTargetCount;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw -= DrawTargetCount;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
    }

    public int CountPlayersTargetingMe()
    {
        var localPlayer = ClientState.LocalPlayer;
        if (localPlayer == null)
        {
            Log.Warning("Local player is not available");
            return 0;
        }

        int count = 0;
        var localPlayerObjectId = localPlayer.ObjectId;

        foreach (var obj in ObjectTable)
        {
            if (obj is Character character)
            {
                if (character.ObjectId != localPlayerObjectId && 
                    character.TargetObjectId == localPlayerObjectId)
                {
                    count++;
                }
            }
        }

        return count;
    }
    
    private void DrawTargetCount()
    {
        if (updateTimer.ElapsedMilliseconds >= UpdateIntervalMs)
        {
            cachedTargetCount = CountPlayersTargetingMe();
            updateTimer.Restart();
        }
        
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.0f);
        
        ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | 
                                 ImGuiWindowFlags.NoResize | 
                                 ImGuiWindowFlags.NoMove | 
                                 ImGuiWindowFlags.NoScrollbar | 
                                 ImGuiWindowFlags.NoInputs | 
                                 ImGuiWindowFlags.AlwaysAutoResize | 
                                 ImGuiWindowFlags.NoBackground;
        
        if (ImGui.Begin("TargetCountOverlay", flags))
        {
            ImGui.SetWindowFontScale(3.0f);
            ImGui.Text($"{cachedTargetCount}");
            ImGui.SetWindowFontScale(1.0f);
        }
        ImGui.End();
        ImGui.PopStyleVar();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
