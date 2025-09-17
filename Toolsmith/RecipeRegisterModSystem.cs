using Newtonsoft.Json.Linq;
using SmithingPlus.Metal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Client;
using Toolsmith.Config;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace Toolsmith {
    public class RecipeRegisterModSystem : ModSystem {

        //A Dictionary to give it the Tool Head's Code.ToString to retreive the Tool CollectibleObject it should create
        public static Dictionary<string, CollectibleObject> TinkerToolGridRecipes;
        //A Dictionary to supply it with a tool head when looking to reforge it, getting the recipe back that produces the item to generate the WorkPiece similar to Smithing Plus
        public static Dictionary<string, SmithingRecipe> ToolHeadSmithingRecipes;
        //Textures are grabbed from the ToolHeads once found
        public static Dictionary<string, ToolHeadTextureData> ToolHeadTexturesCache;

        //This is instantiated and populated in the ToolsmithModSystem, then used here to parse through the recipes once they have loaded, nulled afterwards for space.
        public static List<CollectibleObject> TinkerableToolsList;

        public static List<CollectibleObject> HandleList; //Now populated to generate Grid Recipes with the tool heads, handles, and bindings. Is cleared afterwards to free space, so do not expect it to remain populated.
        public static List<CollectibleObject> BindingList; //^^^
        public static List<CollectibleObject> GripList; //^^^
        public static List<CollectibleObject> TreatmentList; //^^^
        public static List<CollectibleObject> LiquidContainers; //^^^

        public override bool ShouldLoad(EnumAppSide forSide) {
            return forSide == EnumAppSide.Server;
        }

        public override double ExecuteOrder() {
            return 1;
        }

        public override void StartPre(ICoreAPI api) {
            TinkerToolGridRecipes = new Dictionary<string, CollectibleObject>();
            ToolHeadSmithingRecipes = new Dictionary<string, SmithingRecipe>();
            ToolHeadTexturesCache = new Dictionary<string, ToolHeadTextureData>();
        }

        public override void AssetsFinalize(ICoreAPI api) {
            base.AssetsFinalize(api);

            if (ToolsmithModSystem.Config.PrintAllParsedToolsAndParts) {
                ToolsmithModSystem.Logger.Debug("Tool Heads:");
            }
            //Oh god this pains me. This does NOT feel optimal at all. But it works?
            List<GridRecipe> toolRecipes = new List<GridRecipe>();
            foreach (var recipe in api.World.GridRecipes) { //Check each recipe...
                foreach (var tool in TinkerableToolsList.Where(t => recipe.Output.Code.Equals(t.Code))) { //Where the output code matches anything on the Tinkered Tool List (from the configs)...
                    foreach (var ingredient in recipe.resolvedIngredients.Where(i => (i != null) && (i.ResolvedItemstack != null) && (ConfigUtility.IsToolHead(i.ResolvedItemstack.Collectible?.Code.ToString())))) { //And the recipe in question has a Tool Head item that is on the Tool Head Config List
                        if (!ingredient.ResolvedItemstack.Collectible.HasBehavior<CollectibleBehaviorToolHead>()) {
                            ingredient.ResolvedItemstack.Collectible.AddBehavior<CollectibleBehaviorToolHead>(); //Therefore it is a Tool Head! Give it the behavior.
                        }

                        if (ConfigUtility.IsBluntTool(tool.Code)) { //If it is also a blunt tool, add the 'nodamage' Behavior as a tag to the Head as well
                            if (!ingredient.ResolvedItemstack.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                                ingredient.ResolvedItemstack.Collectible.AddBehavior<CollectibleBehaviorToolBlunt>();
                            }
                        }

                        if (ToolsmithModSystem.Config.EnableGridRecipesForToolCrafting) {
                            toolRecipes.AddRange(GenerateToolGridRecipes(api, ingredient, tool));
                        }

                        var gridRecipeTag = ingredient.Code.ToString();
                        foreach (var otherIngredients in recipe.resolvedIngredients.Where(o => (o != null) && (o.ResolvedItemstack != null) && (o.ResolvedItemstack.Collectible.Code.Path == "bone"))) { //Is this one of the bone + head recipes?
                            if (otherIngredients.ResolvedItemstack.Collectible.Code == ToolsmithConstants.BoneHandleCode) {
                                gridRecipeTag += "-bone";
                                break;
                            }
                        }

                        if (!ToolHeadTexturesCache.ContainsKey(ingredient.Code)) {
                            if (ingredient.Type == EnumItemClass.Item) {
                                Item item = ingredient.ResolvedItemstack.Item;
                                ToolHeadTextureData textures = new ToolHeadTextureData();
                                foreach (var tex in item.Textures) {
                                    textures.Tags.Add(tex.Key);
                                    textures.Paths.Add(tex.Value.Base);
                                }
                                ToolHeadTexturesCache.Add(item.Code, textures);
                            } else {
                                Block block = ingredient.ResolvedItemstack.Block;
                                ToolHeadTextureData textures = new ToolHeadTextureData();
                                foreach (var tex in block.Textures) {
                                    textures.Tags.Add(tex.Key);
                                    textures.Paths.Add(tex.Value.Base);
                                }
                                ToolHeadTexturesCache.Add(block.Code, textures);
                            }
                        }

                        if (!TinkerToolGridRecipes.ContainsKey(gridRecipeTag)) {
                            TinkerToolGridRecipes.Add(gridRecipeTag, tool);
                            if (ToolsmithModSystem.Config.PrintAllParsedToolsAndParts) {
                                ToolsmithModSystem.Logger.Debug(ingredient.Code.ToString());
                            }
                        }
                    }
                }
            }

            var handleRecipes = GenerateHandleRecipes(api);
            var sandpaperRecipes = GenerateSandpaperRecipes(api);

            if (toolRecipes != null && toolRecipes.Count > 0) {
                api.World.GridRecipes.AddRange(toolRecipes);
            }
            if (handleRecipes != null && handleRecipes.Count > 0) {
                api.World.GridRecipes.AddRange(handleRecipes);
            }
            if (sandpaperRecipes != null && sandpaperRecipes.Count > 0) {
                api.World.GridRecipes.AddRange(sandpaperRecipes);
            }

            //Make sure to clean up the five lists that were used in all this here! Would be nice not to leave that overhead information when it likely won't be needed after this point.
            TinkerableToolsList = null;
            HandleList = null;
            BindingList = null;
            GripList = null;
            TreatmentList = null;
            LiquidContainers = null;

            toolRecipes = null;
            handleRecipes = null;
        }

        private List<GridRecipe> GenerateToolGridRecipes(ICoreAPI api, GridRecipeIngredient head, CollectibleObject tool) {
            var list = new List<GridRecipe>();
            foreach (var handle in HandleList) {
                foreach (var binding in BindingList) {
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

            if (list.Count > 0) {
                return list;
            } else {
                return null;
            }
        }

        private List<GridRecipe> GenerateHandleRecipes(ICoreAPI api) {
            var list = new List<GridRecipe>();
            foreach (var handle in HandleList) { //For every handle base that was found...
                //if (handle.Code.Path == "stick" || handle.Code.Path == "bone") {
                //    continue;
                //}
                HandlePartDefines handlesStats = ToolsmithModSystem.Stats.BaseHandleParts.TryGetValue(handle.Code.Path); //Grab the stat pair that should be registered in the configs here.
                if (handlesStats != null) { //Just in case, ensure it was found!
                    //Check the stats of the handle found, see what recipes are required to make for this one. Grip? Treatment? Both or neither?
                    //If Grip is allowed on this handle, generate a recipe with each of the Grip ingredients and add to list. Leave the actual assigning of attributes to the Handle Behavior's OnCreatedByCrafting call
                    if (handlesStats.canHaveGrip) {
                        foreach (var gripMat in GripList) {
                            var recipe = new GridRecipe {
                                IngredientPattern = "hg",
                                Width = 2,
                                Height = 1,
                                Ingredients = new Dictionary<string, CraftingRecipeIngredient> {
                                    ["h"] = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code },
                                    ["g"] = new CraftingRecipeIngredient { Type = gripMat.ItemClass, Code = gripMat.Code }
                                },
                                RecipeGroup = 2,
                                ShowInCreatedBy = true,
                                Shapeless = true,
                                Name = "Add " + gripMat.Code + " as a handle grip.",
                                Output = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code }
                            };
                            recipe.ResolveIngredients(api.World);
                            list.Add(recipe);
                        }
                    }

                    //Same for Treatments here! Add to the list afterwards!
                    if (handlesStats.canBeTreated) {
                        foreach (var treatmentMat in TreatmentList) {
                            var treatmentStats = ToolsmithModSystem.Stats.TreatmentParts.Get(treatmentMat.Code.Path);
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
                                    ShowInCreatedBy = true,
                                    Shapeless = true,
                                    Name = "Add " + treatmentMat.Code + " as a handle treatment.",
                                    Output = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code }
                                };
                                recipe.ResolveIngredients(api.World);
                                list.Add(recipe);
                            } else if (treatmentStats != null && LiquidContainers.Count > 0) {
                                ITreeAttribute liquidProps = new TreeAttribute();
                                var liqProps = liquidProps.GetOrAddTreeAttribute("liquidContainerProps");
                                var reqCont = liqProps.GetOrAddTreeAttribute("requiresContent");
                                reqCont.SetString("type", "item");
                                reqCont.SetString("code", treatmentMat.Code);
                                liqProps.SetFloat("requiresLitres", treatmentStats.litersUsed);
                                
                                foreach (var container in LiquidContainers) {
                                    var bucketRecipe = new GridRecipe {
                                        IngredientPattern = "ht",
                                        Width = 2,
                                        Height = 1,
                                        Ingredients = new Dictionary<string, CraftingRecipeIngredient> {
                                            ["h"] = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code },
                                            ["t"] = new CraftingRecipeIngredient { Type = container.ItemClass, Code = container.Code }
                                        },
                                        Attributes = new JsonObject(JToken.Parse(liquidProps.ToJsonToken())),//new JsonObject(JToken.Parse("{liquidContainerProps: {requiresContent: {type: \"item\", code: \"" + treatmentMat.Code + "\" }, requiresLitres: " + treatmentStats.litersUsed + "}}")),
                                        RecipeGroup = 3,
                                        ShowInCreatedBy = true,
                                        Shapeless = true,
                                        Name = "Add " + treatmentMat.Code + " as a handle treatment.",
                                        Output = new CraftingRecipeIngredient { Type = handle.ItemClass, Code = handle.Code }
                                    };

                                    bucketRecipe.ResolveIngredients(api.World);
                                    list.Add(bucketRecipe);
                                }
                            }
                        }
                    }
                }
            }

            if (list.Count > 0) {
                return list;
            } else {
                return null;
            }
        }

        private List<GridRecipe> GenerateSandpaperRecipes(ICoreAPI api) {
            var list = new List<GridRecipe>();
            ITreeAttribute liquidProps = new TreeAttribute();
            var liqProps = liquidProps.GetOrAddTreeAttribute("liquidContainerProps");
            var reqCont = liqProps.GetOrAddTreeAttribute("requiresContent");
            reqCont.SetString("type", "item");
            reqCont.SetString("code", "game:glueportion-pitch-hot");
            liqProps.SetFloat("requiresLitres", 0.5f);

            foreach (var container in LiquidContainers) {
                var containerRecipe = new GridRecipe {
                    IngredientPattern = "SG,P_",
                    Width = 2,
                    Height = 2,
                    Ingredients = new Dictionary<string, CraftingRecipeIngredient> {
                        ["S"] = new CraftingRecipeIngredient { Type = EnumItemClass.Block, Code = new AssetLocation("game:sand-*") },
                        ["G"] = new CraftingRecipeIngredient { Type = container.ItemClass, Code = container.Code },
                        ["P"] = new CraftingRecipeIngredient { Type = EnumItemClass.Item, Code = new AssetLocation("game:paper-parchment") }
                    },
                    Attributes = new JsonObject(JToken.Parse(liquidProps.ToJsonToken())),
                    ShowInCreatedBy = true,
                    Name = "Make sandpaper from Pitch Glue in a " + container.Code + ".",
                    Output = new CraftingRecipeIngredient { Type = EnumItemClass.Item, Code = new AssetLocation("toolsmith:sandpaper"), Quantity = 4 }
                };

                containerRecipe.ResolveIngredients(api.World);
                list.Add(containerRecipe);
            }

            if (list.Count > 0) {
                return list;
            } else {
                return null;
            }
        }

        public override void Dispose() {
            TinkerToolGridRecipes = null;
            ToolHeadSmithingRecipes = null;
            TinkerableToolsList = null;
            ToolHeadTexturesCache = null;
            base.Dispose();
        }
    }
}
