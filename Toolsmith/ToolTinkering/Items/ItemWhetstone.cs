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
        protected bool sharpening = false;
        protected bool firstHoning = false;
        protected bool doneHoning = false;
        protected float totalSharpnessHoned = 0;
        protected float deltaLastTick = 0;
        protected float lastInterval = 0;

        //Copied over from the Grindstone but changed to instead be done in-hand with the items instead of on a block. It's slightly different handling.
        public void HandleSharpenTick(float secondsUsed, ItemSlot mainHandSlot, ItemSlot offhandSlot, EntityAgent byEntity, int isTool) { //"isTool" is fed by the respective items in question when they call this to try and sharpen.
            int curDur = 0;
            int maxDur = 0;
            int curSharp = 0;
            int maxSharp = 0;
            ItemStack item = mainHandSlot.Itemstack;
            ItemStack whetstone = offhandSlot.Itemstack;
            firstHoning = !(item.HasTotalHoneValue());

            TinkeringUtility.RecieveDurabilitiesAndSharpness(ref curDur, ref maxDur, ref curSharp, ref maxSharp, ref totalSharpnessHoned, item, isTool);

            TinkeringUtility.ActualSharpenTick(ref curDur, ref curSharp, maxSharp, ref totalSharpnessHoned, firstHoning, byEntity);

            whetstone.Collectible.DamageItem(byEntity.World, byEntity, offhandSlot);

            TinkeringUtility.SetResultsOfSharpening(curDur, curSharp, totalSharpnessHoned, firstHoning, item, byEntity, mainHandSlot, isTool);

            mainHandSlot.MarkDirty();
            offhandSlot.MarkDirty();

            if (!TinkeringUtility.ToolOrHeadNeedsSharpening(item, byEntity.World)) {
                doneHoning = true;
            }
        }

        public bool IsDoneHoning() {
            return doneHoning;
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

        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) {
            if (extractedStack != null && honingScrape != null && honingScrape.IsPlaying) {
                honingScrape?.FadeOut(0.2f, (s) => { s.Dispose(); honingScrape = null; });
            }

            base.OnModifiedInInventorySlot(world, slot, extractedStack);
        }

        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity) {
            if (sharpening) {
                return "sharpeningstone";
            }

            return null;
        }

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
                sharpening = true;
                ToggleHoningSound(true, byEntity);
                return;
            } else {
                ToggleHoningSound(false, byEntity);
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            if (sharpening) {
                var mainHandSlot = byEntity.RightHandItemSlot;
                UpdateSoundPosition(byEntity.Api, byEntity.Pos.XYZFloat);
                var retVal = TinkeringUtility.TryWhetstoneSharpening(ref deltaLastTick, ref lastInterval, secondsUsed, mainHandSlot, byEntity);
                if (doneHoning || slot.Empty || slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack) <= 1) {
                    ToggleHoningSound(false, byEntity);
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

            if (sharpening) {
                deltaLastTick = 0;
                lastInterval = 0;
                if (byEntity.World.Side.IsServer() && doneHoning) {
                    byEntity.World.PlaySoundAt(new AssetLocation("toolsmith:sounds/honing-finish.ogg"), byEntity, randomizePitch: false);
                    doneHoning = false;
                }
                totalSharpnessHoned = 0;
                sharpening = false;
            }

            byEntity.StopAnimation("sharpeningstone");
            if (!(slot is ItemSlotOffhand)) {
                base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
            }
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason) {
            if (ToolsmithModSystem.Config.AccessibilityDisableNeedToHoldClick && sharpening) {
                return false;
            }

            ToggleHoningSound(false, byEntity);
            
            if (sharpening) {
                deltaLastTick = 0;
                lastInterval = 0;
                /*if (byEntity.World.Side.IsServer()) {
                    byEntity.World.PlaySoundAt(new AssetLocation("toolsmith:sounds/honing-finish.ogg"), byEntity, randomizePitch: false);
                }*/ //Commenting this out to see how it feels/sounds without it playing the 'finish' sound when you let go of the mouse. Seems like a great idea honestly with the current changes.
                totalSharpnessHoned = 0;
                sharpening = false;
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
