using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Toolsmith.Utils {
    public static class MathUtility {

        public static bool ShouldDamageHeadFromCurveChance(IWorldAccessor world, float howSharpPercent) {
            bool shouldDamage = false;
            //Thank you Wolfram Alpha for the formula
            var chanceToDamage = (11.0813 * (Math.Pow(howSharpPercent, 4))) - (32.4235 * (Math.Pow(howSharpPercent, 3))) + (36.4908 * (Math.Pow(howSharpPercent, 2))) - (19.6091 * howSharpPercent) + 4.52141;
            
            if (chanceToDamage >= 1.0) { //Clamp it just in case.
                return true;
            } else if (chanceToDamage <= 0.05) {
                chanceToDamage = 0.05;
            }

            shouldDamage = (world.Rand.NextDouble() <= chanceToDamage);

            return shouldDamage;
        }
    }
}
