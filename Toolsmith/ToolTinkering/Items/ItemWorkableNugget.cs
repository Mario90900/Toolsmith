using SmithingPlus.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.ToolTinkering.Drawbacks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace Toolsmith.ToolTinkering.Items {

    public class ItemWorkableNugget : ItemNugget, IAnvilWorkable {

        public int VoxelCountForHandbook(ItemStack stack) => 2;

        public bool CanWork(ItemStack stack) {
            float temp = stack.Collectible.GetTemperature(api.World, stack);
            float meltingPoint = stack.Collectible.GetMeltingPoint(api.World, null, new DummySlot(stack));

            if (stack.Collectible.Attributes?["workableTemperature"].Exists == true) {
                return stack.Collectible.Attributes["workableTemperature"].AsFloat(meltingPoint / 2) <= temp;
            }

            return temp >= meltingPoint / 2;
        }

        public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack) {
            string metalcode = GetMetalType();
            if (metalcode == null) {
                return Array.Empty<SmithingRecipe>().ToList();
            }
            ItemStack ingot = new ItemStack(api.World.GetItem(new AssetLocation("ingot-" + metalcode)));

            return api.GetSmithingRecipes()
                .Where(r => r.Ingredient.SatisfiesAsIngredient(ingot))
                .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code)
                .ToList();
        }

        public int GetRequiredAnvilTier(ItemStack stack) {
            string metalcode = GetMetalType();
            if (metalcode == null) {
                return 10;
            }
            int tier = 0;

            MetalPropertyVariant var;
            if (api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(metalcode, out var)) {
                tier = var.Tier - 1;
            }

            if (stack.Collectible.Attributes?["requiresAnvilTier"].Exists == true) {
                tier = stack.Collectible.Attributes["requiresAnvilTier"].AsInt(tier);
            }

            return tier;
        }

        public ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil) {
            if (!CanWork(stack) || beAnvil.WorkItemStack == null || (beAnvil.WorkItemStack != null && !beAnvil.CanWorkCurrent)) {
                return null;
            }

            string metalcode = GetMetalType();
            if (metalcode == null) {
                return null;
            }

            var workItemToAdd = api.World.GetItem(new AssetLocation("workitem-" + metalcode));
            if (workItemToAdd == null) {
                return null;
            }
            var toAddItemStack = new ItemStack(workItemToAdd);
            toAddItemStack.Collectible.SetTemperature(api.World, toAddItemStack, stack.Collectible.GetTemperature(api.World, stack));

            if (!string.Equals(beAnvil.WorkItemStack.Collectible.Variant["metal"], stack.Collectible.Variant["metal"])) {
                if (api.Side == EnumAppSide.Client) {
                    (api as ICoreClientAPI).TriggerIngameError(this, "notequal", Lang.Get("Must be the same metal to add voxels"));
                }
                return null;
            }

            if (AddVoxels(api, ref beAnvil.Voxels) == 0) {
                if (api.Side == EnumAppSide.Client) {
                    (api as ICoreClientAPI).TriggerIngameError(this, "requireshammering", Lang.Get("Try hammering down before adding additional voxels"));
                }
                return null;
            }

            return toAddItemStack;
        }

        public ItemStack GetBaseMaterial(ItemStack stack) {
            return stack;
        }

        public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil) {
            return EnumHelveWorkableMode.NotWorkable;
        }

        public static int AddVoxels(ICoreAPI api, ref byte[,,] voxels) {
            int voxelsAdded = 0;
            int voxelsToAdd = 2;
            byte[,,] voxelsCopy = ReforgingUtility.GetVoxelCopyFromByteVoxels(voxels);

            if (api.World.Rand.NextDouble() <= ToolsmithModSystem.Config.ExtraBitVoxelChance) {
                voxelsToAdd += 1;
            }

            for (int y = 0; y < 6; y++) {
                if (voxelsAdded < voxelsToAdd && voxelsCopy[8, y, 7] == 0) {
                    voxelsCopy[8, y, 7] = 1;
                    voxelsAdded++;
                }

                if (voxelsAdded < voxelsToAdd && voxelsCopy[8, y, 8] == 0) {
                    voxelsCopy[8, y, 8] = 1;
                    voxelsAdded++;
                }

                if (voxelsAdded >= voxelsToAdd) {
                    break;
                }
            }

            if (voxelsAdded >= voxelsToAdd) {
                voxels = voxelsCopy;
                return voxelsAdded;
            }

            return 0;
        }

        public string GetMetalType() {
            var smeltedCode = CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible.LastCodePart();
            if (smeltedCode == null) {
                ToolsmithModSystem.Logger.Error("Something is being given the Workable Nugget Class but has no combustible props. This will cause the item to not function like a proper workable item. The item in question is: " + Code);
            }
            return smeltedCode;
        }
    }
}
