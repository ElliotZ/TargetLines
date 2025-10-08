using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using DrahsidLib;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Common.Math;
using System.Runtime.InteropServices;
using static TargetLines.ClassJobHelper;

namespace TargetLines;

public static class GameObjectExtensions {
    public static unsafe float GetScale(this IGameObject obj) {
        CSGameObject* _obj = (CSGameObject*)obj.Address;
        return _obj->Scale;
    }

    public static bool IsVisible(this IGameObject obj, bool occlusion) {
        Vector3 safePos = obj.Position;
        safePos.Y += 0.1f;

        if (obj.GetScale() == 0.0f) {
            return false;
        }

        return Globals.IsVisible(obj.GetHeadPosition(), occlusion);
    }

    public static unsafe bool TargetIsTargetable(this IGameObject obj) {
        if (obj.TargetObject == null) {
            return false;
        }
        CSGameObject* targetobj = (CSGameObject*)obj.TargetObject.Address;
        return targetobj->GetIsTargetable();
    }

    public static Vector3 GetHeadPosition(this IGameObject obj) {
        Vector3 pos = obj.Position;
        pos.Y += obj.GetCursorHeight() - 0.2f;
        return pos;
    }

    const int CursorHeightOffset = 0x124;

    public static float GetCursorHeight(this IGameObject obj)
    {
        return Marshal.PtrToStructure<float>(obj.Address + CursorHeightOffset);
    }

    public static float GetHeadHeight(this IGameObject obj)
    {
        return Marshal.PtrToStructure<float>(obj.Address + CursorHeightOffset) - 0.2f;
    }

    public static bool GetIsPlayerCharacter(this IGameObject obj) {
        return obj.ObjectKind == ObjectKind.Player;
    }

    public static bool GetIsBattleNPC(this IGameObject obj) {
        return obj.ObjectKind == ObjectKind.BattleNpc;
    }

    public static bool GetIsBattleChara(this IGameObject obj) {
        return obj is IBattleChara;
    }

    public static IPlayerCharacter GetPlayerCharacter(this IGameObject obj) {
        return obj as IPlayerCharacter;
    }

    public static unsafe CSGameObject* GetClientStructGameObject(this IGameObject obj)
    {
        return (CSGameObject*)obj.Address;
    }

    public static unsafe TargetSettings GetTargetSettings(this IGameObject obj) {
        TargetSettings settings = new TargetSettings();
        settings.Flags = TargetFlags.任意;

        if (Service.ClientState.LocalPlayer != null) {
            if (obj.EntityId == Service.ClientState.LocalPlayer.EntityId) {
                settings.Flags |= TargetFlags.自身;
            }
        }

        if (obj.GetIsPlayerCharacter()) {
            GroupManager* gm = GroupManager.Instance();
            settings.Flags |= TargetFlags.玩家;
            foreach (PartyMember member in gm->MainGroup.PartyMembers) {
                if (member.EntityId == obj.EntityId) {
                    settings.Flags |= TargetFlags.小队;
                }
            }

            if ((gm->MainGroup.AllianceFlags & 1) != 0 && (settings.Flags & TargetFlags.小队) != 0) {
                foreach (PartyMember member in gm->MainGroup.AllianceMembers) {
                    if (member.EntityId == obj.EntityId) {
                        settings.Flags |= TargetFlags.团队;
                    }
                }
            }

            ClassJob ID = (ClassJob)obj.GetPlayerCharacter().ClassJob.RowId;
            settings.Jobs = ClassJobToBit(ID);
            if (DPSJobs.Contains(ID)) {
                settings.Flags |= TargetFlags.DPS;
                if (MeleeDPSJobs.Contains(ID)) {
                    settings.Flags |= TargetFlags.近战;
                }
                else if (PhysicalRangedDPSJobs.Contains(ID)) {
                    settings.Flags |= TargetFlags.远敏;
                }
                else if (MagicalRangedDPSJobs.Contains(ID)) {
                    settings.Flags |= TargetFlags.法系;
                }
            }
            else if (HealerJobs.Contains(ID)) {
                settings.Flags |= TargetFlags.治疗;
                if (PureHealerJobs.Contains(ID)) {
                    settings.Flags |= TargetFlags.纯治疗;
                }
                else if (ShieldHealerJobs.Contains(ID)) {
                    settings.Flags |= TargetFlags.护盾治疗;
                }
            }
            else if (TankJobs.Contains(ID)) {
                settings.Flags |= TargetFlags.坦克;
            }
            else if (CrafterGathererJobs.Contains(ID)) {
                settings.Flags |= TargetFlags.生产采集;
            }
        }
        else if (obj.GetIsBattleNPC()) {
            settings.Flags |= TargetFlags.敌人;
        }
        else {
            settings.Flags |= TargetFlags.NPC;
        }

        return settings;
    }
}

