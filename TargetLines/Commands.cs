using Dalamud.Game.Command;
using DrahsidLib;

namespace TargetLines;

internal static class Commands {
    public static void Initialize() {
        Service.CommandManager.AddHandler("/ptlines", new CommandInfo(OnPTLines)
        {
            ShowInHelp = true,
            HelpMessage = "开启/关闭设置界面"
        });

        Service.CommandManager.AddHandler("/ttl", new CommandInfo(OnTTL)
        {
            ShowInHelp = true,
            HelpMessage = "开启/关闭目标线绘制"
        });
    }

    public static void Dispose() {
        Service.CommandManager.RemoveHandler("/ptlines");
        Service.CommandManager.RemoveHandler("/ttl");
    }

    public static void ToggleConfig() {
        Windows.Config.IsOpen = !Windows.Config.IsOpen;
    }

    public static void OnPTLines(string command, string args) {
        Windows.Config.IsOpen = !Windows.Config.IsOpen;
    }

    private static void OnTTL(string command, string args) {
        string str = "打开了";
        Globals.Config.saved.ToggledOff = !Globals.Config.saved.ToggledOff;

        if (Globals.Config.saved.ToggledOff) {
            str = "关闭了";
        }

        Service.ChatGui.Print($"目标线绘制 {str}");
    }
}
