using HarmonyLib;
using SmithingOverhaul.Config;
using SmithingOverhaul.Item;
using SmithingOverhaul.Property;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Toolsmith;

namespace SmithingOverhaul
{
    public class SmithingOverhaulModSystem : ModSystem
    {
        public static SmithingOverhaulConfig Config;
        public static float VoxelsPerBit;

        public Dictionary<string, SmithingPropertyVariant> metalPropsByCode;

        public const string AnvilPatches = "anvilPatches";
        public const string ItemWorkItemPatches = "workItemPatches";
        public const string WorkItemStatsPatches = "workItemStatsPatches";

        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass($"{ToolsmithModSystem.ModId}:SmithingWorkItem", typeof(SmithingWorkItem));
            HarmonyPatch();
        }
        private static void HarmonyPatch()
        {
            ToolsmithModSystem.HarmonyInstance.PatchCategory(AnvilPatches);
            ToolsmithModSystem.HarmonyInstance.PatchCategory(ItemWorkItemPatches);
            ToolsmithModSystem.HarmonyInstance.PatchCategory(WorkItemStatsPatches);
            ToolsmithModSystem.Logger.VerboseDebug("Patched functions for Smithing purposes.");
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            base.AssetsLoaded(api);

            var metalAssets = api.Assets.GetMany<SmithingProperty>(api.Logger, "worldproperties/block/metal.json");
            foreach (var metals in metalAssets.Values)
            {
                for (int i = 0; i < metals.Variants.Length; i++)
                {
                    // Metals currently don't have a domain
                    var metal = metals.Variants[i];
                    metalPropsByCode[metal.Code.Path] = metal;
                }
            }
        }
    }
}
