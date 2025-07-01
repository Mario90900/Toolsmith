using SmithingOverhaul.BlockEntity;
using SmithingOverhaul.Property;
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

        public virtual float GetTemperature(IWorldAccessor world, ItemStack stack, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return 20f;
        }

        public virtual float GetTemperature(IWorldAccessor world, ItemStack stack, double didReceiveHeat, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return 20f;
        }

        public virtual void SetTemperature(IWorldAccessor world, ItemStack stack, float temperature, bool delayCooldown, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }
        public virtual bool CanWork(IWorldAccessor world, ItemStack stack, ref EnumHandling handling) 
        {
            handling = EnumHandling.PassThrough;
            return true;
        }

        public virtual void AfterOnHit(int voxelsChanged, ItemStack stack, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual void AfterOnUpset(ItemStack stack, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual void AfterOnSplit(ItemStack stack, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual float AddStrain(float changeInStrain, ItemStack stack, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return 20f;
        }

        public virtual void RecoverStrain(float temperature, ItemStack stack, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual bool IsOverstrained(ItemStack stack, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return false;
        }


    }
}
