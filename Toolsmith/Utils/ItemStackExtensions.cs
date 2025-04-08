using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Toolsmith.Utils {
    public static class ItemStackExtensions {
        //Woe all ye who enter here... This is a lot of basically helper functions/extensions to just kinda encapsulate the same Attribute get/set to avoid accidentally typo-ing any of the tags.
        //I kinda hope it might be possible to clean this up some but, wew. A problem for another time if inspiration strikes - Using pretty much all of them fairly regularly so... Maybe this IS good as is?
        //But guh it makes me dizzy a bit scrolling through it all and scanning over it.

        // -- ItemStack Extensions for the Tool items themselves --
        //Tool Head on full Tool ItemStack cluster
        public static ItemStack GetToolhead(this ItemStack itemStack) { //If there is no tool head returned, lets reset it in here.
            var head = itemStack.Attributes?.GetItemstack(ToolsmithAttributes.ToolHead);
            if (head == null) {
                itemStack.ResetNullHead(ToolsmithModSystem.Api.World);
                head = itemStack.Attributes.GetItemstack(ToolsmithAttributes.ToolHead);
            }
            head.ResolveBlockOrItem(ToolsmithModSystem.Api.World);
            return head.Clone();
        }

        public static void SetToolhead(this ItemStack itemStack, ItemStack toolhead) {
            itemStack.Attributes.SetItemstack(ToolsmithAttributes.ToolHead, toolhead.Clone()); //Save a clone of what was passed, maybe it's only been saving a reference each time...
        }

        public static int GetToolheadCurrentDurability(this ItemStack itemStack) { //If ANY of these part durability return -1, assume they are fresh new parts and full durability. It might be safer to just run with something obviously impossible, instead of something that will hit eventually - though should break when it does.
            return itemStack.Attributes.GetInt(ToolsmithAttributes.ToolHeadCurrentDur, itemStack.GetToolheadMaxDurability());
        }

        public static void SetToolheadCurrentDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt(ToolsmithAttributes.ToolHeadCurrentDur, dur);
        }

        public static int GetToolheadMaxDurability(this ItemStack itemStack) {
            var maxDur = itemStack.Attributes.GetInt(ToolsmithAttributes.ToolHeadMaxDur, -1);

            if (maxDur < 0) {
                itemStack.ResetNullHead(ToolsmithModSystem.Api.World);
                maxDur = itemStack.Attributes.GetInt(ToolsmithAttributes.ToolHeadMaxDur);
            }

            return maxDur;
        }

        public static void SetToolheadMaxDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt(ToolsmithAttributes.ToolHeadMaxDur, dur);
        }

        //Since this can be called or used before
        public static bool HasPlaceholderHead(this ItemStack itemStack) {
            if (itemStack.GetToolhead().Collectible.Code == ToolsmithConstants.FallbackHeadCode) {
                return true;
            } else {
                return false;
            }
        }

        //Tool Handle on full Tool ItemStack cluster
        public static ItemStack GetToolhandle(this ItemStack itemStack) { //Same as the head, if there is no Handle, lets reset it in here. Hopefully this makes it stronger.
            var handle = itemStack.Attributes?.GetItemstack(ToolsmithAttributes.ToolHandle);
            if (handle == null) {
                itemStack.ResetNullHandleOrBinding(ToolsmithModSystem.Api.World);
                handle = itemStack.Attributes.GetItemstack(ToolsmithAttributes.ToolHandle);
            }
            handle.ResolveBlockOrItem(ToolsmithModSystem.Api.World);
            return handle.Clone();
        }

        public static void SetToolhandle(this ItemStack itemStack, ItemStack toolhandle) {
            itemStack.Attributes.SetItemstack(ToolsmithAttributes.ToolHandle, toolhandle.Clone());
        }

        public static int GetToolhandleCurrentDurability(this ItemStack itemStack) {
            return itemStack.Attributes.GetInt(ToolsmithAttributes.ToolHandleCurrentDur, itemStack.GetToolhandleMaxDurability());
        }

        public static void SetToolhandleCurrentDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt(ToolsmithAttributes.ToolHandleCurrentDur, dur);
        }

        public static int GetToolhandleMaxDurability(this ItemStack itemStack) {
            var maxDur = itemStack.Attributes.GetInt(ToolsmithAttributes.ToolHandleMaxDur, -1);

            if (maxDur < 0) {
                itemStack.ResetNullHandleOrBinding(ToolsmithModSystem.Api.World);
                maxDur = itemStack.Attributes.GetInt(ToolsmithAttributes.ToolHandleMaxDur);
            }

            return maxDur;
        }

        public static void SetToolhandleMaxDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt(ToolsmithAttributes.ToolHandleMaxDur, dur);
        }

        //Tool Binding on full Tool ItemStack cluster
        public static ItemStack? GetToolbinding(this ItemStack itemStack) { //A Null Tool Binding means that no binding was used, this is the only case where it is allowed.
            var binding = itemStack.Attributes?.GetItemstack(ToolsmithAttributes.ToolBinding);
            if (binding == null) {
                return null;
            }
            binding.ResolveBlockOrItem(ToolsmithModSystem.Api.World);
            return binding.Clone();
        }

        public static void SetToolbinding(this ItemStack itemStack, ItemStack binding) {
            itemStack.Attributes.SetItemstack(ToolsmithAttributes.ToolBinding, binding.Clone());
        }

        public static int GetToolbindingCurrentDurability(this ItemStack itemStack) {
            return itemStack.Attributes.GetInt(ToolsmithAttributes.ToolBindingCurrentDur, itemStack.GetToolbindingMaxDurability());
        }

        public static void SetToolbindingCurrentDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt(ToolsmithAttributes.ToolBindingCurrentDur, dur);
        }

        public static int GetToolbindingMaxDurability(this ItemStack itemStack) {
            var maxDur = itemStack.Attributes.GetInt(ToolsmithAttributes.ToolBindingMaxDur, -1);

            if (maxDur < 0) {
                itemStack.ResetNullHandleOrBinding(ToolsmithModSystem.Api.World);
                maxDur = itemStack.Attributes.GetInt(ToolsmithAttributes.ToolBindingMaxDur);
            }

            return maxDur;
        }

        public static void SetToolbindingMaxDurability(this ItemStack itemStack, int dur) {
            itemStack.Attributes.SetInt(ToolsmithAttributes.ToolBindingMaxDur, dur);
        }

        //Other Full Tool stats inhereted from the parts, set on crafting generally
        public static float GetGripChanceToDamage(this ItemStack itemStack) {
            return itemStack.Attributes.GetFloat(ToolsmithAttributes.GripChanceToDamage, 1.0f);
        }

        public static void SetGripChanceToDamage(this ItemStack itemStack, float chance) {
            itemStack.Attributes.SetFloat(ToolsmithAttributes.GripChanceToDamage, chance);
        }

        public static float GetSpeedBonus(this ItemStack itemStack) {
            return itemStack.Attributes.GetFloat(ToolsmithAttributes.SpeedBonus);
        }

        public static void SetSpeedBonus(this ItemStack itemStack, float speed) {
            itemStack.Attributes.SetFloat(ToolsmithAttributes.SpeedBonus, speed);
        }

        //Extensions to handle resetting invalid tools that are lacking any durability values

        //Since it's possible to have issues either detecting proper tools, configuration being wonky, or other things, there needs to be a default fallback to set here.
        //Any time the Tool Head is accessed, check if it's the Dummy and revert to vanilla mechanics to prevent crashing.
        public static void ResetNullHead(this ItemStack itemStack, IWorldAccessor world) {
            if (itemStack.Attributes == null) {
                itemStack.Attributes = new TreeAttribute();
            }

            //Figure out the Tool Head and add the missing stats and ItemStack!
            if (RecipeRegisterModSystem.TinkerToolGridRecipes != null) { //If this is being ran on the server-side, then RecipeRegisterModSystem will have actually booted and everything! This actually makes for a decent 'Am I client or server' check when not provided the world or api!
                string headCode = null;
                foreach (var t in RecipeRegisterModSystem.TinkerToolGridRecipes) {
                    if (t.Value.Code.Equals(itemStack.Collectible.Code)) {
                        headCode = t.Key;
                        break;
                    }
                }

                if (headCode == null) { //If headCode is still null at this point, it never found a proper key. Is something wrong with the configs, or is something getting improperly registered through a wildcard?
                                        //Either way, this needs an error printed and it has to be accounted for at any point.
                    ToolsmithModSystem.Logger.Error("Ran into a tool without an entry in the GridRecipes Dictionary! Something might be wrong with your configs, or something is getting improperly given the behaviors?\nThe Itemstack in question is: " + itemStack.ToString() + "\nAdding it to the Ignore list to revert to vanilla behaviors when encountered again, and assigning placeholder values to hopefully prevent this from being run again. If you get a Candle when this breaks somehow, this is why!");
                    ToolsmithModSystem.IgnoreCodes.Add(itemStack.Collectible.Code.ToString());
                    var headStackBackup = new ItemStack(world.GetItem(new AssetLocation(ToolsmithConstants.FallbackHeadCode)), 1); //Placeholder Candle! Wow! It'll be something so it actually _have_ something in there. No more nulls.
                    var curHeadDurBackup = 1; //Set these to just 1 so that something is set. Since this code should be ignored everywhere, this might help prevent re-checking it as well.
                    var maxHeadDurBackup = 1;
                    itemStack.SetToolhead(headStackBackup);
                    itemStack.SetToolheadCurrentDurability(curHeadDurBackup);
                    itemStack.SetToolheadMaxDurability(maxHeadDurBackup);
                    return;
                }

                var headStack = new ItemStack(world.GetItem(new AssetLocation(headCode)), 1);
                var headDur = (int)(itemStack.Attributes.GetDecimal(ToolsmithAttributes.Durability, itemStack.Collectible.GetBaseMaxDurability(itemStack)) * ToolsmithModSystem.Config.HeadDurabilityMult); //If the tool has already been used some, this hopefully should reset it to have the head-damage be the existing durability, but generate new binding and handle stats.
                var headMaxDur = (int)(itemStack.Collectible.GetBaseMaxDurability(itemStack) * ToolsmithModSystem.Config.HeadDurabilityMult);

                headStack.SetCurrentPartDurability(headDur);
                headStack.SetMaxPartDurability(headMaxDur);
                itemStack.SetToolhead(headStack);
                itemStack.SetToolheadCurrentDurability(headDur);
                itemStack.SetToolheadMaxDurability(headMaxDur);
            } else {
                //Instead for part of the temp-fix, just run off the assumption that for now it might work fine to have a placeholder basic calc to initialize it based on the default Durability value.
                //Might have to figure out pinging the server for an item update on the client side here... Or make sure the Serverside always marks the slot as dirty to update it to clients. Hopefully?
                var headStack = new ItemStack(world.GetItem(new AssetLocation(ToolsmithConstants.FallbackHeadCode)), 1); //Placeholder Candle! Wow! It'll be something so it actually _have_ something in there. No more nulls.
                var curHeadDur = (int)(itemStack.Attributes.GetDecimal(ToolsmithAttributes.Durability, itemStack.Collectible.GetBaseMaxDurability(itemStack)) * ToolsmithModSystem.Config.HeadDurabilityMult); //If the tool has already been used some, this hopefully should reset it to have the head-damage be the existing durability, but generate new binding and handle stats.
                var maxHeadDur = (int)(itemStack.Collectible.GetBaseMaxDurability(itemStack) * ToolsmithModSystem.Config.HeadDurabilityMult);
                itemStack.SetToolhead(headStack);
                itemStack.SetToolheadCurrentDurability(curHeadDur);
                itemStack.SetToolheadMaxDurability(maxHeadDur);
            }
        }

        //Since the handle and binding can be expected to not have something, it makes sense to set them to Stick and 'none' respectively as defaults.
        public static void ResetNullHandleOrBinding(this ItemStack itemStack, IWorldAccessor world) {
            if (itemStack.Attributes == null) {
                itemStack.Attributes = new TreeAttribute();
            }

            ItemStack handle = null;
            int maxHandleDur = itemStack.Attributes.GetInt(ToolsmithAttributes.ToolHandleMaxDur, -1);
            HandleWithStats handleWithStats = null;

            ItemStack binding = null;
            int maxBindingDur = itemStack.Attributes.GetInt(ToolsmithAttributes.ToolBindingMaxDur, -1);
            BindingWithStats bindingWithStats = null;

            if (maxHandleDur < 0) { //Just need to grab the basic Stick if there is no durability already set.
                handle = new ItemStack(world.GetItem(new AssetLocation(ToolsmithConstants.DefaultHandleCode)), 1);
                handleWithStats = ToolsmithModSystem.Config.ToolHandlesWithStats.Get(ToolsmithConstants.DefaultHandlePartKey);
            } else {
                handle = itemStack.GetToolhandle();
                handleWithStats = ToolsmithModSystem.Config.ToolHandlesWithStats.Get(handle.Collectible.Code.Path);
            }
            BindingStats bindingStats;
            if (maxBindingDur < 0) { //Since 'None' is a valid binding, it still needs setting to that, and given the default durability levels! No Itemstack needed though.
                bindingStats = ToolsmithModSystem.Stats.bindings.Get(ToolsmithConstants.DefaultBindingPartKey);
            } else {
                binding = itemStack.GetToolbinding();
                bindingWithStats = ToolsmithModSystem.Config.BindingsWithStats.Get(binding.Collectible.Code.Path);
                bindingStats = ToolsmithModSystem.Stats.bindings.Get(bindingWithStats.bindingStats);
            }
            
            var baseDur = itemStack.Collectible.GetBaseMaxDurability(itemStack);
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

            if (maxHandleDur < 0) {
                handle.SetCurrentPartDurability((int)handleDur);
                handle.SetMaxPartDurability((int)handleDur);
                itemStack.SetToolhandle(handle);
                itemStack.SetToolhandleCurrentDurability((int)handleDur);
                itemStack.SetToolhandleMaxDurability((int)handleDur);
            }
            if (maxBindingDur < 0) {
                if (binding != null) {
                    binding.SetCurrentPartDurability((int)bindingDur);
                    binding.SetMaxPartDurability((int) bindingDur);
                    itemStack.SetToolbinding(binding);
                }
                itemStack.SetToolbindingCurrentDurability((int)bindingDur);
                itemStack.SetToolbindingMaxDurability((int)bindingDur);
            }

            var speedBonus = handleStats.speedBonus + gripStats.speedBonus;
            itemStack.SetSpeedBonus(speedBonus);
            itemStack.SetGripChanceToDamage(gripStats.chanceToDamage);
        }

        public static void SetMaxDurBypassFlag(this ItemStack itemStack) {
            itemStack.Attributes.SetBool(ToolsmithAttributes.BypassMaxCall, true); //Doesn't matter what it's set to, but for human-readable sake, it's set to true.
        }

        public static bool GetMaxDurBypassFlag(this ItemStack itemStack) {
            return itemStack.Attributes.HasAttribute(ToolsmithAttributes.BypassMaxCall); //The existance of this attribute is all that matters, that is our flag!
        }

        public static void ClearMaxDurBypassFlag(this ItemStack itemStack) {
            if (itemStack.Attributes.HasAttribute(ToolsmithAttributes.BypassMaxCall)) { //Just verify that it exists first.
                itemStack.Attributes.RemoveAttribute(ToolsmithAttributes.BypassMaxCall); //If so, just remove the flag attribute.
            }
        }

        // -- ItemStack Extensions for the Part items --
        public static void SetCurrentPartDurability(this ItemStack itemStack, int durability) {
            itemStack.Attributes.SetInt(ToolsmithAttributes.ToolPartCurrentDur, durability);
        }

        public static int GetCurrentPartDurability(this ItemStack itemStack) {
            return itemStack.Attributes.GetInt(ToolsmithAttributes.ToolPartCurrentDur, itemStack.GetMaxPartDurability());
        }

        public static void SetMaxPartDurability(this ItemStack itemStack, int durability) {
            itemStack.Attributes.SetInt(ToolsmithAttributes.ToolPartMaxDur, durability);
        }

        public static int GetMaxPartDurability(this ItemStack itemStack) {
            return itemStack.Attributes.GetInt(ToolsmithAttributes.ToolPartMaxDur, -1);
        }

        public static float GetPartRemainingHPPercent(this ItemStack itemStack) { //Since by design of wanting to let handles and bindings scale to the Tool's vanilla HP values, this is needed to 'hold' over the damage it sustained during use if it survived.
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

        //Attempt to bypass the expected Toolsmith GetMaxDurability patch because this is a KNOWN call to GetMaxDurability with the intent to recieve the result of other mods factoring in their patches or changes through this method. If there are none? It just returns the default base call anyway through the GetMaxDurability patch!
        //Since the idea was to replace all itemStack.Collectible.Durability; accesses I made with this, that means in the end it will return the same thing as long as it is called through this specific method.
        public static int GetBaseMaxDurability(this CollectibleObject collectibleObject, ItemStack itemStack) { //itemStack is the expected tool in question.
            itemStack.SetMaxDurBypassFlag(); //This should be the only place this is EVER called.
            var baseDur = (int)itemStack.Collectible.GetMaxDurability(itemStack);
            itemStack.ClearMaxDurBypassFlag(); //Make sure to clean the flag up afterwards, since we don't want to leave the flag there once it is read.
            return baseDur;
        }
    }
}
