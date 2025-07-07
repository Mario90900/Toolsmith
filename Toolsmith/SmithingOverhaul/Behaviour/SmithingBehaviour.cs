using SmithingOverhaul.Property;
using Toolsmith.SmithingOverhaul.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace SmithingOverhaul.Behaviour
{
    public class SmithingBehavior : CollectibleBehavior
    {
        public SmithingPropertyVariant metalProps;

        public SmithingBehavior(CollectibleObject collObj) : base(collObj)
        {
        }
        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            var metalcode = collObj.Variant["metal"];
            SmithingPropertyVariant var;

            if (api.ModLoader.GetModSystem<SmithingOverhaulModSystem>().metalPropsByCode.TryGetValue(metalcode, out var))
            {
                metalProps = var;
            }

        }

        public virtual void OnTemperatureEffect(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, float temperature, double hourDiff, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }
        public virtual void OnCoolingEffect(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, float tempDiff, double hourDiff, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }
        public virtual void OnHeatingEffect(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, float tempDiff, double hourDiff, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }
        public virtual bool OnCanWork(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, ref EnumHandling handling) 
        {
            handling = EnumHandling.PassThrough;
            return true;
        }

        public virtual void AfterOnHit(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, int voxelsChanged, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual void AfterOnUpset(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual void AfterOnSplit(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual void OnAddStrain(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, float changeInStrain, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual void OnRecoverStrain(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, float temperature, double hourDiff, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }
    }
}
