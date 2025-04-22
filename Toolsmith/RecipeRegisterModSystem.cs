using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Config;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
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
            List<GridRecipe> toolRecipes = new List<GridRecipe>();
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

                        toolRecipes.AddRange(GenerateToolGridRecipes(api, ingredient, tool));

                        if (!TinkerToolGridRecipes.ContainsKey(ingredient.Code.ToString())) {
                            TinkerToolGridRecipes.Add(ingredient.Code.ToString(), tool);
                            if (ToolsmithModSystem.Config.PrintAllParsedToolsAndParts) {
                                ToolsmithModSystem.Logger.Debug(ingredient.Code.ToString());
                            }
                        }
                    }
                }
            }
            var handleRecipes = GenerateHandleRecipes(api);

            //Make sure to clean up the four lists that were used in all this here! Would be nice not to leave that overhead information when it likely won't be needed after this point.
            ToolsmithModSystem.HandleList = null;
            ToolsmithModSystem.BindingList = null;
            ToolsmithModSystem.GripList = null;
            ToolsmithModSystem.BindingList = null;

            if (toolRecipes != null && toolRecipes.Count > 0) {
                api.World.GridRecipes.AddRange(toolRecipes);
            }
            if (handleRecipes != null && handleRecipes.Count > 0) {
                api.World.GridRecipes.AddRange(handleRecipes);
            }
        }

        private List<GridRecipe> GenerateToolGridRecipes(ICoreAPI api, GridRecipeIngredient head, CollectibleObject tool) {
            var list = new List<GridRecipe>();
            foreach (var handle in ToolsmithModSystem.HandleList) {
                foreach (var binding in ToolsmithModSystem.BindingList) {
                    var recipe = new GridRecipe {
                        IngredientPattern = "hb,r_",
                        Width = 2,
                        Height = 2,
                        Ingredients = new Dictionary<string, CraftingRecipeIngredient> {
                            ["h"] = head.Clone(),
                            ["b"] = new CraftingRecipeIngredient { Type = binding.ItemClass, Code = binding.Code },
                            ["r"] = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code }
                        },
                        RecipeGroup = 2,
                        ShowInCreatedBy = false,
                        Name = "Craft a " + tool.Code + " from a " + handle.Code + " and " + binding.Code + ".",
                        Output = new CraftingRecipeIngredient { Type = tool.ItemClass, Code = tool.Code }
                    };
                    recipe.ResolveIngredients(api.World);
                    list.Add(recipe);
                }
            }

            //Do the recipes need to be resolved here? Or will the server handle it automatically after this concludes?
            if (list.Count > 0) {
                return list;
            } else {
                return null;
            }
        }

        private List<GridRecipe> GenerateHandleRecipes(ICoreAPI api) {
            var list = new List<GridRecipe>();
            foreach (var handle in ToolsmithModSystem.HandleList) { //For every handle base that was found...
                if (handle.Code.Path == "stick" || handle.Code.Path == "bone") {
                    continue;
                }
                HandleStatPair handlesStats = ToolsmithModSystem.Config.BaseHandleRegistry.Get<string, HandleStatPair>(handle.Code.Path); //Grab the stat pair that should be registered in the configs here.
                if (handlesStats != null) { //Just in case, ensure it was found!
                    //Check the stats of the handle found, see what recipes are required to make for this one. Grip? Treatment? Both or neither?
                    //If Grip is allowed on this handle, generate a recipe with each of the Grip ingredients and add to list. Leave the actual assigning of attributes to the Handle Behavior's OnCreatedByCrafting call
                    if (handlesStats.canHaveGrip) {
                        foreach (var gripMat in ToolsmithModSystem.GripList) {
                            var recipe = new GridRecipe {
                                IngredientPattern = "hg",
                                Width = 2,
                                Height = 1,
                                Ingredients = new Dictionary<string, CraftingRecipeIngredient> {
                                    ["h"] = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code },
                                    ["g"] = new CraftingRecipeIngredient { Type = gripMat.ItemClass, Code = gripMat.Code }
                                },
                                RecipeGroup = 2,
                                ShowInCreatedBy = false,
                                Name = "Add " + gripMat.Code + " as a handle grip.",
                                Output = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code }
                            };
                            recipe.ResolveIngredients(api.World);
                            list.Add(recipe);
                        }
                    }

                    //Same for Treatments here! Add to the list afterwards!
                    if (handlesStats.canBeTreated) {
                        var bucket = api.World.GetBlock(new AssetLocation("game:woodbucket"));
                        foreach (var treatmentMat in ToolsmithModSystem.TreatmentList) {
                            var treatmentStats = ToolsmithModSystem.Config.TreatmentRegistry.Get(treatmentMat.Code.Path);
                            if (treatmentStats != null && !treatmentStats.isLiquid) {
                                var recipe = new GridRecipe {
                                    IngredientPattern = "ht",
                                    Width = 2,
                                    Height = 1,
                                    Ingredients = new Dictionary<string, CraftingRecipeIngredient> {
                                        ["h"] = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code },
                                        ["t"] = new CraftingRecipeIngredient { Type = treatmentMat.ItemClass, Code = treatmentMat.Code }
                                    },
                                    RecipeGroup = 3,
                                    ShowInCreatedBy = false,
                                    Name = "Add " + treatmentMat.Code + " as a handle treatment.",
                                    Output = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code }
                                };
                                recipe.ResolveIngredients(api.World);
                                list.Add(recipe);
                            } else if (treatmentStats != null && bucket != null) {
                                var recipe = new GridRecipe {
                                    IngredientPattern = "ht",
                                    Width = 2,
                                    Height = 1,
                                    Ingredients = new Dictionary<string, CraftingRecipeIngredient> {
                                        ["h"] = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code },
                                        ["t"] = new CraftingRecipeIngredient { Type = bucket.ItemClass, Code = bucket.Code }
                                    },
                                    Attributes = new JsonObject(JToken.Parse($"liquidContainerProps: {{ requiresContent: {{ type: {treatmentMat.ItemClass}, code: {treatmentMat.Code} }}, requiresLitres: {treatmentStats.litersUsed}}}")),
                                    RecipeGroup = 3,
                                    ShowInCreatedBy = false,
                                    Name = "Add " + treatmentMat.Code + " as a handle treatment.",
                                    Output = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code }
                                };
                                recipe.ResolveIngredients(api.World);
                                list.Add(recipe);
                            }
                        }
                    }
                }
            }

            //Do the recipes need to be resolved here? Or will the server handle it automatically after this concludes?
            if (list.Count > 0) {
                return list;
            } else {
                return null;
            }
        }

        public override void Dispose() {
            TinkerToolGridRecipes = null;
            base.Dispose();
        }
    }
}
