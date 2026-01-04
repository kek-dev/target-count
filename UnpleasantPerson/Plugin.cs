using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using System.Numerics;
using System.Diagnostics;
using UnpleasantPersonPlugin.Windows;

namespace UnpleasantPersonPlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    public Configuration Configuration { get; init; }
    
    private IFontHandle? largeFontHandle = null;

    public readonly WindowSystem WindowSystem = new("UnpleasantPersonPlugin");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    
    private int cachedTargetCount = 0;
    private Stopwatch updateTimer = Stopwatch.StartNew();
    private const int UpdateIntervalMs = 100;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw += DrawTargetCount;
        
        Framework.Update += OnFrameworkUpdate;
        
        BuildFonts();

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw -= DrawTargetCount;
        
        largeFontHandle?.Dispose();
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
    }

    public int CountPlayersTargetingMe()
    {
        var localPlayer = ObjectTable.LocalPlayer;
        if (localPlayer == null)
        {
            return 0;
        }

        var localPlayerGameObjectId = localPlayer.GameObjectId;

        int count = 0;
        foreach (var obj in ObjectTable)
        {
            if (obj == null)
                continue;

            if (obj is IPlayerCharacter playerCharacter && 
                playerCharacter.GameObjectId != localPlayerGameObjectId && 
                obj.TargetObjectId == localPlayerGameObjectId)
            {
                count++;
            }
        }

        return count;
    }
    
    private void OnFrameworkUpdate(IFramework framework)
    {
        if (updateTimer.ElapsedMilliseconds >= UpdateIntervalMs)
        {
            cachedTargetCount = CountPlayersTargetingMe();
            updateTimer.Restart();
        }
    }
    
    internal void BuildFonts()
    {
        largeFontHandle?.Dispose();
        largeFontHandle = PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
        {
            e.OnPreBuild(tk =>
            {
                var config = new SafeFontConfig { SizePx = Configuration.TextSize };
                var font = tk.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansJpMedium, config);
                config.MergeFont = font;
                tk.AddGameSymbol(config);
                tk.SetFontScaleMode(font, FontScaleMode.UndoGlobalScale);
            });
        });
    }
    
    private void DrawTargetCount()
    {

        if (Configuration.HideWhenZero && cachedTargetCount == 0)
        {
            return;
        }

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        ImGui.SetNextWindowPos(Configuration.CounterPosition, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowBgAlpha(0.0f);
        
        ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | 
                                 ImGuiWindowFlags.NoResize | 
                                 ImGuiWindowFlags.NoScrollbar | 
                                 ImGuiWindowFlags.AlwaysAutoResize | 
                                 ImGuiWindowFlags.NoBackground;
        
        if (ImGui.Begin("TargetCountOverlay", flags))
        {
            var newPos = ImGui.GetWindowPos();
            var posDiff = Vector2.Distance(newPos, Configuration.CounterPosition);
            if (posDiff > 1.0f)
            {
                Configuration.CounterPosition = newPos;
                Configuration.Save();
            }

            var text = $"{cachedTargetCount}";
            
            if (Configuration.DropShadow)
            {
                var shadowOffset = new Vector2(2, 2);
                var shadowColor = ImGui.GetColorU32(new Vector4(0, 0, 0, 0.5f));
                
                if (largeFontHandle != null && largeFontHandle.Available)
                {
                    using (largeFontHandle.Push())
                    {
                        var textPos = ImGui.GetCursorScreenPos();
                        var drawList = ImGui.GetWindowDrawList();
                        drawList.AddText(textPos + shadowOffset, shadowColor, text);
                        ImGui.Text(text);
                    }
                }
            }
            else
            {
                if (largeFontHandle != null && largeFontHandle.Available)
                {
                    using (largeFontHandle.Push())
                    {
                        ImGui.Text(text);
                    }
                }
            }
        }
        ImGui.End();
        ImGui.PopStyleVar();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
