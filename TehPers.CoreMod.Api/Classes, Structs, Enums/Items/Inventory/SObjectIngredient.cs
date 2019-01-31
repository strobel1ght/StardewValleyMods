﻿using System;
using Microsoft.Xna.Framework;
using StardewValley;
using TehPers.CoreMod.Api.Drawing.Sprites;
using TehPers.CoreMod.Api.Items.Recipes;
using SObject = StardewValley.Object;

namespace TehPers.CoreMod.Api.Items.Inventory {
    public class SObjectIngredient : IIngredient {
        private readonly int _index;

        public int Quantity { get; }
        public ISprite Sprite { get; }

        public SObjectIngredient(ICoreApi coreApi, int index, int quantity = 1) {
            this._index = index;
            this.Quantity = quantity;
            this.Sprite = coreApi.Drawing.ObjectSpriteSheet.TryGetSprite(index, out ISprite sprite) ? sprite : throw new ArgumentOutOfRangeException(nameof(index));
        }

        public bool Matches(Item item) {
            return item is SObject obj && !obj.bigCraftable.Value && obj.ParentSheetIndex == this._index;
        }

        public string GetDisplayName() {
            return new SObject(Vector2.Zero, this._index, 1).DisplayName;
        }
    }
}