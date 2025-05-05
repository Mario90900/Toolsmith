using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Config;
using Toolsmith.Utils;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.ServerMods.NoObf;
using Toolsmith.ToolTinkering.Items;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.Client;

namespace Toolsmith.ToolTinkering {
    //This is beginning to hold the MEAT of the whole tinkering system. It has various helper functions that are being used in multiple places to help keep everything just running the single code calls and ensuring it isn't spaghetti while I add more ways to do the same things.
    public static class TinkeringUtility {

        static int[] sharpnessColors = new int[11] {
            ColorUtil.Hex2Int("#7e0279"),
            ColorUtil.Hex2Int("#7a3299"),
            ColorUtil.Hex2Int("#6f4eb6"),
            ColorUtil.Hex2Int("#5e67ce"),
            ColorUtil.Hex2Int("#457fe2"),
            ColorUtil.Hex2Int("#1995f0"),
            ColorUtil.Hex2Int("#00abf8"),
            ColorUtil.Hex2Int("#00bffd"),
            ColorUtil.Hex2Int("#00d3fd"),
            ColorUtil.Hex2Int("#00e6fb"),
            ColorUtil.Hex2Int("#43f8f8")
        };
        static int[] unpleasantGradient = new int[11] { //It's very unpleasant.
            ColorUtil.Hex2Int("#9e5400"),
            ColorUtil.Hex2Int("#c34523"),
            ColorUtil.Hex2Int("#e5274a"),
            ColorUtil.Hex2Int("#fe007c"),
            ColorUtil.Hex2Int("#ff00b8"),
            ColorUtil.Hex2Int("#fa35fd"),
            ColorUtil.Hex2Int("#ff5fa3"),
            ColorUtil.Hex2Int("#ff746a"),
            ColorUtil.Hex2Int("#fb8e00"),
            ColorUtil.Hex2Int("#aebb00"),
            ColorUtil.Hex2Int("#23d726")
        };
        static int[] monhunGradiant = new int[11] {
            ColorUtil.Hex2Int("#ff0f00"),
            ColorUtil.Hex2Int("#ff4b00"),
            ColorUtil.Hex2Int("#ff6b00"),
            ColorUtil.Hex2Int("#ffb300"),
            ColorUtil.Hex2Int("#fff700"),
            ColorUtil.Hex2Int("#b4fd00"),
            ColorUtil.Hex2Int("#24ff00"),
            ColorUtil.Hex2Int("#009aab"),
            ColorUtil.Hex2Int("#0000FF"),
            ColorUtil.Hex2Int("#8080ff"),
            ColorUtil.Hex2Int("#ffffff")
        };
        static int[] SharpnessColorGradient;

        //This just sets the color gradiant for the Sharpness Bar. Only run this on the Client. This bit was copied over from GuiStyle vanilla code! And the hex values adjusted.
        public static void InitializeSharpnessColorGradient() {
            var gradiantChoice = SelectGradiantForSharpness();

            SharpnessColorGradient = new int[100];
            for (int i = 0; i < 10; i++) {
                for (int j = 0; j < 10; j++) {
                    SharpnessColorGradient[10 * i + j] = ColorUtil.ColorOverlay(gradiantChoice[i], gradiantChoice[i + 1], (float)j / 10f);
                }
            }
        }

        private static int[] SelectGradiantForSharpness() {
            switch (ToolsmithModSystem.GradientSelection) {
                case 0:
                    return sharpnessColors;
                case 1:
                    return unpleasantGradient;
                case 2:
                    return monhunGradiant;
                default:
                    return sharpnessColors;
            }
        }

        public static bool ShouldRenderSharpnessBar(ItemStack item) {
            if (!item.HasToolCurrentSharpness() || !item.HasToolMaxSharpness()) {
                return false;
            }
            if ((item.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() || item.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) && !item.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                return item.GetToolCurrentSharpness() != item.GetToolMaxSharpness();
            } else {
                return false;
            }
        }

        public static int GetItemSharpnessColor(ItemStack item) {
            int maxSharpness = item.GetToolMaxSharpness();
            if (maxSharpness == 0) {
                return 0;
            }

            int num = GameMath.Clamp(100 * item.GetToolCurrentSharpness() / maxSharpness, 0, 99);
            return SharpnessColorGradient[num];
        }

        public static int ToolsmithGetItemDamageColor(ItemStack item) {
            if (item.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                var max = FindLowestMaxDurabilityForBar(item);
                if (max == 0) {
                    return 0;
                }

                int num = GameMath.Clamp(100 * FindLowestCurrentDurabilityForBar(item) / max, 0, 99);
                return GuiStyle.DamageColorGradient[num];
            } else {
                return item.Collectible.GetItemDamageColor(item);
            }
        }

        //For rendering the durability bar to be used in the transpiler. Generally for just Tinkered Tools here, smithed ones can use the default!
        public static int FindLowestCurrentDurabilityForBar(ItemStack itemStack) {
            if (itemStack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                if (!itemStack.HasToolheadCurrentDurability() || !itemStack.HasToolhandleCurrentDurability() || !itemStack.HasToolbindingCurrentDurability()) {
                    return itemStack.Collectible.GetRemainingDurability(itemStack);
                }
                var head = itemStack.GetToolheadCurrentDurability();
                var handle = itemStack.GetToolhandleCurrentDurability();
                var binding = itemStack.GetToolbindingCurrentDurability();
                int lowest;

                if (binding < handle) {
                    lowest = binding;
                } else {
                    lowest = handle;
                }
                if (lowest < head) {
                    return lowest;
                } else {
                    return head;
                }
            } else {
                return itemStack.Collectible.GetRemainingDurability(itemStack);
            }
        }

        //Used just like the above, but for the max durabilities!
        public static int FindLowestMaxDurabilityForBar(ItemStack itemStack) {
            if (itemStack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                if (!itemStack.HasToolhandleMaxDurability() || !itemStack.HasToolbindingMaxDurability()) {
                    return itemStack.Collectible.GetMaxDurability(itemStack);
                }
                var head = itemStack.GetToolheadMaxDurability();
                var handle = itemStack.GetToolhandleMaxDurability();
                var binding = itemStack.GetToolbindingMaxDurability();
                int lowest;

                if (binding < handle) {
                    lowest = binding;
                } else {
                    lowest = handle;
                }
                if (lowest < head) {
                    return lowest;
                } else {
                    return head;
                }
            } else {
                return itemStack.Collectible.GetMaxDurability(itemStack);
            }
        }

        public static void HandleBrokenTinkeredTool(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int remainingHeadDur, int remainingSharpness, int remainingHandleDur, int remainingBindingDur, bool headBroke, bool refillSlot) {
            ItemStack brokenToolStack = itemslot.Itemstack;
            CollectibleObject toolObject = brokenToolStack.Collectible;
            ItemStack toolHead = null;
            bool gaveHead = false;
            ItemStack toolHandle = null;
            bool gaveHandle = false;
            ItemStack toolBinding = brokenToolStack.GetToolbinding(); //This one is the only different one since it COULD be null representing a lack of a binding, but need to see if it even has a binding first by checking this fact
            bool gaveBinding = false;
            ItemStack bitsDrop = null;

            if (remainingHeadDur > 0 && !brokenToolStack.HasPlaceholderHead()) {
                toolHead = brokenToolStack.GetToolhead();
                toolHead.SetPartCurrentDurability(remainingHeadDur);
                toolHead.SetPartMaxDurability(brokenToolStack.GetToolheadMaxDurability());
                toolHead.SetPartCurrentSharpness(remainingSharpness);
                toolHead.SetPartMaxSharpness(brokenToolStack.GetToolMaxSharpness());
            } else {
                headBroke = true; //This right here might be key for compatability sake. The way I built the whole system runs off the assumption that the Tool's Head determines the tool.
                                  //Thus, it can be considered that a tool does not fully "break" in the vanilla sense until the Head itself breaks, it only "falls apart" ie: the tool head flies off the handle, there's possible durability left on both.
                                  //Because of this, always need to consider the possibility of dropping a handle or binder, but if the 'Head' is broken, we also want to run other mod's 'on damage' calls along with vanilla.
                                  //Anything below that checks for !headBroke is looking to see if the Tool should be "Broken" or simply "Fallen Apart" in this sense, if it's fallen apart, do similar checks to vanilla tool breaking locally here. Otherwise let Vanilla code deal with it, since it's all or nothing after this patch is done.
            }
            if (remainingHandleDur > 0) {
                toolHandle = brokenToolStack.GetToolhandle();
                toolHandle.SetPartCurrentDurability(remainingHandleDur);
                toolHandle.SetPartMaxDurability(brokenToolStack.GetToolhandleMaxDurability());
            }
            if (toolBinding != null) { //Binding doesn't always drop, only if the durability is above the threshold, and then if it's below, it breaks and if made of metal, drops some bits
                BindingStats bindingStats = ToolsmithModSystem.Stats.bindings.Get(ToolsmithModSystem.Config.BindingRegistry.Get(toolBinding.Collectible.Code.Path).bindingStatTag);
                float bindingPercentRemains = (float)(remainingBindingDur) / (float)(brokenToolStack.GetToolbindingMaxDurability());
                if (bindingPercentRemains < bindingStats.recoveryPercent) { //If the remaining HP percent is less then the recovery percent, the binding is used up.
                    toolBinding = null; //Set it back to null to prevent dropping anything later! And then to see if Bits should drop!
                }

                if (toolBinding == null && bindingStats.isMetal) {
                    int numBits;
                    if (world.Rand.NextDouble() < 0.5) {
                        numBits = ToolsmithConstants.NumBitsReturnMinimum;
                    } else {
                        numBits = ToolsmithConstants.NumBitsReturnMinimum + 1;
                    }
                    bitsDrop = new ItemStack(world.GetItem(new AssetLocation("game:metalbit-" + bindingStats.metalType)), numBits);
                }
            }

            if (ToolsmithModSystem.Config.DebugMessages && world.Api.Side.IsServer()) {
                ToolsmithModSystem.Logger.Debug("Tool broke!");
                ToolsmithModSystem.Logger.Debug("The tool head is " + (toolHead?.Collectible.Code.ToString()));
                ToolsmithModSystem.Logger.Debug("Head has durability: " + remainingHeadDur);
                ToolsmithModSystem.Logger.Debug("Tool Handle is " + (toolHandle?.Collectible.Code.ToString()));
                ToolsmithModSystem.Logger.Debug("Handle has durability: " + remainingHandleDur);
                ToolsmithModSystem.Logger.Debug("Tool Binding is " + (toolBinding?.Collectible.Code.ToString()));
                ToolsmithModSystem.Logger.Debug("Binding has durability: " + remainingBindingDur);
            }

            EntityPlayer player = byEntity as EntityPlayer;
            if (player != null) {
                //Try to give the player each part, if given successfully, set the stack to null again to represent this
                if (!headBroke && toolHead != null) {
                    gaveHead = player.TryGiveItemStack(toolHead);
                }
                if (toolHandle != null) {
                    IModularPartRenderer behavior = (IModularPartRenderer)toolHandle.Collectible.CollectibleBehaviors.FirstOrDefault(b => (b as IModularPartRenderer) != null);
                    behavior.ResetRotationAndOffset(toolHandle);
                    gaveHandle = player.TryGiveItemStack(toolHandle);
                }
                if (toolBinding != null) {
                    gaveBinding = player.TryGiveItemStack(toolBinding);
                } else if (bitsDrop != null && player.TryGiveItemStack(bitsDrop)) {
                    bitsDrop = null;
                }

                if (!headBroke) { //Move this inside the loop and AFTER giving the itemstack to prevent it from ending up in the same slot that the tool was in. This prevents things like the Treecutting code from falsely assuming the axe is not broke when it actually is.
                    itemslot.Itemstack = null; //Actually 'break' the original item, but only if the head part isn't broken yet. Handle the 'falling apart' of the tools here, but let the 'breaking' happen elsewhere if the head DID fully break.

                    if (refillSlot && toolObject.Tool.HasValue) { //Attempt to refill the slot with a same tool only after the slot is emptied, otherwise it won't succeed.
                        string ident = toolObject.Attributes?["slotRefillIdentifier"].ToString();
                        toolObject.RefillSlotIfEmpty(itemslot, byEntity as EntityAgent, (ItemStack stack) => (ident == null) ? (stack.Collectible.Tool == toolObject.Tool) : (stack.ItemAttributes?["slotRefillIdentifier"]?.ToString() == ident));
                    }

                    if (world.Side.IsServer()) {
                        world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), player);
                    }
                } /*else {
                    itemslot.Itemstack.SetToolheadCurrentDurability(1); //Set it to 1 just in case setting it to 0 gets the game to just delete it from existance. This also works for checks similar to Smithing Plus, which checks if 'remaining dur' is greater then the damage it will take.
                                                                        //But this is a failsafe if another mod does not use the Collectable.GetRemainingDurability call and instead just directly reads the Attributes. Smithing Plus... :P
                }*/ //This should not be needed anymore!
            } else {
                if (!headBroke) { //This needs to be in here as well since no matter what, this needs to run
                    itemslot.Itemstack = null; //Actually 'break' the original item, but only if the head part isn't broken yet. Handle the 'falling apart' of the tools here, but let the 'breaking' happen elsewhere if the head DID fully break.
                    if (world.Side.IsServer()) {
                        world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.SidedPos.X, byEntity.SidedPos.Y, byEntity.SidedPos.Z, null, 1f, 16f);
                    }
                } else {
                    itemslot.Itemstack.SetToolheadCurrentDurability(1); //Set it to 1 just in case setting it to 0 gets the game to just delete it from existance. This also works for checks similar to Smithing Plus, which checks if 'remaining dur' is greater then the damage it will take.
                                                                        //But this is a failsafe if another mod does not use the Collectable.GetRemainingDurability call and instead just directly reads the Attributes. Smithing Plus... :P
                }
            }

            //Drop any remaining tools not given to the player into the world
            if (!headBroke && toolHead != null && !gaveHead) {
                world.SpawnItemEntity(toolHead, byEntity.Pos.XYZ);
            }
            if (toolHandle != null && !gaveHandle) {
                IModularPartRenderer behavior = (IModularPartRenderer)toolHandle.Collectible.CollectibleBehaviors.FirstOrDefault(b => (b as IModularPartRenderer) != null);
                behavior.ResetRotationAndOffset(toolHandle);
                world.SpawnItemEntity(toolHandle, byEntity.Pos.XYZ);
            }
            if (toolBinding != null && !gaveBinding) {
                world.SpawnItemEntity(toolBinding, byEntity.Pos.XYZ);
            } else if (bitsDrop != null) {
                world.SpawnItemEntity(bitsDrop, byEntity.Pos.XYZ);
            }
        }

        public static ItemWhetstone WhetstoneInOffhand(EntityAgent byEntity) {
            if (byEntity.LeftHandItemSlot.Empty) {
                return null;
            }
            var offhandItem = byEntity.LeftHandItemSlot?.Itemstack?.Item;
            return offhandItem as ItemWhetstone;
        }

        public static bool ValidHandleInOffhand(EntityAgent byEntity) {
            var offhandItemCol = byEntity.LeftHandItemSlot?.Itemstack?.Collectible;
            var offhandItemstack = byEntity.LeftHandItemSlot?.Itemstack;
            if (offhandItemCol == null || offhandItemstack.HasWetTreatment()) {
                return false;
            }
            return offhandItemCol.HasBehavior<CollectibleBehaviorToolHandle>();
        }

        public static bool ValidBindingInOffhand(EntityAgent byEntity) {
            var offhandItem = byEntity.LeftHandItemSlot?.Itemstack?.Collectible;
            if (offhandItem == null) {
                return true;
            }
            return offhandItem.HasBehavior<CollectibleBehaviorToolBinding>();
        }

        public static void AssemblePartBundle(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel) {
            ItemStack bundle = new ItemStack(byEntity.World.GetItem(ToolsmithConstants.ToolBundleCode), 1);
            ItemSlot handleSlot = byEntity.LeftHandItemSlot;
            ItemStack head = slot.Itemstack;
            ItemStack handle = handleSlot.Itemstack;

            var bundleMultiPartRenderTree = bundle.GetMultiPartRenderTree(); //Time to assign the data for rendering the Bundle!
            var headPartAndTransformTree = bundleMultiPartRenderTree.GetPartAndTransformRenderTree(ToolsmithAttributes.ModularPartHeadName);

            //Set up the Head tree!
            headPartAndTransformTree.SetPartOffsetX(0);
            headPartAndTransformTree.SetPartOffsetY(0);
            headPartAndTransformTree.SetPartOffsetZ(0.2f);
            headPartAndTransformTree.SetPartRotationX(0);
            headPartAndTransformTree.SetPartRotationY(45);
            headPartAndTransformTree.SetPartRotationZ(0);
            var headPartTree = headPartAndTransformTree.GetPartRenderTree();
            headPartTree.SetPartShapePath(head.Item.Shape.Base.Domain + ":shapes/" + head.Item.Shape.Base.Path);
            var headTextureTree = headPartTree.GetPartTextureTree();
            if (byEntity.Api.Side.IsServer()) {
                ToolHeadTextureData textures;
                var success = RecipeRegisterModSystem.ToolHeadTexturesCache.TryGetValue(head.Item.Code, out textures);
                if (success) {
                    for (int i = 0; i < textures.Tags.Count; i++) {
                        headTextureTree.SetPartTexturePathFromKey(textures.Tags[i], textures.Paths[i]);
                    }
                } else {
                    ToolsmithModSystem.Logger.Error("Could not find the tool head's ToolHeadTextureData entry when crafting a Part Bundle. Might not render right.");
                }
            } else {
                foreach (var tex in head.Item.Textures) {
                    headTextureTree.SetPartTexturePathFromKey(tex.Key, tex.Value.Base);
                }
            }
            
            //Handle time for the Render Data! But at least that's already part of the handle by now. Most likely.
            if (handle.HasMultiPartRenderTree()) {
                var handleMultiPartTree = handle.GetMultiPartRenderTree();
                foreach (var tree in handleMultiPartTree) {
                    var subPartAndTransformTree = handleMultiPartTree.GetPartAndTransformRenderTree(tree.Key);
                    subPartAndTransformTree.SetPartOffsetX(-0.1f);
                    subPartAndTransformTree.SetPartOffsetY(0);
                    subPartAndTransformTree.SetPartOffsetZ(0);
                    subPartAndTransformTree.SetPartRotationX(0);
                    subPartAndTransformTree.SetPartRotationY(90);
                    subPartAndTransformTree.SetPartRotationZ(0);
                    bundleMultiPartRenderTree.SetPartAndTransformRenderTree(tree.Key, handleMultiPartTree.GetPartAndTransformRenderTree(tree.Key));
                }
            } else if (handle.HasPartRenderTree()) {
                var handlePartAndTransformTree = bundleMultiPartRenderTree.GetPartAndTransformRenderTree(ToolsmithAttributes.ModularPartHandleName);
                handlePartAndTransformTree.SetPartOffsetX(-0.1f);
                handlePartAndTransformTree.SetPartOffsetY(0);
                handlePartAndTransformTree.SetPartOffsetZ(0);
                handlePartAndTransformTree.SetPartRotationX(0);
                handlePartAndTransformTree.SetPartRotationY(90);
                handlePartAndTransformTree.SetPartRotationZ(0);
                handlePartAndTransformTree.SetPartRenderTree(handle.GetPartRenderTree());
            } else {
                var handlePartAndTransformTree = bundleMultiPartRenderTree.GetPartAndTransformRenderTree(ToolsmithAttributes.ModularPartHandleName);
                handlePartAndTransformTree.SetPartOffsetX(0);
                handlePartAndTransformTree.SetPartOffsetY(0);
                handlePartAndTransformTree.SetPartOffsetZ(0);
                handlePartAndTransformTree.SetPartRotationX(0);
                handlePartAndTransformTree.SetPartRotationY(145);
                handlePartAndTransformTree.SetPartRotationZ(0);
                var handlePartTree = handlePartAndTransformTree.GetPartRenderTree();
                handlePartTree.SetPartShapePath(handle.Item.Shape.Base.Domain + ":shapes/" + handle.Item.Shape.Base.Path);
            }

            bundle.SetToolhead(slot.TakeOut(1));
            bundle.SetToolhandle(handleSlot.TakeOut(1)); //Take out one, and set it as the Bundle's tool handle!

            handleSlot.MarkDirty();
            ItemStack tempHolder = slot.Itemstack;
            slot.Itemstack = bundle; //Above holds the possible multiple-stacked Toolheads, this finally gives the crafted tool to slot that previously had the head(s)
            slot.MarkDirty();
            if (tempHolder != null) {
                if (!byEntity.TryGiveItemStack(tempHolder)) { //This should hopefully return any remainder!
                    byEntity.World.SpawnItemEntity(tempHolder, byEntity.Pos.XYZ);
                }
            }
        }

        public static void AssembleFullTool(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel) {
            CollectibleObject craftedTool;
            ItemStack head = slot.Itemstack.GetToolhead();
            var success = RecipeRegisterModSystem.TinkerToolGridRecipes.TryGetValue(head.Collectible.Code.ToString(), out craftedTool);
            if (success) {
                ItemSlot bindingSlot = byEntity.LeftHandItemSlot;
                ItemStack craftedItemStack = new ItemStack(byEntity.World.GetItem(craftedTool.Code), 1); //Create the tool in question
                ItemSlot placeholderOutput = new ItemSlot(new DummyInventory(ToolsmithModSystem.Api));
                placeholderOutput.Itemstack = craftedItemStack;

                GridRecipe DummyRecipe = new() {
                    AverageDurability = false,
                    Output = new() {
                        ResolvedItemstack = craftedItemStack
                    },
                    Name = new AssetLocation("toolsmith:inhandtinkertoolcrafting")
                };

                ItemStack handle = slot.Itemstack.GetToolhandle();
                ItemSlot headSlot = new ItemSlot(new DummyInventory(ToolsmithModSystem.Api));
                headSlot.Itemstack = head;
                ItemSlot handleSlot = new ItemSlot(new DummyInventory(ToolsmithModSystem.Api));
                handleSlot.Itemstack = handle;
                ItemSlot[] inputSlots;
                if (bindingSlot.Empty) {
                    inputSlots = new ItemSlot[] { headSlot, handleSlot };
                } else {
                    inputSlots = new ItemSlot[] { headSlot, handleSlot, bindingSlot };
                }

                craftedItemStack.Collectible.ConsumeCraftingIngredients(inputSlots, placeholderOutput, DummyRecipe); //This line is needed because of ItemRarity, but at the same time, this is technically called _AFTER_ the 'onCreatedByCrafting' line, when the player actually clicks to take the item...
                                                                                                                     //Might be a good idea to reconsider when the whole Tinker Tool Crafting logic is called, but... Would require patching this call, and it's ONLY for Item Rarity so far, not exactly a priority by a long shot. Leaving this note incase something else uses this, but also probably not a big deal to make the change either?
                craftedItemStack.Collectible.OnCreatedByCrafting(inputSlots, placeholderOutput, DummyRecipe); //Hopefully call this just like it would if properly crafted in the grid!

                if (!bindingSlot.Empty) {
                    bindingSlot.TakeOut(1);
                    bindingSlot.MarkDirty();
                }
                slot.Itemstack = craftedItemStack;
                slot.MarkDirty();
            }
        }

        //Older code now, may be repurposed for the workbench later on.
        public static ItemSlot SearchForPossibleBindings(IPlayer player) { //Searches only the Hotbar just for efficiency sake! Also kinda ease of use that you don't have to dump EVERYTHING on the ground that might be a binding. Just store it in bags.
            IInventory hotbar = player.InventoryManager.GetHotbarInventory();
            foreach (var slot in hotbar.Where(s => s.Itemstack != null)) {
                if (slot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBinding>()) {
                    return slot;
                }
            }
            return null;
        }

        //Old code now, may be repurposed for the Workbench later on.
        public static void CraftTool(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            CollectibleObject craftedTool;
            var success = RecipeRegisterModSystem.TinkerToolGridRecipes.TryGetValue(slot.Itemstack.Collectible.Code.ToString(), out craftedTool);
            if (success) {
                IPlayer player = ((EntityPlayer)byEntity).Player; //I THINK only a player can ever craft something? I dunno. This aint modded Minecraft though so maybe no fake players to account for woo.
                ItemSlot bindingSlot = SearchForPossibleBindings(player); //Does the Player have a valid Binding in their hotbar? Note, this bindingSlot can be null! That means no binding found and not used.
                if (bindingSlot == null) { //If so, grab it and the handle...
                    bindingSlot = new DummySlot();
                }
                ItemSlot handleSlot = byEntity.LeftHandItemSlot;

                ItemStack craftedItemStack = new ItemStack(byEntity.World.GetItem(craftedTool.Code), 1); //Create the tool in question
                ItemSlot placeholderOutput = new ItemSlot(new DummyInventory(ToolsmithModSystem.Api));
                placeholderOutput.Itemstack = craftedItemStack;

                GridRecipe DummyRecipe = new() {
                    AverageDurability = false,
                    Output = new() {
                        ResolvedItemstack = craftedItemStack
                    }
                };
                ItemSlot[] inputSlots;
                if (bindingSlot.GetType() != typeof(DummySlot)) {
                    inputSlots = new ItemSlot[] { slot, handleSlot, bindingSlot };
                } else {
                    inputSlots = new ItemSlot[] { slot, handleSlot };
                }

                craftedItemStack.Collectible.ConsumeCraftingIngredients(inputSlots, placeholderOutput, DummyRecipe);
                craftedItemStack.Collectible.OnCreatedByCrafting(inputSlots, placeholderOutput, DummyRecipe); //Hopefully call this just like it would if properly crafted in the grid!

                handleSlot.TakeOut(1); //Decrement inputs, and place the finished item in the ToolHead's Slot
                handleSlot.MarkDirty();
                if (bindingSlot.GetType() != typeof(DummySlot)) {
                    bindingSlot.TakeOut(1);
                    bindingSlot.MarkDirty();
                }
                ItemStack tempHolder = slot.Itemstack; //I don't believe any Toolhead will actually stack more then once -- Actually they do. Huh. I never tried before. Good thing I had this!
                slot.Itemstack = craftedItemStack; //Above holds the possible multiple-stacked Toolheads, this finally gives the crafted tool to slot that previously had the head(s)
                slot.MarkDirty();
                if (tempHolder.StackSize > 1) {
                    tempHolder.StackSize -= 1;
                    if (!byEntity.TryGiveItemStack(tempHolder)) { //This should hopefully return any remainder!
                        byEntity.World.SpawnItemEntity(tempHolder, byEntity.Pos.XYZ);
                    }
                }
            }
        }

        public static bool IsAnyToolPart(CollectibleObject item, IWorldAccessor world) {
            if (world.Side.IsServer() && ToolsmithModSystem.IgnoreCodes.Count > 0 && ToolsmithModSystem.IgnoreCodes.Contains(item.Code.ToString())) {
                return false;
            } else if (item.HasBehavior<CollectibleBehaviorToolHead>()) {
                return true;
            } else if (item.HasBehavior<CollectibleBehaviorToolHandle>()) {
                return true;
            } else if (item.HasBehavior<CollectibleBehaviorToolBinding>()) {
                return true;
            }
            
            return false;
        }

        //This checks if it is a valid repair tool as well as if it is a fully tinkered tool or if it is just a tool's head, since the durabilities are stored under different attributes
        public static int IsValidSharpenTool(CollectibleObject item, IWorldAccessor world) {
            if (world.Side.IsServer() && ToolsmithModSystem.IgnoreCodes.Count > 0 && ToolsmithModSystem.IgnoreCodes.Contains(item.Code.ToString())) { //First check if the ignore list has any entries, and ensure this one isn't on it. Likely means something got improperly given the Behavior on init.
                return 0;
            } else if (item.HasBehavior<CollectibleBehaviorTinkeredTools>()) { //This one stores it under 'tinkeredToolHead' durability
                if (!item.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                    return 1;
                }
            } else if (item.HasBehavior<CollectibleBehaviorSmithedTools>()) { //And this one just uses the regular durability values since it's just a single solid tool, no parts
                if (!item.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                    return 2;
                }
            } else if (item.HasBehavior<CollectibleBehaviorToolHead>()) { //While this stores it as just 'toolPartDurability', since not every part will be a head, but every head will have this behavior
                if (!item.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                    return 3;
                }
            }

            return 0;
        }

        public static bool IsDeconstructableTool(CollectibleObject item, IWorldAccessor world) {
            if (world.Side.IsServer() && ToolsmithModSystem.IgnoreCodes.Count > 0 && ToolsmithModSystem.IgnoreCodes.Contains(item.Code.ToString())) { //First check if the ignore list has any entries, and ensure this one isn't on it. Likely means something got improperly given the Behavior on init.
                return false;
            } else if (item.HasBehavior<CollectibleBehaviorTinkeredTools>()) { //This one stores it under 'tinkeredToolHead' durability
                return true;
            }

            return false;
        }

        public static bool ToolOrHeadNeedsSharpening(ItemStack item, IWorldAccessor world) {
            var curSharp = item.GetToolCurrentSharpness();
            var maxSharp = item.GetToolMaxSharpness();
            int curDur;
            var toolType = IsValidSharpenTool(item.Collectible, world);

            if (toolType == 1) {
                curDur = item.GetToolheadCurrentDurability();
            } else if (toolType == 2) {
                curDur = item.GetSmithedDurability();
            } else if (toolType == 3) {
                curDur = item.GetPartCurrentDurability();
            } else {
                curDur = 0;
            }

            return (curSharp < maxSharp && curDur > 0);
        }

        public static bool TryWhetstoneSharpening(ref float deltaLastTick, ref float lastInterval, float secondsUsed, ItemSlot slot, EntityAgent byEntity) {
            if (byEntity.World.Side.IsServer()) {
                deltaLastTick = secondsUsed - lastInterval;

                if (deltaLastTick >= ToolsmithConstants.sharpenInterval) { //Try not to repair EVERY single tick to space it out some. Cause of this, repair 5 durability each time so it doesn't take forever.
                    var whetstone = WhetstoneInOffhand(byEntity);

                    if (whetstone != null && !slot.Empty) { //If the offhand is still a Whetstone, sharpen! Otherwise break out of this entirely and end the action.
                        var isTool = IsValidSharpenTool(slot.Itemstack.Collectible, byEntity.World);
                        whetstone.HandleSharpenTick(secondsUsed, slot, byEntity.LeftHandItemSlot, byEntity, isTool);
                    } else {
                        return false;
                    }

                    deltaLastTick = 0;
                    lastInterval = MathUtility.FloorToNearestMult(secondsUsed, ToolsmithConstants.sharpenInterval);

                    if (!ToolOrHeadNeedsSharpening(slot.Itemstack, byEntity.World)) {
                        return false; //End the interaction when it doesn't need sharpening anymore
                    }
                }
            }

            return true;
        }

        //The next three methods are for the three steps of handling the sharpness honing. It helped to encapsulate it all to handle both the Grindstone and the Whetstones here.
        public static void RecieveDurabilitiesAndSharpness(ref int curDur, ref int maxDur, ref int curSharp, ref int maxSharp, ItemStack item, int isTool) {
            if (isTool == 1) { //The item is a Tinkered Tool! Use the extensions for the tool's head durability.
                curDur = item.GetToolheadCurrentDurability();
                maxDur = item.GetToolheadMaxDurability();
                if (item.HasPlaceholderHead()) { //If the tool still has no proper head item saved to it, something went wrong and an error should have been printed.
                    return;
                }
                var handleDur = item.GetToolhandleCurrentDurability(); //This is mostly just being called to test that the tools are fully initialized.
                var bindingDur = item.GetToolbindingCurrentDurability(); //^^^
                curSharp = item.GetToolCurrentSharpness();
                maxSharp = item.GetToolMaxSharpness();
            } else if (isTool == 2) { //The item is a Smithed Tool!
                curDur = item.GetSmithedDurability();
                maxDur = item.GetSmithedMaxDurability();
                curSharp = item.GetToolCurrentSharpness();
                maxSharp = item.GetToolMaxSharpness();
            } else { //The item is just a Tool Head, not on a tool put together. Use the extensions for Part Durability.
                curDur = item.GetPartCurrentDurability();
                maxDur = item.GetPartMaxDurability();
                curSharp = item.GetPartCurrentSharpness();
                maxSharp = item.GetPartMaxSharpness();
            }
        }

        public static void ActualSharpenTick(ref int curDur, ref int curSharp, ref float totalSharpnessHoned, int maxSharp, EntityAgent byEntity) {
            if (curSharp < maxSharp && curDur > 0) {
                float percent = 1.0f;
                if (ToolsmithModSystem.Config.GrindstoneSharpenPerTick >= 1 && ToolsmithModSystem.Config.GrindstoneSharpenPerTick <= 100) {
                    percent = ((float)ToolsmithModSystem.Config.GrindstoneSharpenPerTick / 100f);
                }

                int percentSharpen = (int)Math.Ceiling(percent * maxSharp);
                curSharp += percentSharpen;
                if (curSharp >= maxSharp) {
                    curSharp = maxSharp;
                } else {
                    totalSharpnessHoned += percent;
                }

                bool damageDurability = true;
                if (totalSharpnessHoned > 0.66) {
                    damageDurability = (byEntity.World.Rand.NextDouble() <= 0.05f);
                } else if (totalSharpnessHoned > 0.33) {
                    damageDurability = MathUtility.ShouldDamageFromSharpening(byEntity.World, totalSharpnessHoned);
                }

                if (damageDurability) {
                    curDur -= percentSharpen;
                }
            }
        }

        public static void SetResultsOfSharpening(int curDur, int curSharp, ItemStack item, EntityAgent byEntity, ItemSlot mainHandSlot, int isTool) {
            if (isTool == 1) {
                item.SetToolheadCurrentDurability(curDur);
                item.SetToolCurrentSharpness(curSharp);
                if (curDur <= 0) {
                    CollectibleObject toolObj = item.Collectible;
                    HandleBrokenTinkeredTool(byEntity.World, byEntity, mainHandSlot, 0, curSharp, item.GetToolhandleCurrentDurability(), item.GetToolbindingCurrentDurability(), true, false);
                    item.SetBrokeWhileSharpeningFlag();
                    toolObj.DamageItem(byEntity.World, byEntity, mainHandSlot);
                    if (item != null) {
                        item.ClearBrokeWhileSharpeningFlag();
                    }
                }
            } else if (isTool == 2) {
                item.SetSmithedDurability(curDur);
                item.SetToolCurrentSharpness(curSharp);
                if (curDur <= 0) {
                    item.SetSmithedDurability(1);
                    item.SetBrokeWhileSharpeningFlag();
                    item.Collectible.DamageItem(byEntity.World, byEntity, mainHandSlot);
                    if (item != null) {
                        item.ClearBrokeWhileSharpeningFlag();
                    }
                }
            } else {
                item.SetPartCurrentDurability(curDur);
                item.SetPartCurrentSharpness(curSharp);
                if (curDur <= 0) {
                    //Wait... This might be silly and may be hacky but... Can I just make it into an item AND break it right here right now? Lmao
                    CollectibleObject toolToBreakObj; //This might proc other mod's on damage stuff for the head as if it were a real tool.
                    var success = RecipeRegisterModSystem.TinkerToolGridRecipes.TryGetValue(item.Collectible.Code.ToString(), out toolToBreakObj);
                    if (success) {
                        ItemStack toolToBreak = new ItemStack(byEntity.World.GetItem(toolToBreakObj.Code), 1);
                        if (IsDeconstructableTool(toolToBreak.Collectible, byEntity.World)) { //Just to make sure it isn't on the ignore list already, but it should only come back as a Tinkered Tool.
                                             //Initiate that JUST to insure it breaks now! Haha!
                            item = null;
                            mainHandSlot.Itemstack = toolToBreak.Clone();
                            mainHandSlot.Itemstack.SetToolheadCurrentDurability(1);
                            mainHandSlot.Itemstack.SetBrokeWhileSharpeningFlag();
                            mainHandSlot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, mainHandSlot);
                            if (!mainHandSlot.Empty) {
                                mainHandSlot.Itemstack.ClearBrokeWhileSharpeningFlag();
                                mainHandSlot.Itemstack = null;
                            }
                        } else { //Just in case, if all else fails, just destroy the head. But man I hope this works, haha.
                            item = null;
                        }
                    }
                }
            }
        }

        public static void DisassembleTool(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            //Actually take apart the tool here!
            //Get the parts of the tool from it
            var tool = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            var head = tool.GetToolhead();
            var handle = tool.GetToolhandle();
            var binding = tool.GetToolbinding(); //Might be null if there is no binding.

            //Check if the Binding is null, if not, see if it is still at full durability - otherwise don't return it.
            if (binding != null) {
                if (tool.GetToolbindingCurrentDurability() != tool.GetToolbindingMaxDurability()) {
                    binding = null; //If it's null, no binding drop!
                }
            }

            //For both the Head and Handle, set the part durabilities (and sharpness for head!)
            head.SetPartCurrentDurability(tool.GetToolheadCurrentDurability());
            head.SetPartMaxDurability(tool.GetToolheadMaxDurability());
            head.SetPartCurrentSharpness(tool.GetToolCurrentSharpness());
            head.SetPartMaxSharpness(tool.GetToolMaxSharpness());
            handle.SetPartCurrentDurability(tool.GetToolhandleCurrentDurability());
            handle.SetPartMaxDurability(tool.GetToolhandleMaxDurability());

            //Return it all to the player, and get rid of the tool.
            bool gaveHead = false;
            bool gaveHandle = false;
            bool gaveBinding = false;
            var player = byPlayer.Entity;

            if (player != null) {
                gaveHead = player.TryGiveItemStack(head);
                IModularPartRenderer handleBehavior = (IModularPartRenderer)handle.Collectible.CollectibleBehaviors.FirstOrDefault(b => (b as IModularPartRenderer) != null);
                handleBehavior.ResetRotationAndOffset(handle);
                gaveHandle = player.TryGiveItemStack(handle);
                if (binding != null) {
                    gaveBinding = player.TryGiveItemStack(binding);
                }
            }

            byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack = null;

            //If no room in inventory, drop in world instead.
            if (!gaveHead) {
                player.World.SpawnItemEntity(head, player.Pos.XYZ);
            }
            if (!gaveHandle) {
                IModularPartRenderer handleBehavior = (IModularPartRenderer)handle.Collectible.CollectibleBehaviors.FirstOrDefault(b => (b as IModularPartRenderer) != null);
                handleBehavior.ResetRotationAndOffset(handle);
                player.World.SpawnItemEntity(handle, player.Pos.XYZ);
            }
            if (binding != null && !gaveBinding) {
                player.World.SpawnItemEntity(binding, player.Pos.XYZ);
            }

            byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        }

        public static bool CheckForAndScrubStickBone(ItemStack stack) {
            if (IsStickOrBone(stack)) {
                if ((!stack.HasPartCurrentDurability() && !stack.HasPartMaxDurability()) || !(stack.GetPartCurrentDurability() < stack.GetPartMaxDurability())) {
                    if (!stack.HasHandleGripTag() && !stack.HasHandleTreatmentTag()) {
                        stack.RemoveMultiPartRenderTree();
                        stack.RemovePartRenderTree();
                        stack.RemoveHandleStatTag();
                        stack.RemovePartCurrentDurability();
                        stack.RemovePartMaxDurability();
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsStickOrBone(ItemStack stack) {
            if (stack.Collectible.Code == ToolsmithConstants.DefaultHandleCode || stack.Collectible.Code == ToolsmithConstants.BoneHandleCode) {
                return true;
            } else {
                return false;
            }
        }
    }
}
