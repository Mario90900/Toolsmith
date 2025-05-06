using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Toolsmith.ToolTinkering.Items {
    public class ItemWhetstone : Item {
        protected ILoadedSound honingScrape;
        protected bool sharpening = false;
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

            TinkeringUtility.RecieveDurabilitiesAndSharpness(ref curDur, ref maxDur, ref curSharp, ref maxSharp, item, isTool);

            TinkeringUtility.ActualSharpenTick(ref curDur, ref curSharp, ref totalSharpnessHoned, maxSharp, byEntity);

            whetstone.Collectible.DamageItem(byEntity.World, byEntity, offhandSlot);

            TinkeringUtility.SetResultsOfSharpening(curDur, curSharp, item, byEntity, mainHandSlot, isTool);

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
                        Position = byEntity.Pos.AsBlockPos.ToVec3f().Add(0.5f, 0.5f, 0.5f),
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

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) {
            if (!(slot is ItemSlotOffhand)) {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            }
            var mainHandSlot = byEntity.RightHandItemSlot;
            if (handling == EnumHandHandling.PreventDefault) {
                return;
            }

            if (!mainHandSlot.Empty && TinkeringUtility.ToolOrHeadNeedsSharpening(mainHandSlot.Itemstack, byEntity.World)) {
                handling = EnumHandHandling.PreventDefault;
                sharpening = true;
                ToggleHoningSound(true, byEntity);
                return;
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            if (sharpening) {
                var mainHandSlot = byEntity.RightHandItemSlot;
                var retVal = TinkeringUtility.TryWhetstoneSharpening(ref deltaLastTick, ref lastInterval, secondsUsed, mainHandSlot, byEntity);
                if (slot.Empty || slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack) <= 1) {
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
            if (sharpening) {
                deltaLastTick = 0;
                lastInterval = 0;
                ToggleHoningSound(false, byEntity);
                if (byEntity.World.Side.IsServer()) {
                    byEntity.World.PlaySoundAt(new AssetLocation("toolsmith:sounds/honing-finish.ogg"), blockSel.Position, 0);
                }
                totalSharpnessHoned = 0;
                sharpening = false;
            }

            if (!(slot is ItemSlotOffhand)) {
                base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
            }
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason) {
            if (sharpening) {
                deltaLastTick = 0;
                lastInterval = 0;
                ToggleHoningSound(false, byEntity);
                if (byEntity.World.Side.IsServer()) {
                    byEntity.World.PlaySoundAt(new AssetLocation("toolsmith:sounds/honing-finish.ogg"), blockSel.Position, 0);
                }
                totalSharpnessHoned = 0;
                sharpening = false;
            }

            if (!(slot is ItemSlotOffhand)) {
                return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);
            } else {
                return true;
            }
        }
    }
}
