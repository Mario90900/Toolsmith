using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace Toolsmith {
    public class RecipeRegisterModSystem : ModSystem {

        public static Dictionary<string, CollectibleObject> TinkerToolGridRecipes; //A Dictionary to give it the Tool Head's Code.ToString to retreive the Tool CollectibleObject it should create

        public override bool ShouldLoad(EnumAppSide forSide) {
            return forSide == EnumAppSide.Server;
        }

        public override double ExecuteOrder() {
            return 1;
        }

        public override void StartPre(ICoreAPI api) {
            TinkerToolGridRecipes = new Dictionary<string, CollectibleObject>();
        }

        public override void AssetsFinalize(ICoreAPI api) {
            base.AssetsFinalize(api);

            if (ToolsmithModSystem.Config.PrintAllParsedToolsAndParts) {
                ToolsmithModSystem.Logger.Debug("Tool Heads:");
            }
            //Oh god this pains me. This does NOT feel optimal at all. But it works?
            foreach (var recipe in api.World.GridRecipes) { //Check each recipe...
                foreach (var tool in ToolsmithModSystem.TinkerableToolsList.Where(t => recipe.Output.Code.Equals(t.Code))) { //Where the output code matches anything on the Tinkered Tool List (from the configs)...
                    foreach (var ingredient in recipe.resolvedIngredients.Where(i => (i != null) && (i.ResolvedItemstack != null) && (ConfigUtility.IsToolHead(i.ResolvedItemstack.Collectible?.Code.ToString())))) { //And the recipe in question has a Tool Head item that is on the Tool Head Config List
                        if (!ingredient.ResolvedItemstack.Collectible.HasBehavior<CollectibleBehaviorToolHead>()) {
                            ingredient.ResolvedItemstack.Collectible.AddBehavior<CollectibleBehaviorToolHead>(); //Therefore it is a Tool Head! Give it the behavior.
                        }

                        if (ConfigUtility.IsBluntTool(tool.Code)) { //If it is also a blunt tool, add the 'nodamage' Behavior as a tag to the Head as well
                            if (!ingredient.ResolvedItemstack.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                                ingredient.ResolvedItemstack.Collectible.AddBehavior<CollectibleBehaviorToolBlunt>();
                            }
                        }

                        if (!TinkerToolGridRecipes.ContainsKey(ingredient.Code.ToString())) {
                            TinkerToolGridRecipes.Add(ingredient.Code.ToString(), tool);
                            if (ToolsmithModSystem.Config.PrintAllParsedToolsAndParts) {
                                ToolsmithModSystem.Logger.Debug(ingredient.Code.ToString());
                            }
                        }
                    }
                }
            }
        }

        public override void Dispose() {
            TinkerToolGridRecipes = null;
            base.Dispose();
        }
    }
}
