using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Toolsmith.Utils {
    public static class ItemStackExtensions {
        //Woe all ye who enter here... This is a lot of basically helper functions/extensions to just kinda encapsulate the same Attribute get/set to avoid accidentally typo-ing any of the tags.
        //I kinda hope it might be possible to clean this up some but, wew. A problem for another time if inspiration strikes - Using pretty much all of them fairly regularly so... Maybe this IS good as is?
        //But guh it makes me dizzy a bit scrolling through it all and scanning over it.

        // -- ItemStack Extensions for the Tool items themselves --
        //Tool Head on full Tool ItemStack cluster
        internal static ItemStack? GetToolhead(this ItemStack itemStack) {
            return itemStack.Attributes?.GetItemstack("tinkeredToolHead");
        }

        internal static void SetToolhead(this ItemStack itemStack, ItemStack toolhead) {
            itemStack.Attributes.SetItemstack("tinkeredToolHead", toolhead);
        }

        internal static int GetToolheadCurrentDurability(this ItemStack itemStack) { //If ANY of these part durability return 0, assume they are fresh new parts and full durability.
            return itemStack.Attributes.GetInt("tinkeredToolHeadDurability", itemStack.GetToolheadMaxDurability());
        }

        internal static void SetToolheadCurrentDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt("tinkeredToolHeadDurability", dur);
        }

        internal static int GetToolheadMaxDurability(this ItemStack itemStack) {
            return itemStack.Attributes.GetInt("tinkeredToolHeadMaxDurability");
        }

        internal static void SetToolheadMaxDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt("tinkeredToolHeadMaxDurability", dur);
        }

        //Tool Handle on full Tool ItemStack cluster
        internal static ItemStack? GetToolhandle(this ItemStack itemStack) {
            return itemStack.Attributes?.GetItemstack("tinkeredToolHandle");
        }

        internal static void SetToolhandle(this ItemStack itemStack, ItemStack toolhandle) {
            itemStack.Attributes.SetItemstack("tinkeredToolHandle", toolhandle);
        }

        internal static int GetToolhandleCurrentDurability(this ItemStack itemStack) {
            return itemStack.Attributes.GetInt("tinkeredToolHandleDurability", itemStack.GetToolhandleMaxDurability());
        }

        internal static void SetToolhandleCurrentDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt("tinkeredToolHandleDurability", dur);
        }

        internal static int GetToolhandleMaxDurability(this ItemStack itemStack) {
            return itemStack.Attributes.GetInt("tinkeredToolHandleMaxDurability");
        }

        internal static void SetToolhandleMaxDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt("tinkeredToolHandleMaxDurability", dur);
        }

        //Tool Binding on full Tool ItemStack cluster
        internal static ItemStack? GetToolbinding(this ItemStack itemStack) {
            return itemStack.Attributes?.GetItemstack("tinkeredToolBinding");
        }

        internal static void SetToolbinding(this ItemStack itemStack, ItemStack binding) {
            itemStack.Attributes.SetItemstack("tinkeredToolBinding", binding);
        }

        internal static int GetToolbindingCurrentDurability(this ItemStack itemStack) {
            return itemStack.Attributes.GetInt("tinkeredToolBindingDurability", itemStack.GetToolbindingMaxDurability());
        }

        internal static void SetToolbindingCurrentDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt("tinkeredToolBindingDurability", dur);
        }

        internal static int GetToolbindingMaxDurability(this ItemStack itemStack) {
            return itemStack.Attributes.GetInt("tinkeredToolBindingMaxDurability");
        }

        internal static void SetToolbindingMaxDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt("tinkeredToolBindingMaxDurability", dur);
        }

        //Other Full Tool stats inhereted from the parts, set on crafting generally
        internal static float GetGripChanceToDamage(this ItemStack itemStack) {
            return itemStack.Attributes.GetFloat("gripChanceToDamage", 1.0f);
        }

        internal static void SetGripChanceToDamage(this ItemStack itemStack, float chance) {
            itemStack.Attributes.SetFloat("gripChanceToDamage", chance);
        }

        internal static float GetSpeedBonus(this ItemStack itemStack) {
            return itemStack.Attributes.GetFloat("speedBonus");
        }

        internal static void SetSpeedBonus(this ItemStack itemStack, float speed) {
            itemStack.Attributes.SetFloat("speedBonus", speed);
        }

        //Extensions to handle resetting invalid tools that are lacking any durability values

        internal static void ResetNullHead(this ItemStack itemStack, IWorldAccessor world) {
            //Figure out the Tool Head and add the missing stats and ItemStack!
            string toolCode = null;
            foreach (var t in RecipeRegisterModSystem.TinkerToolGridRecipes) {
                if (t.Value.Code.Equals(itemStack.Collectible.Code)) {
                    toolCode = t.Key;
                    break;
                }
            }
            var headStack = new ItemStack(world.GetItem(new AssetLocation(toolCode)), 1);
            var headDur = ((int)itemStack.Attributes.GetDecimal("durability", itemStack.Collectible.Durability)) * 5; //If the tool has already been used some, this hopefully should reset it to have the head-damage be the existing durability, but generate new binding and handle stats.
            var headMaxDur = itemStack.Collectible.Durability * 5;

            headStack.SetMaxPartDurability(headMaxDur);
            headStack.SetCurrentPartDurability(headDur);
            itemStack.SetToolhead(headStack);
            itemStack.SetToolheadCurrentDurability(headDur);
            itemStack.SetToolheadMaxDurability(headMaxDur);
        }

        internal static void ResetNullHandleOrBinding(this ItemStack itemStack, IWorldAccessor world) {
            ItemStack handle = null;
            int maxHandleDur = itemStack.GetToolhandleMaxDurability();
            HandleWithStats handleWithStats = null;

            ItemStack binding = null;
            int maxBindingDur = itemStack.GetToolbindingMaxDurability();
            BindingWithStats bindingWithStats = null;

            if (maxHandleDur == 0) { //Just need to grab the basic Stick if there is no durability already set.
                handle = new ItemStack(world.GetItem(new AssetLocation("game:stick")), 1);
                handleWithStats = ToolsmithModSystem.Config.ToolHandlesWithStats.Get("stick");
            } else {
                handle = itemStack.GetToolhandle();
                handleWithStats = ToolsmithModSystem.Config.ToolHandlesWithStats.Get(handle.Collectible.Code.Path);
            }
            BindingStats bindingStats;
            if (maxBindingDur == 0) { //Since 'None' is a valid binding, it still needs setting to that, and given the default durability levels! No Itemstack needed though.
                bindingStats = ToolsmithModSystem.Stats.bindings.Get("none");
            } else {
                binding = itemStack.GetToolbinding();
                bindingWithStats = ToolsmithModSystem.Config.BindingsWithStats.Get(binding.Collectible.Code.Path);
                bindingStats = ToolsmithModSystem.Stats.bindings.Get(bindingWithStats.bindingStats);
            }

            var baseDur = itemStack.Collectible.Durability;
            var handleStats = ToolsmithModSystem.Stats.handles.Get(handleWithStats.handleStats);
            var gripStats = ToolsmithModSystem.Stats.grips.Get(handleWithStats.gripStats);
            var treatmentStats = ToolsmithModSystem.Stats.treatments.Get(handleWithStats.treatmentStats);

            var handleDur = baseDur * handleStats.baseHPfactor; //Starting with the handle: Account for baseHPfactor first in the handle...
            handleDur = handleDur + (handleDur * handleStats.selfHPBonus); //plus the selfDurabilityBonus
            handleDur = handleDur + (handleDur * treatmentStats.handleHPbonus); //Then any treatment bonus
            handleDur = handleDur + (handleDur * bindingStats.handleHPBonus); //Finally the Binding bonus, and all this should be multiplicitive, cause why not haha

            var bindingDur = baseDur * bindingStats.baseHPfactor; //Now for the binding, but this has fewer parts.
            bindingDur = bindingDur + (bindingDur * bindingStats.selfHPBonus);
            bindingDur = bindingDur + (bindingDur * handleStats.bindingHPBonus);

            if (maxHandleDur == 0) {
                itemStack.SetToolhandle(handle);
                itemStack.SetToolhandleCurrentDurability((int)handleDur);
                itemStack.SetToolhandleMaxDurability((int)handleDur);
            }
            if (maxBindingDur == 0) {
                if (binding != null) {
                    itemStack.SetToolbinding(binding);
                }
                itemStack.SetToolbindingCurrentDurability((int)bindingDur);
                itemStack.SetToolbindingMaxDurability((int)bindingDur);
            }

            var speedBonus = handleStats.speedBonus + gripStats.speedBonus;
            itemStack.SetSpeedBonus(speedBonus);
            itemStack.SetGripChanceToDamage(gripStats.chanceToDamage);
        }

        // -- ItemStack Extensions for the Part items --
        internal static void SetCurrentPartDurability(this ItemStack itemStack, int durability) {
            itemStack.Attributes.SetInt("toolPartCurrentDurability", durability);
        }

        internal static int GetCurrentPartDurability(this ItemStack itemStack) {
            return itemStack.Attributes.GetInt("toolPartCurrentDurability", itemStack.GetMaxPartDurability());
        }

        internal static void SetMaxPartDurability(this ItemStack itemStack, int durability) {
            itemStack.Attributes.SetInt("toolPartMaxDurability", durability);
        }

        internal static int GetMaxPartDurability(this ItemStack itemStack) {
            return itemStack.Attributes.GetInt("toolPartMaxDurability");
        }

        internal static float GetPartRemainingHPPercent(this ItemStack itemStack) { //Since by design of wanting to let handles and bindings scale to the Tool's vanilla HP values, this is needed to 'hold' over the damage it sustained during use if it survived.
            var currentDur = itemStack.GetCurrentPartDurability();
            var maxDur = itemStack.GetMaxPartDurability();
            if (currentDur > 0 && maxDur > 0) {
                return (float)currentDur / maxDur;
            }
            return 0.0f;
        }

        // -- More Generic ItemStack extensions or helper methods intended to handle items/collectibleobjects --
        public static void AddBehavior<T>(this CollectibleObject collectibleObject) where T : CollectibleBehavior {
            var existingBehavior = collectibleObject.CollectibleBehaviors.FirstOrDefault(b => b.GetType() == typeof(T));
            collectibleObject.CollectibleBehaviors.Remove(existingBehavior);
            var addedBehavior = (T)Activator.CreateInstance(typeof(T), collectibleObject);
            collectibleObject.CollectibleBehaviors = collectibleObject.CollectibleBehaviors.Append(addedBehavior);
        }

        //Checks if a given CollectableObject is made of metal
        public static bool IsCraftableMetal(this CollectibleObject collectibleObject, ICoreAPI api = null) {
            return collectibleObject.GetMetalItem(api) != null;
        }

        //Will return null if it is not a metal material!
        public static string GetMetalItem(this CollectibleObject collectibleObject, ICoreAPI api = null) {
            api ??= ToolsmithModSystem.Api;
            var ingotItem = api?.World.GetItem(new AssetLocation("game:ingot-" + collectibleObject.GetMetalMaterial()));
            return ingotItem?.Variant["metal"] ?? ingotItem?.Variant["material"];
        }

        //Will return null if the given CollectibleObject does not have a 'metal' or 'material' variant typing! 
        public static string GetMetalMaterial(this CollectibleObject collectibleObject) {
            return collectibleObject.Variant["metal"] ?? collectibleObject.Variant["material"];
        }
    }
}
