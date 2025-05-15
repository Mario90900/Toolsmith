using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Toolsmith.ToolTinkering.Items {

    public class ItemWorkableNugget : ItemNugget, IAnvilWorkable {

        public bool CanWork(ItemStack stack) {
            throw new NotImplementedException();
        }

        public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack) {
            throw new NotImplementedException();
        }

        public int GetRequiredAnvilTier(ItemStack stack) {
            throw new NotImplementedException();
        }

        public ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil) {
            throw new NotImplementedException();
        }

        public ItemStack GetBaseMaterial(ItemStack stack) {
            throw new NotImplementedException();
        }

        public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil) {
            return EnumHelveWorkableMode.NotWorkable;
        }
    }
}
