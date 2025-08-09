using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Toolsmith.ToolTinkering.Items {
    public class ItemWhetstone : Item, IOffhandDominantInteractionItem {
        protected ILoadedSound honingScrape;

        //Copied over from the Grindstone but changed to instead be done in-hand with the items instead of on a block. It's slightly different handling.
        public void HandleSharpenTick(float secondsUsed, ItemSlot mainHandSlot, ItemSlot offhandSlot, EntityAgent byEntity, int isTool) { //"isTool" is fed by the respective items in question when they call this to try and sharpen.
            int curDur = 0;
            int maxDur = 0;
            int curSharp = 0;
            int maxSharp = 0;
            ItemStack item = mainHandSlot.Itemstack;
            ItemStack whetstone = offhandSlot.Itemstack;
            var firstHoning = !(item.HasTotalHoneValue());
            var totalSharpnessHoned = 0.0f;

            TinkeringUtility.RecieveDurabilitiesAndSharpness(ref curDur, ref maxDur, ref curSharp, ref maxSharp, ref totalSharpnessHoned, item, isTool);

            TinkeringUtility.ActualSharpenTick(ref curDur, ref curSharp, maxSharp, ref totalSharpnessHoned, firstHoning, byEntity);

            whetstone.Collectible.DamageItem(byEntity.World, byEntity, offhandSlot);

            TinkeringUtility.SetResultsOfSharpening(curDur, curSharp, totalSharpnessHoned, firstHoning, item, byEntity, mainHandSlot, isTool);

            if (!TinkeringUtility.ToolOrHeadNeedsSharpening(item, byEntity.World)) {
                whetstone.SetWhetstoneDoneSharpen();
            }

            mainHandSlot.MarkDirty();
            offhandSlot.MarkDirty();
        }

        public void ToggleHoningSound(bool startSound, EntityAgent byEntity) {
            if (startSound) {
                var api = byEntity.Api;
                if (!api.Side.IsClient()) {
                    return;
                }
                if (honingScrape == null || !honingScrape.IsPlaying) {
                    honingScrape = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams() {
                        Location = new AssetLocation("toolsmith:sounds/whetstone-scraping-loop.ogg"),
                        ShouldLoop = true,
                        Position = byEntity.Pos.XYZFloat,
                        DisposeOnFinish = false,
                        Volume = 0,
                        Range = 6,
                        SoundType = EnumSoundType.Ambient
                    });

                    if (honingScrape != null) {
                        honingScrape.Start();
                        honingScrape.FadeTo(0.75, 1f, (s) => { });
                    }
                } else {
                    if (honingScrape.IsPlaying) {
                        honingScrape.FadeTo(0.75, 1f, (s) => { });
                    }
                }
            } else {
                honingScrape?.FadeOut(0.2f, (s) => { s.Dispose(); honingScrape = null; });
            }
        }

        public void UpdateSoundPosition(ICoreAPI api, Vec3f pos) {
            if (!api.Side.IsClient()) {
                return;
            }

            if (honingScrape != null && honingScrape.IsPlaying) {
                honingScrape.SetPosition(pos);
            }
        }

        public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1) {
            if (itemslot.Itemstack.Collectible.GetRemainingDurability(itemslot.Itemstack) <= amount) {
                if (honingScrape != null && honingScrape.IsPlaying) {
                    honingScrape?.FadeOut(0.2f, (s) => { s.Dispose(); honingScrape = null; });
                }

                if (byEntity as EntityPlayer != null) {
                    byEntity.StopAnimation("sharpeningstone");
                }
            }

            base.DamageItem(world, byEntity, itemslot, amount);
        }

        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) {
            if (extractedStack != null && honingScrape != null && honingScrape.IsPlaying) {
                honingScrape?.FadeOut(0.2f, (s) => { s.Dispose(); honingScrape = null; });
            }

            base.OnModifiedInInventorySlot(world, slot, extractedStack);
        }

        /*public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity) {
            if (sharpening) {
                return "sharpeningstone";
            }

            return null;
        }*/

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) {
            if (!(slot is ItemSlotOffhand)) {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            }

            var mainHandSlot = byEntity.RightHandItemSlot;
            if (handling == EnumHandHandling.PreventDefault) {
                return;
            }

            if (firstEvent && !mainHandSlot.Empty && TinkeringUtility.ToolOrHeadNeedsSharpening(mainHandSlot.Itemstack, byEntity.World, byEntity)) {
                handling = EnumHandHandling.PreventDefault;
                byEntity.StartAnimation("sharpeningstone");
                slot.Itemstack.SetWhetstoneInUse(0.0f);
                ToggleHoningSound(true, byEntity);
                return;
            } else {
                ToggleHoningSound(false, byEntity);
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            if (slot.Itemstack.WhetstoneInUse()) {
                var mainHandSlot = byEntity.RightHandItemSlot;
                UpdateSoundPosition(byEntity.Api, byEntity.Pos.XYZFloat);
                var lastInterval = slot.Itemstack.GetWhetstoneInUse();
                var retVal = TinkeringUtility.TryWhetstoneSharpening(ref lastInterval, secondsUsed, mainHandSlot, byEntity);
                if (slot.Empty || slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack) <= 1 || slot.Itemstack.WhetstoneDoneSharpen()) {
                    byEntity.StopAnimation("sharpeningstone");
                    ToggleHoningSound(false, byEntity);
                } else {
                    slot.Itemstack.SetWhetstoneInUse(lastInterval);
                }
                return retVal;
            }

            if (!(slot is ItemSlotOffhand)) {
                return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
            } else {
                return true;
            }
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            ToggleHoningSound(false, byEntity);

            if (slot.Empty || slot.Itemstack.WhetstoneInUse()) {
                if (byEntity.World.Side.IsServer() && slot.Itemstack.WhetstoneDoneSharpen()) {
                    byEntity.World.PlaySoundAt(new AssetLocation("toolsmith:sounds/honing-finish.ogg"), byEntity, randomizePitch: false);
                    slot.Itemstack.ClearWhetstoneDoneSharpen();
                }
                slot.Itemstack.ClearWhetstoneInUse();
            }

            byEntity.StopAnimation("sharpeningstone");
            if (!(slot is ItemSlotOffhand)) {
                base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
            }
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason) {
            if (ToolsmithModSystem.Config.AccessibilityDisableNeedToHoldClick && !slot.Empty && slot.Itemstack.WhetstoneInUse()) {
                return false;
            }

            ToggleHoningSound(false, byEntity);
            
            if (slot.Empty || slot.Itemstack.WhetstoneInUse()) {
                slot.Itemstack.ClearWhetstoneInUse();
            }

            byEntity.StopAnimation("sharpeningstone");
            if (!(slot is ItemSlotOffhand)) {
                return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);
            } else {
                return true;
            }
        }

        //It is important to know that this will send the OffhandItem the Main Hand slot! And not the Offhand one like in the _actual_ calls above.
        //This returns false if it should steal the call, and true if it should let it keep going.
        public bool HasOffhandInteractionAvailable(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent) {
            if (!slot.Empty && TinkeringUtility.IsValidSharpenTool(slot.Itemstack.Collectible, byEntity.World) > 0) {
                return false;
            }

            return true;
        }
    }
}
