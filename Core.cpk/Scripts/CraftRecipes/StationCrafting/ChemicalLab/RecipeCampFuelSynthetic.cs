﻿namespace AtomicTorch.CBND.CoreMod.CraftRecipes
{
    using System;
    using AtomicTorch.CBND.CoreMod.Items.Generic;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures.CraftingStations;
    using AtomicTorch.CBND.CoreMod.Systems;
    using AtomicTorch.CBND.CoreMod.Systems.Crafting;

    public class RecipeCampFuelSynthetic : Recipe.RecipeForStationCrafting
    {
        public override string Name => "Camp fuel (synthetic)";

        protected override void SetupRecipe(
            StationsList stations,
            out TimeSpan duration,
            InputItems inputItems,
            OutputItems outputItems)
        {
            stations.Add<ObjectChemicalLab>();

            duration = CraftingDuration.Short;

            inputItems.Add<ItemCanisterGasoline>(count: 3);
            inputItems.Add<ItemCanisterMineralOil>(count: 1);

            outputItems.Add<ItemCampFuel>(count: 5);
            outputItems.Add<ItemCanisterEmpty>(count: 4);
        }
    }
}