using HarmonyLib;
using KMod;
using System;
using UnityEngine;

namespace DigThroughTiles
{
    public class Patch
    {
        public class PinYinSearchMod : UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                base.OnLoad(harmony);
                Debug.Log("[DigThroughTiles] Dig Through Tiles Mod Loadad.");
            }
        }

        [HarmonyPatch(typeof(DigTool), "PlaceDig")]
        public class DigThroughTilesPatch
        {
            public static bool Prefix(int cell, int animationDelay, ref GameObject __result)
            {
                bool flag = DigTool.Instance.IsActiveLayer(ToolParameterMenu.FILTERLAYERS.TILES);
                bool flag2 = DigTool.Instance.IsActiveLayer(ToolParameterMenu.FILTERLAYERS.NATURALBACKWALL);
                bool flag3 = Grid.Solid[cell] && !Grid.Foundation[cell];

                // 去掉foundation和solid的判定，只检测背景墙
                bool flag4 = BackwallManager.HasBackwall(cell);

                if (Grid.Objects[cell, 7] == null && ((flag3 && flag) || (flag4 && flag2)))
                {
                    for (int i = 0; i < 45; i++)
                    {
                        if (Grid.Objects[cell, i] != null && Grid.Objects[cell, i].GetComponent<Constructable>() != null)
                        {
                            __result = null;
                            return false;
                        }
                    }
                    GameObject gameObject = Util.KInstantiate(Assets.GetPrefab(new Tag("DigPlacer")));
                    gameObject.GetComponent<Diggable>().digTypeFlags = (flag ? 1 : 0) | (flag2 ? 2 : 0);
                    gameObject.SetActive(value: true);
                    Grid.Objects[cell, 7] = gameObject;

                    Vector3 position = Grid.CellToPosCBC(cell, DigTool.Instance.visualizerLayer);
                    position.z += -0.15f;
                    gameObject.transform.SetPosition(position);
                    gameObject.GetComponentInChildren<EasingAnimations>().PlayAnimation("ScaleUp", Mathf.Max(0f, (float)animationDelay * 0.02f));

                    __result = gameObject;
                    return false;
                }
                if (Grid.Objects[cell, 7] != null)
                {
                    __result = Grid.Objects[cell, 7];
                    return false;
                }
                __result = null;
                return false;
            }
        }
        [HarmonyPatch(typeof(Diggable), "OnSolidChanged")]
        public class Diggable_OnSolidChanged_Patch
        {
            // 在 OnSolidChanged 执行前，临时把 Grid.Foundation 置为 false
            // 如果 Grid.Foundation 是true的话，Diggable 会认为这个格子是有实体方块的，不允许挖背景墙
            public static void Prefix(Diggable __instance, int ___cached_cell, out bool __state)
            {
                __state = false;
                if (__instance.WillDigBackwall()
                    && Grid.Solid[___cached_cell]
                    && Grid.Foundation[___cached_cell]
                    && BackwallManager.HasBackwall(___cached_cell))
                {
                    __state = true; // 临时修改标记
                    Grid.Foundation[___cached_cell] = false;
                }
            }
            public static void Postfix(int ___cached_cell, bool __state)
            {
                if (__state)
                {
                    Grid.Foundation[___cached_cell] = true; // 恢复标记
                }
            }
        }
    }
    [HarmonyPatch(typeof(Diggable), "OnWorkTick")]
    public class Diggable_OnWorkTick_Patch
    {
        public static bool Prefix(
            Diggable __instance,
            WorkerBase worker,
            float dt,
            ref bool __result,
            int ___cached_cell,
            ref bool ___isDigComplete)
        {
            // 拦截，挖掘格子里有玩家放置的实体方块
            if (Grid.Solid[___cached_cell] && Grid.Foundation[___cached_cell])
            {
                // 挖掘的是背景墙
                if (__instance.WillDigBackwall() && BackwallManager.HasBackwall(___cached_cell))
                {
                    // 只对背景墙造成伤害
                    float approximateDigTime = Diggable.GetApproximateDigTime(___cached_cell);
                    float amount = dt / approximateDigTime;
                    float damage = Grid.Damage[___cached_cell];
                    damage += amount;
                    Grid.Damage[___cached_cell] = Mathf.Min(1f, damage);
                    if (Grid.Damage[___cached_cell] >= 1f)
                    {
                        SimMessages.Dig(___cached_cell, -1, skipEvent: false, backwall: true);
                        Grid.Damage[___cached_cell] = 0f;
                    }
                }
                else
                {
                    // 挖的不是背景，标记任务完成
                    ___isDigComplete = true;
                }
                __result = ___isDigComplete;

                return false;
            }
            return true; // 其他情况走原版逻辑
        }
    }
}
