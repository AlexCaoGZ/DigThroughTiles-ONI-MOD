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

                // 在判定天然背景墙时，无视同一格中存在的其他建筑
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
    }
}
