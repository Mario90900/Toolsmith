using System;
using System.Collections.Generic;
using System.Linq;
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
        protected float totalSharpnessHoned = 0;
        protected ILoadedSound honingScrape;

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

        public void DoneSharpening() {
            totalSharpnessHoned = 0;
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
    }
}
