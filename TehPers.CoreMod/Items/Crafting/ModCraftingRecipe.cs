﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using TehPers.CoreMod.Api.Items.Inventory;
using TehPers.CoreMod.Api.Items.Recipes;
using TehPers.CoreMod.Items.Inventory;

namespace TehPers.CoreMod.Items.Crafting {
    internal class ModCraftingRecipe : CustomCraftingRecipe {
        private static readonly FieldInfo _descriptionField = typeof(CraftingRecipe).GetField("description", BindingFlags.Instance | BindingFlags.NonPublic);

        public override int ComponentWidth { get; }
        public override int ComponentHeight { get; }
        public override IRecipe Recipe { get; }

        public ModCraftingRecipe(string name, IRecipe recipe, bool isCookingRecipe) : base("Torch", isCookingRecipe) {
            this.Recipe = recipe;
            this.ComponentWidth = (int) Math.Ceiling(recipe.Sprite.Width / 16f);
            this.ComponentHeight = (int) Math.Ceiling(recipe.Sprite.Height / 16f);

            // Recipe details
            this.name = name;
            this.DisplayName = recipe.GetDisplayName();
            ModCraftingRecipe._descriptionField.SetValue(this, recipe.GetDescription());
            this.timesCrafted = Game1.player.craftingRecipes.ContainsKey(name) ? Game1.player.craftingRecipes[name] : 0;

            // TODO: not sure what to do here, maybe this should just be 1 unless there's a single result?
            this.numberProducedPerCraft = recipe.Results.Sum(result => result.Quantity);
        }
    }
}