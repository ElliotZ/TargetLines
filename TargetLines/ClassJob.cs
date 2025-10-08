using System.Collections.Generic;

namespace TargetLines; 
public static class ClassJobHelper {
    public enum TargetFlags : int {
        /* 0x0001 */ 任意 = (1 << 0),        // any entity
        /* 0x0002 */ 玩家 = (1 << 1),     // any Player character
        /* 0x0004 */ 敌人 = (1 << 2),      // any enemy
        /* 0x0008 */ NPC = (1 << 3),        // any npc
        /* 0x0010 */ 团队 = (1 << 4),   // any Alliance member (Excluding party)
        /* 0x0020 */ 小队 = (1 << 5),      // any Party member
        /* 0x0040 */ 自身 = (1 << 6),       // local player
        /* 0x0080 */ DPS = (1 << 7),        // include DPS
        /* 0x0100 */ 治疗 = (1 << 8),     // include Healer
        /* 0x0200 */ 坦克 = (1 << 9),       // include Tank
        /* 0x0400 */ 生产采集 = (1 << 10),   // include Crafter/Gatherers
        /* 0x0800 */ 近战 = (1 << 11),          // include Melee DPS
        /* 0x1000 */ 远敏 = (1 << 12), // include Physical Ranged DPS
        /* 0x2000 */ 法系 = (1 << 13),  // include Magical Ranged DPS
        /* 0x4000 */ 纯治疗 = (1 << 14),        // include Pure Healer
        /* 0x8000 */ 护盾治疗 = (1 << 15),      // include Shield Healer
    }

    public static string[] TargetFlagDescriptions = {
        "无条件绘制",
        "当一名玩家角色是目标时绘制",
        "当一名敌人是目标时绘制",
        "当一名NPC是目标时绘制",
        "当团队成员是目标是绘制",
        "当小队成员是目标时绘制",
        "当你是目标是时绘制",
        "当目标角色职能为DPS时绘制",
        "当目标角色职能为治疗时绘制",
        "当目标角色职能为坦克时绘制",
        "当目标角色职能为生产采集时绘制",
        "当目标角色职能为近战DPS时绘制（选择后DPS职业选项无效）",
        "当目标角色职能为远敏DPS时绘制（选择后DPS职业选项无效）",
        "当目标角色职能为法系DPS时绘制（选择后DPS职业选项无效）",
        "当目标角色职能为纯治疗时绘制（选择后治疗职业选项无效）",
        "当目标角色职能为护盾治疗时绘制（选择后治疗职业选项无效）",
    };


    public enum ClassJob : uint {
        /*  0 */ 冒险者 = 0,
        /*  1 */ 剑术师,
        /*  2 */ 格斗家,
        /*  3 */ 斧术师,
        /*  4 */ 枪术师,
        /*  5 */ 弓箭手, 
        /*  6 */ 幻术师,
        /*  7 */ 咒术师,
        /*  8 */ 刻木匠,
        /*  9 */ 锻铁匠,
        /* 10 */ 铸甲匠,
        /* 11 */ 雕金匠,
        /* 12 */ 制革匠,
        /* 13 */ 裁衣匠,
        /* 14 */ 炼金术士,
        /* 15 */ 烹调师,
        /* 16 */ 采矿工,
        /* 17 */ 园艺工,
        /* 18 */ 捕鱼人,
        /* 19 */ 骑士,
        /* 20 */ 武僧,
        /* 21 */ 战士,
        /* 22 */ 龙骑士,
        /* 23 */ 诗人,
        /* 24 */ 白魔法师,
        /* 25 */ 黑魔法师,
        /* 26 */ 秘术师,
        /* 27 */ 召唤师,
        /* 28 */ 学者,
        /* 29 */ 双剑师,
        /* 30 */ 忍者,
        /* 31 */ 机工士,
        /* 32 */ 暗黑骑士,
        /* 33 */ 占星术士,
        /* 34 */ 武士,
        /* 35 */ 赤魔法师,
        /* 36 */ 青魔法师,
        /* 37 */ 绝枪战士,
        /* 38 */ 舞者,
        /* 39 */ 钐镰客,
        /* 40 */ 贤者,
        /* 41 */ 蝰蛇剑士,
        /* 42 */ 绘灵法师,
        Count
    };

    public static ulong ClassJobToBit(ClassJob id) {
        if (id >= ClassJob.Count || id < 0) {
            return 0;
        }

        return (ulong)(1UL << (int)id);
    }

    public static ulong ClassJobToBit(int id) {
        if (id >= (int)ClassJob.Count || id < 0) {
            return 0;
        }

        return (ulong)(1UL << id);
    }

    public static List<ClassJob> DPSJobs = new List<ClassJob> {
        ClassJob.冒险者, ClassJob.格斗家, ClassJob.枪术师, ClassJob.弓箭手,
        ClassJob.咒术师, ClassJob.武僧, ClassJob.龙骑士, ClassJob.诗人,
        ClassJob.黑魔法师, ClassJob.秘术师, ClassJob.召唤师, ClassJob.双剑师,
        ClassJob.忍者, ClassJob.机工士, ClassJob.武士, ClassJob.赤魔法师,
        ClassJob.青魔法师, ClassJob.舞者, ClassJob.钐镰客, ClassJob.蝰蛇剑士, ClassJob.绘灵法师
    };

    public static List<ClassJob> HealerJobs = new List<ClassJob> {
        ClassJob.幻术师, ClassJob.白魔法师, ClassJob.学者, ClassJob.占星术士,
        ClassJob.贤者
    };

    public static List<ClassJob> TankJobs = new List<ClassJob> {
        ClassJob.剑术师, ClassJob.斧术师, ClassJob.骑士, ClassJob.战士,
        ClassJob.暗黑骑士, ClassJob.绝枪战士
    };

    public static List<ClassJob> CrafterGathererJobs = new List<ClassJob> {
        ClassJob.刻木匠, ClassJob.锻铁匠, ClassJob.铸甲匠, ClassJob.雕金匠,
        ClassJob.制革匠, ClassJob.裁衣匠, ClassJob.炼金术士, ClassJob.烹调师,
        ClassJob.采矿工, ClassJob.园艺工, ClassJob.捕鱼人
    };

    public static List<ClassJob> MeleeDPSJobs = new List<ClassJob> {
        ClassJob.武僧, ClassJob.龙骑士, ClassJob.忍者, ClassJob.武士,
        ClassJob.钐镰客, ClassJob.格斗家, ClassJob.枪术师, ClassJob.双剑师,
        ClassJob.冒险者, ClassJob.蝰蛇剑士
    };

    public static List<ClassJob> PhysicalRangedDPSJobs = new List<ClassJob> {
        ClassJob.诗人, ClassJob.机工士, ClassJob.舞者, ClassJob.弓箭手
    };

    public static List<ClassJob> MagicalRangedDPSJobs = new List<ClassJob> {
        ClassJob.黑魔法师, ClassJob.召唤师, ClassJob.赤魔法师, ClassJob.青魔法师,
        ClassJob.咒术师, ClassJob.秘术师, ClassJob.绘灵法师
    };

    public static List<ClassJob> PureHealerJobs = new List<ClassJob> {
        ClassJob.白魔法师, ClassJob.占星术士, ClassJob.幻术师
    };

    public static List<ClassJob> ShieldHealerJobs = new List<ClassJob> {
        ClassJob.学者, ClassJob.贤者
    };

    public static bool CompareTargetSettings(ref TargetSettings goal, ref TargetSettings entity) {
        TargetFlags gflags = goal.Flags;
        TargetFlags eflags = entity.Flags;
        bool ret = false;

        // entity does not matter if this rule accepts any entity
        if ((gflags & TargetFlags.任意) != 0) {
            return true;
        }

        // entity must be Player, check if their job is on the job list, if the job list has any entries
        if (goal.Jobs != 0 && (eflags & TargetFlags.玩家) != 0) {
            bool invalid_job = true;
            for (int index = 0; index < (int)ClassJob.Count; index++) {
                ulong jobflag = ClassJobToBit(index);
                if ((goal.Jobs & jobflag) != 0 && (entity.Jobs & jobflag) != 0) {
                    invalid_job = false;
                    break;
                }
            }

            if (invalid_job) {
                return false;
            }
        }

        // nullify role flags when specific roles are selected
        if ((gflags & TargetFlags.DPS) != 0) {
            if ((gflags & TargetFlags.近战) != 0 || (gflags & TargetFlags.远敏) != 0 || (gflags & TargetFlags.法系) != 0) {
                gflags &= ~TargetFlags.DPS;
            }
        }

        if ((gflags & TargetFlags.治疗) != 0) {
            if ((gflags & TargetFlags.纯治疗) != 0 || (gflags & TargetFlags.护盾治疗) != 0) {
                gflags &= ~TargetFlags.治疗;
            }
        }

        // check if any other flags are true
        for (int index = 1; index < 16; index++) {
            bool gbit = ((int)gflags & (1 << index)) != 0;
            bool ebit = ((int)eflags & (1 << index)) != 0;

            if (gbit && ebit) {
                ret = true;
                break;
            }
        }

        return ret;
    }
}
