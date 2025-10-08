using DrahsidLib;
using Dalamud.Bindings.ImGui;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static TargetLines.ClassJobHelper;

namespace TargetLines;

internal enum ConfigPerformanceImpact {
    优化,
    无,
    低,
    中,
    高,
}

internal class ConfigWindow : WindowWrapper {
    public static string ConfigWindowName = "Target Lines EX 设置";
    private static Vector2 MinSize = new Vector2(240, 240);

    private readonly Vector4 PerformanceHighColor = new Vector4(1, 0, 0, 1);
    private readonly Vector4 PerformanceMedColor = new Vector4(1, 1, 0, 1);
    private readonly Vector4 PerformanceLowColor = new Vector4(1, 1, 0.5f, 0.5f);
    private readonly Vector4 PerformanceNoneColor = new Vector4(1, 1, 1, 1);
    private readonly Vector4 PerformanceBeneficialColor = new Vector4(0, 1, 0, 1);

    public ConfigWindow() : base(ConfigWindowName, MinSize) { }

    private string AddSpacesToCamelCase(string text) {
        if (string.IsNullOrEmpty(text)) {
            return string.Empty;
        }

        StringBuilder result = new StringBuilder(text.Length * 2);
        result.Append(text[0]);

        for (int index = 1; index < text.Length; index++) {
            if (char.IsUpper(text[index]) && !char.IsUpper(text[index - 1])) {
                result.Append(' ');
            }
            result.Append(text[index]);
        }

        return result.ToString();
    }


    private bool DrawTargetFlagEditor(ref TargetFlags flags, string guard) {
        int flag_count = Enum.GetValues(typeof(TargetFlags)).Length;
        bool should_save = false;
        float charsize = ImGui.CalcTextSize("F").X * 24;

        for (int index = 0; index < flag_count; index++) {
            TargetFlags current_flag = (TargetFlags)(1 << index);
            string label = AddSpacesToCamelCase(current_flag.ToString());
            int flags_dirty = (int)flags;
            float start = ImGui.GetCursorPosX();
            if (ImGui.CheckboxFlags($"{label}##{guard}{index}", ref flags_dirty, (int)current_flag)) {
                flags = (TargetFlags)flags_dirty;
                should_save = true;
            }
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip(TargetFlagDescriptions[index]);
            }

            if (Globals.Config.saved.CompactFlagDisplay) {
                if ((index + 1) % 4 != 0) {
                    ImGui.SameLine();
                }
            }
            else {
                int mod = (index + 1) % 2;
                if (mod != 0) {
                    ImGui.SameLine(start + charsize);
                }
            }
        }

        return should_save;
    }

    private bool DrawJobFlagEditor(ref ulong flags, string guard) {
        bool should_save = false;
        float charsize = ImGui.CalcTextSize("F").X * 24;
        if (ImGui.TreeNode($"职业##Jobs{guard}")) {
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("如果这些选项中的任何一个被启用的话，当目标是玩家时，"
                               + "只有选中的职业会绘制。如果不是玩家的话，这些选项是无效的。");
            }
            for (int index = 0; index < (int)ClassJob.Count; index++) {
                ulong flag = ClassJobToBit(index);
                bool toggled = (flags & flag) != 0;
                string label = $"{(ClassJob)index}##{guard}_{index}";
                float start = ImGui.GetCursorPosX();

                if (ImGui.Checkbox(label, ref toggled)) {
                    should_save = true;
                    if (toggled) {
                        flags |= flag;
                    }
                    else {
                        flags &= ~flag;
                    }
                }

                if (Globals.Config.saved.CompactFlagDisplay) {
                    if ((index + 1) % 4 != 0) {
                        ImGui.SameLine();
                    }
                }
                else {
                    int mod = (index + 1) % 2;
                    if (mod != 0) {
                        ImGui.SameLine(start + charsize);
                    }
                }
            }
            ImGui.NewLine();
            ImGui.TreePop();
        }

        return should_save;
    }

    // returns ImGui.IsItemHovered()
    private bool DrawPerformanceImpact(ConfigPerformanceImpact impact) {
        Vector4 color;
        bool ret = ImGui.IsItemHovered();
        ImGui.SameLine();
        switch(impact)
        {
            default:
            case ConfigPerformanceImpact.无:
                color = PerformanceNoneColor;
                break;
            case ConfigPerformanceImpact.低:
                color = PerformanceLowColor;
                break;
            case ConfigPerformanceImpact.中:
                color = PerformanceMedColor;
                break;
            case ConfigPerformanceImpact.高:
                color = PerformanceHighColor;
                break;
            case ConfigPerformanceImpact.优化:
                color = PerformanceBeneficialColor;
                break;
        }
        ImGui.TextColored(color, $"性能影响：{impact}");
        return ret;
    }

    private bool DrawFilters() {
        bool should_save = false;

        int selected = (int)Globals.Config.saved.OnlyInCombat;
        if (ImGui.ListBox("战斗设置", ref selected, Enum.GetNames(typeof(InCombatOption)))) {
            Globals.Config.saved.OnlyInCombat = (InCombatOption)selected;
            should_save = true;
        }

        should_save |= ImGui.Checkbox("只在武器抽出时显示目标线", ref Globals.Config.saved.OnlyUnsheathed);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("启用后，当武器收起时不再绘制目标线");
        }

        Vector4 color = Globals.Config.saved.LineColor.Color.Color;
        Vector4 ocolor = Globals.Config.saved.LineColor.OutlineColor.Color;

        should_save |= ImGui.Checkbox("显示默认目标线", ref Globals.Config.saved.LineColor.Visible);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("如果启用此选项，以下设置会用来绘制默认的目标线");
        }

        if (Globals.Config.saved.LineColor.Visible) {
            if (ImGui.ColorEdit4("默认颜色", ref color)) {
                Globals.Config.saved.LineColor.Color.Color = color;
                should_save = true;
            }

            if (ImGui.ColorEdit4("默认描边颜色", ref ocolor)) {
                Globals.Config.saved.LineColor.OutlineColor.Color = ocolor;
                should_save = true;
            }
        }

        ImGui.Spacing();
        ImGui.Separator();

        should_save |= ImGui.Checkbox("紧凑显示", ref Globals.Config.saved.CompactFlagDisplay);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("开启后每行会显示4个选项而不是两个");
        }

        ImGui.Text("目标过滤以及颜色设定");
        if (ImGui.Button("新建")) {
            Globals.Config.LineColors.Add(new TargetSettingsPair(new TargetSettings(), new TargetSettings(), new LineColor()));
            Globals.Config.SortLineColors();
            should_save = true;
        }

        ImGui.Spacing();

        for (int qndex = 0; qndex < Globals.Config.LineColors.Count; qndex++) {
            var settings = Globals.Config.LineColors[qndex];
            var guid = settings.UniqueId.ToString();
            int flag_count = Enum.GetValues(typeof(TargetFlags)).Length;
            List<string> from = new List<string>();
            List<string> to = new List<string>();

            color = settings.LineColor.Color.Color;
            ocolor = settings.LineColor.OutlineColor.Color;

            for (int index = 0; index < flag_count; index++) {
                TargetFlags current_flag = (TargetFlags)(1 << index);
                if (((int)settings.From.Flags & (int)current_flag) != 0) {
                    from.Add(AddSpacesToCamelCase(current_flag.ToString()));
                }
                if (((int)settings.To.Flags & (int)current_flag) != 0) {
                    to.Add(AddSpacesToCamelCase(current_flag.ToString()));
                }
            }

            int priority = settings.GetPairPriority(settings.FocusTarget);
            if (ImGui.TreeNode($"{string.Join('|', from)} -> {string.Join('|', to)} ({priority}{(settings.FocusTarget ? "F" : "")})###LineColorsEntry{guid}")) {
                if (ImGui.TreeNode($"源头###From{guid}")) {
                    if (DrawTargetFlagEditor(ref settings.From.Flags, $"From{guid}Flags")) {
                        should_save = true;
                    }

                    if (DrawJobFlagEditor(ref settings.From.Jobs, $"From{guid}Jobs")) {
                        should_save = true;
                    }
                    ImGui.TreePop();
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("目标线从这些对象发出");
                }

                if (ImGui.TreeNode($"被选中###To{guid}")) {
                    if (DrawTargetFlagEditor(ref settings.To.Flags, $"To{guid}Flags")) {
                        should_save = true;
                    }

                    if (DrawJobFlagEditor(ref settings.To.Jobs, $"To{guid}Jobs")) {
                        should_save = true;
                    }
                    ImGui.TreePop();
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("目标线指向这些对象");
                }

                if (ImGui.Checkbox("焦点目标", ref settings.FocusTarget))
                {
                    should_save = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("开启之后只有当涉及焦点目标的时候会绘制");
                }


                if (ImGui.ColorEdit4($"颜色###Color{guid}", ref color)) {
                    settings.LineColor.Color.Color = color;
                    should_save = true;
                }

                if (ImGui.ColorEdit4($"描边颜色###OColor{guid}", ref ocolor)) {
                    settings.LineColor.OutlineColor.Color = ocolor;
                    should_save = true;
                }

                if (ImGui.Checkbox($"用二次曲线绘制###UseQuad{guid}", ref settings.LineColor.UseQuad)) {
                    should_save = true;
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("开启之后这条线会用二次曲线方程计算而不是三次曲线方程。如果你想让不同的目标线具有稍微不同的形状的话，可以用这个选项。二次曲线看起来会更加圆一点。");
                }

                if (ImGui.Checkbox($"可见###Visible{guid}", ref settings.LineColor.Visible)) {
                    should_save = true;
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("关闭之后这条线将不再绘制");
                }

                if (ImGui.InputInt($"优先级###Priority{guid}", ref settings.Priority, 1, 1, default, ImGuiInputTextFlags.EnterReturnsTrue)) {
                    Globals.Config.SortLineColors();
                    should_save = true;
                    break;
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("优先级更高的规则会优先适用。输入-1的话插件会自动配置优先级。");
                }

                if (ImGui.Button($"删除###DeleteEntry{guid}")) {
                    Globals.Config.LineColors.RemoveAt(qndex);
                    Globals.Config.SortLineColors();
                    should_save = true;
                    ImGui.TreePop();
                    break;
                }

                ImGui.TreePop();
            }
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("源头 -> 目标 (优先级)");
            }

            ImGui.Separator();
        }

        ImGui.Spacing();
        ImGui.Separator();

        if (ImGui.Button("重置设置")) {
            Globals.Config.saved = new SavedConfig();
            Globals.Config.InitializeDefaultLineColorsConfig();
            Globals.Config.Save();
            TargetLineManager.InitializeTargetLines(); // reset lines
        }
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("将所有设置重置为默认设置。这会删除你添加的目标线配置！");
        }

        ImGui.SameLine();
        if (ImGui.Button("复制预设")) {
            ImGui.SetClipboardText(JsonConvert.SerializeObject(Globals.Config.LineColors));
        }
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("把你的设置复制到剪贴板");
        }

        ImGui.SameLine();
        if (ImGui.Button("粘贴预设")) {
            Globals.Config.LineColors = JsonConvert.DeserializeObject<List<TargetSettingsPair>>(ImGui.GetClipboardText());
        }
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("从剪贴板粘贴预设。这会覆盖你现有的所有配置！");
        }

        return should_save;
    }

    private bool DrawVisuals() {
        bool should_save = false;

        should_save |= ImGui.Checkbox("使用传统样式", ref Globals.Config.saved.SolidColor);
        if (DrawPerformanceImpact(ConfigPerformanceImpact.优化)) {
            ImGui.SetTooltip("开启之后目标线会使用旧版的绘制方式，而不是比较漂亮的新版绘制方式。\n"
                           + "目标线看起来会更加的平，并且不支持脉动效果，也不支持UI碰撞。但是这种绘制不会出现锯齿。基本上是青春版目标线。");
        }

        ImGui.Separator();

        if (ImGui.TreeNode("遮罩")) {
            should_save |= ImGui.Checkbox("遮罩剔除", ref Globals.Config.saved.OcclusionCulling);
            if (DrawPerformanceImpact(ConfigPerformanceImpact.高)) {
                ImGui.SetTooltip("启用之后，如果目标先的起点、中点和终点都在屏幕外的话，将不再绘制目标线。对于敌对目标来说这个选项是永远启用的。");
            }

            var level = ConfigPerformanceImpact.高;
            if (Globals.Config.saved.TextureCurveSampleCount < 32) {
                level = ConfigPerformanceImpact.中;
            }
            else if (Globals.Config.saved.DynamicSampleCount) {
                level = ConfigPerformanceImpact.低;
            }
            if (Globals.Config.saved.SolidColor == false) {
                should_save |= ImGui.Checkbox("UI碰撞剔除", ref Globals.Config.saved.UIOcclusion);

                if (DrawPerformanceImpact(level)) {
                    ImGui.SetTooltip("开启之后，与大多数UI元素相交的目标线的部分将不会绘制。这个选项的性能损耗与曲线平滑度的设置有关。");
                }
            }
            else {
                ImGui.TextDisabled($" [ {(Globals.Config.saved.UIOcclusion ? 'X' : ' ')} ] UI碰撞剔除");
                if (DrawPerformanceImpact(level)) {
                    ImGui.SetTooltip("禁用“使用传统样式”之后可以设置。");
                }
            }

            ImGui.TreePop();
        }
        ImGui.Separator();

        if (Globals.Config.saved.SolidColor == false) {
            if (ImGui.TreeNode("新式特效设置")) {
                should_save |= ImGui.Checkbox("使用脉动特效", ref Globals.Config.saved.PulsingEffect);
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("开启之后，如果使用新式绘制的话，目标线会从源头向目标周期性地闪烁");
                }

                should_save |= ImGui.Checkbox("接近目标时淡化", ref Globals.Config.saved.FadeToEnd);
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("开启后目标线在越靠近端点时会变得越透明。");
                }

                if (Globals.Config.saved.FadeToEnd) {
                    should_save |= ImGui.SliderFloat("端点不透明度 %", ref Globals.Config.saved.FadeToEndScalar, 0.0f, 1.0f);
                }
                else
                {
                    ImGui.TextDisabled("端点不透明度 %");
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("启用“接近目标时淡化”之后可以设置");
                }

                should_save |= ImGui.Checkbox("动态平滑度", ref Globals.Config.saved.DynamicSampleCount);
                if (DrawPerformanceImpact(ConfigPerformanceImpact.优化)) {
                    ImGui.SetTooltip("开启后目标线的绘制采样数会随距离自动调整。这也许能提高性能。");
                }

                int selected = (int)Globals.Config.saved.LinePartyMode;
                if (ImGui.ListBox("小队过滤模式", ref selected, Enum.GetNames(typeof(LinePartyMode))))
                {
                    Globals.Config.saved.LinePartyMode = (LinePartyMode)selected;
                    should_save = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("PartyOnly = 只绘制小队成员相关的目标线\n"
                                   + "PartyOnlyInAlliance = 在团队中，只绘制小队成员相关的目标线\n"
                                   + "AllianceOnly = 只绘制团队成员相关的目标线");
                }

                if (!Globals.Config.saved.DynamicSampleCount) {
                    should_save |= ImGui.SliderInt("采样数", ref Globals.Config.saved.TextureCurveSampleCount, 3, 512);
                    if (DrawPerformanceImpact(ConfigPerformanceImpact.中)) {
                        ImGui.SetTooltip("绘制目标线所用的采样数。降低数值可以提高性能。");
                    }

                    ImGui.TextDisabled($"[ {(Globals.Config.saved.UseScreenSpaceLOD ? 'X' : ' ')} ] 使用屏幕空间LOD");
                    if (DrawPerformanceImpact(ConfigPerformanceImpact.优化))
                    {
                        ImGui.SetTooltip("启用“动态平滑度”之后可以设置。");
                    }

                    ImGui.TextDisabled($"[ {(Globals.Config.saved.ViewAngleSampling ? 'X' : ' ')} ] 视角采样");
                    if (DrawPerformanceImpact(ConfigPerformanceImpact.优化))
                    {
                        ImGui.SetTooltip("启用“动态平滑度”之后可以设置。");
                    }
                }
                else {
                    should_save |= ImGui.SliderInt("最小平滑度", ref Globals.Config.saved.TextureCurveSampleCountMin, 3, Globals.Config.saved.TextureCurveSampleCountMax - 3);
                    if (DrawPerformanceImpact(ConfigPerformanceImpact.中)) {
                        ImGui.SetTooltip("绘制目标线所用的最小采样数。降低数值可以提高性能。");
                    }

                    should_save |= ImGui.SliderInt("最大平滑度", ref Globals.Config.saved.TextureCurveSampleCountMax, Globals.Config.saved.TextureCurveSampleCountMin + 3, 512);
                    if (DrawPerformanceImpact(ConfigPerformanceImpact.低)) {
                        ImGui.SetTooltip("绘制目标线所用的最大采样数。降低数值可以在绘制比较长的目标线时提高性能。");
                    }

                    should_save |= ImGui.Checkbox("使用屏幕空间LOD", ref Globals.Config.saved.UseScreenSpaceLOD);
                    if (DrawPerformanceImpact(ConfigPerformanceImpact.优化))
                    {
                        ImGui.SetTooltip("开启后目标线的细节层次会使用屏幕空间来采样，而不是用3D空间采样。");
                    }

                    should_save |= ImGui.Checkbox("视角采样", ref Globals.Config.saved.ViewAngleSampling);
                    if (DrawPerformanceImpact(ConfigPerformanceImpact.优化))
                    {
                        ImGui.SetTooltip("开启后如果目标线不垂直于视线的话，会降低相应的细节层次。");
                    }
                }
                ImGui.TreePop();
            }
        }
        else {
            ImGui.TextDisabled(" > 新式特效设置");
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("禁用“使用传统样式”之后可以展开。");
            }
        }
        ImGui.Separator();

        if (ImGui.TreeNode("通用目标线外观")) {
            should_save |= ImGui.SliderFloat("高度比例", ref Globals.Config.saved.HeightScale, 0.0f, 1.0f);
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("设置目标线的竖直位置。0 表示脚底，1 表示头顶。");
            }

            should_save |= ImGui.SliderFloat("玩家角色弧高", ref Globals.Config.saved.PlayerHeightBump, 0.0f, 10.0f);
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("如果目标线从玩家发出，起点会升高这么多。");
            }

            should_save |= ImGui.SliderFloat("敌对对象弧高", ref Globals.Config.saved.EnemyHeightBump, 0.0f, 10.0f);
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("如果目标线从敌人发出，起点会升高这么多。");
            }

            should_save |= ImGui.SliderFloat("曲率", ref Globals.Config.saved.ArcHeightScalar, 0.0f, 2.0f);
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("目标线中部的比例。0 会让目标线变直，1 会让目标线中点处在两个对象高度的总和处。");
            }

            should_save |= ImGui.SliderFloat("目标线粗细", ref Globals.Config.saved.LineThickness, 0.0f, 64.0f);
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("0 会让目标线消失。");
            }

            should_save |= ImGui.SliderFloat("描边粗细", ref Globals.Config.saved.OutlineThickness, 0.0f, 72.0f);
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("0 会让描边消失。");
            }

            ImGui.Spacing();
            ImGui.Spacing();
            if (ImGui.TreeNode("特效设置")) {
                should_save |= ImGui.Checkbox("使用呼吸特效", ref Globals.Config.saved.BreathingEffect);
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("开启后目标线的透明度会呈现呼吸特效。");
                }

                should_save |= ImGui.SliderFloat("振幅", ref Globals.Config.saved.WaveAmplitudeOffset, 0.0f, 0.5f);
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("呼吸特效透明度变化的最大值和最小值的差。");
                }

                should_save |= ImGui.SliderFloat("频率", ref Globals.Config.saved.WaveFrequencyScalar, 0.0f, 10.0f);
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("呼吸特效变化的快慢。");
                }
            }

            ImGui.TreePop();
        }
        ImGui.Separator();

        if (ImGui.TreeNode("动画设置")) {
            int selected = (int)Globals.Config.saved.DeathAnimation;
            if (ImGui.ListBox("目标消失动画", ref selected, Enum.GetNames(typeof(LineDeathAnimation)))) {
                Globals.Config.saved.DeathAnimation = (LineDeathAnimation)selected;
                should_save = true;
            }
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("用于计算曲率变化的方程类型。");
            }

            should_save |= ImGui.SliderFloat("目标切换动效", ref Globals.Config.saved.NewTargetEaseTime, 0.0f, 5.0f);
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("切换目标时，目标线动效持续的时长。");
            }

            should_save |= ImGui.SliderFloat("目标消失动效", ref Globals.Config.saved.NoTargetFadeTime, 0.0f, 5.0f);
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("目标消失时，目标线动效持续的时长。");
            }

            should_save |= ImGui.SliderFloat("目标消失动效权重", ref Globals.Config.saved.DeathAnimationTimeScale, 1.0f, 4.0f);
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("目标消失时动效前后半的权重。1 表示目标线在动画结束时完全变平。2 表示动画到一半时会变平。");
            }
            ImGui.TreePop();
        }
        ImGui.Separator();

        return should_save;
    }

    private bool DrawDebug() {
        bool should_save = false;

        should_save |= ImGui.Checkbox("Debug Dynamic Sample Count", ref Globals.Config.saved.DebugDynamicSampleCount);
        should_save |= ImGui.Checkbox("Debug UI Collision", ref Globals.Config.saved.DebugUICollision);
        should_save |= ImGui.Checkbox("Draw Initial UI Collision Rects (0)", ref Globals.Config.saved.DebugUIInitialRectList);
        should_save |= ImGui.Checkbox("Draw Merged UI Collision Rects (1)", ref Globals.Config.saved.DebugUIMergedRectList);
        should_save |= ImGui.Checkbox("Draw Final UI Collision Rects (2)", ref Globals.Config.saved.DebugUIFinalRectList);
        should_save |= ImGui.Checkbox("Draw the result of clipping (drawable areas)", ref Globals.Config.saved.DebugUICollisionArea);
        should_save |= ImGui.Checkbox("Debug DX Lines", ref Globals.Config.saved.DebugDXLines);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Toggling this requires a plugin restart (I am lazy)");
        }

        ImGui.TextDisabled($"Number of rendered lines: {TargetLineManager.RenderedLineCount.ToString()}");
        ImGui.TextDisabled($"Number of processed lines: {TargetLineManager.ProcessedLineCount.ToString()}");

        return should_save;
    }

    public override void Draw() {
        bool should_save = false;
        bool node_hover = false;
        bool nest = false;
        if (ImGui.BeginTabBar("ConfigTabs")) {
            if (ImGui.BeginTabItem("配置")) {
                nest = true;
                node_hover = ImGui.IsItemHovered();
                should_save |= DrawFilters();
                ImGui.EndTabItem();
            }
            if (!nest) {
                node_hover = ImGui.IsItemHovered();
            }
            if (node_hover) {
                ImGui.SetTooltip("配置目标线出现的条件和方式。");
            }

            node_hover = false;
            nest = false;
            if (ImGui.BeginTabItem("样式")) {
                nest = true;
                node_hover = ImGui.IsItemHovered();
                should_save |= DrawVisuals();
                ImGui.EndTabItem();
            }
            if (!nest) {
                node_hover = ImGui.IsItemHovered();
            }
            if (node_hover) {
                ImGui.SetTooltip("设置目标线的外观和性能。");
            }

            node_hover = false;
            nest = false;
            if (ImGui.BeginTabItem("Debug")) {
                nest = true;
                node_hover = ImGui.IsItemHovered();
                should_save |= DrawDebug();
                ImGui.EndTabItem();
            }
            if (!nest) {
                node_hover = ImGui.IsItemHovered();
            }
            if (node_hover) {
                ImGui.SetTooltip("有时候会有bug");
            }
            ImGui.EndTabBar();
        }

        if (should_save) {
            Globals.Config.Save();
            TargetLineManager.InitializeTargetLines(); // reset lines
        }
    }
}

