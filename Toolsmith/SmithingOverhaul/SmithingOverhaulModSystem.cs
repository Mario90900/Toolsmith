using HarmonyLib;
using System.Collections.Generic;
using Toolsmith.SmithingOverhaul.Config;
using Toolsmith.SmithingOverhaul.Property;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Toolsmith.SmithingOverhaul
{
    public class SmithingOverhaulModSystem : ModSystem
    {
        public static ICoreAPI Api;
        public static Harmony HarmonyObject;
        public static ILogger Logger;
        public static string ModId;
        public static string ModVersion;
        public static SmithingOverhaulConfig Config;

        public Dictionary<string, SmithingPropertyVariant> metalPropsByCode;

        public const string AnvilPatches = "anvilPatches";
        public const string ItemWorkItemPatches = "workItemPatches";
        public const string WorkItemStatsPatches = "workItemStatsPatches";
        public override void StartPre(ICoreAPI api)
        {
            Logger = Mod.Logger;
            ModId = Mod.Info.ModID;
            ModVersion = Mod.Info.Version;
            Api = api;
        }
        public override void Start(ICoreAPI api)
        {
            HarmonyPatch();
        }
        private static void HarmonyPatch()
        {
            if (HarmonyObject != null)
            {
                return;
            }
            HarmonyObject = new Harmony(ModId);
            Logger.VerboseDebug("Harmony is starting Patches!");
            HarmonyObject.PatchCategory(AnvilPatches);
            HarmonyObject.PatchCategory(ItemWorkItemPatches);
            Logger.VerboseDebug("Patched functions for Smithing purposes.");
        }
        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);
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

        public override void Dispose()
        {
            base.Dispose();
        }

        public override double ExecuteOrder()
        {
            return base.ExecuteOrder();
        }

        public override bool ShouldLoad(ICoreAPI api)
        {
            return base.ShouldLoad(api);
        }

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return base.ShouldLoad(forSide);
        }

        

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
        }
    }
}
