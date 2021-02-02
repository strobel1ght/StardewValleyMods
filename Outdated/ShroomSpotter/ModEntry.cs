using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace ShroomSpotter
{
    public class ModEntry : Mod
    {
        private ModConfig Config;

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
            Helper.Events.Input.ButtonPressed += ButtonPressed;
            Helper.Events.Display.RenderingActiveMenu += RenderingActiveMenu;
            Helper.Events.Display.RenderedActiveMenu += RenderedActiveMenu;
        }

        #region Events

        private void ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // Check the pressed button
            if (e.Button != Config.GetShroomLevels) return;

            // Find all shroom levels
            var shroomLevels = new List<int>();
            var daysTilShroom = -1;
            while (shroomLevels.Count == 0 && ++daysTilShroom < 50) shroomLevels = GetShroomLayers(daysTilShroom);
            if (shroomLevels.Count > 0)
            {
                if (daysTilShroom == 0)
                    Game1.showGlobalMessage("Shroom layers will spawn on these mine levels: " +
                                            string.Join(", ", shroomLevels));
                else
                    Game1.showGlobalMessage("Shrooms will spawn in " + daysTilShroom +
                                            " day(s) on these mine levels: " + string.Join(", ", shroomLevels));
            }
            else
            {
                Game1.showGlobalMessage("No shroom layers will spawn in the next 50 days!");
            }
        }

        private void RenderingActiveMenu(object sender, RenderingActiveMenuEventArgs e)
        {
            // Only render shrooms on the billboard
            if (!(Game1.activeClickableMenu is Billboard menu)) return;

            // Try to get the calendar field
            if (!(Helper.Reflection.GetField<List<ClickableTextureComponent>>(menu, nameof(Billboard.calendarDays))
                ?.GetValue() is List<ClickableTextureComponent> calendarDays)) return;

            // Get the current hover text
            var hoverField = Helper.Reflection.GetField<string>(menu, "hoverText");
            var hoverText = hoverField.GetValue();

            // Make sure the current hover text doesn't already have mushroom information
            if (hoverText.Contains("Shrooms") || hoverText.Contains("shrooms")) return;

            // Update the hover text
            var mouseX = Game1.getMouseX();
            var mouseY = Game1.getMouseY();
            for (var day = 1; day <= 28; day++)
            {
                // Check if the mouse is over the current calendar day
                var component = calendarDays[day - 1];
                if (!component.bounds.Contains(mouseX, mouseY)) continue;

                // Add any mushroom text
                var shrooms = GetShroomLayers(day - Game1.dayOfMonth);
                if (hoverText.Length > 0) hoverText += "\n";
                if (shrooms.Count > 0) hoverText += "Shrooms: " + string.Join(", ", shrooms);
                else hoverText += "No shrooms";
                break;
            }

            hoverField.SetValue(hoverText);
        }

        private void RenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            // Only render shrooms on the billboard
            if (!(Game1.activeClickableMenu is Billboard menu)) return;

            // Try to get the calendar field
            if (!(Helper.Reflection.GetField<List<ClickableTextureComponent>>(menu, nameof(Billboard.calendarDays))
                ?.GetValue() is List<ClickableTextureComponent> calendarDays)) return;

            // Get the current hover text
            var hoverText = Helper.Reflection.GetField<string>(menu, "hoverText").GetValue();

            // Draw the shrooms on the calendar
            for (var day = 1; day <= 28; day++)
            {
                var component = calendarDays[day - 1];
                var shrooms = GetShroomLayers(day - Game1.dayOfMonth);

                // Check if a shroom layer exists on this day
                if (shrooms.Count <= 0) continue;

                // Draw the shroom
                var source = GameLocation.getSourceRectForObject(422);
                var dest = new Vector2(component.bounds.Right - 8f * Game1.pixelZoom, component.bounds.Y);
                e.SpriteBatch.Draw(Game1.objectSpriteSheet, dest, source, Color.White, 0.0f, Vector2.Zero,
                    Game1.pixelZoom / 2f, SpriteEffects.None, 1f);
            }

            // Redraw the hover text so it appears over the mushrooms
            IClickableMenu.drawHoverText(e.SpriteBatch, hoverText, Game1.dialogueFont);
        }

        #endregion

        private static List<int> GetShroomLayers(int relativeDay)
        {
            var shroomLevels = new List<int>();
            for (var mineLevel = 1; mineLevel < 120; mineLevel++)
            {
                var skipMushroomCheck = false;
                if (mineLevel % 5 == 0)
                    // skip elevator floors for everything
                    continue;

                // Monster infestation seems to override mushroom spawns so that is checked first
                var random = new Random((int) Game1.stats.DaysPlayed + relativeDay + mineLevel * 100 +
                                        (int) Game1.uniqueIDForThisGame / 2);
                if (random.NextDouble() < 0.044 && mineLevel % 40 > 5 && mineLevel % 40 < 30 && mineLevel % 40 != 19)
                {
                    if (random.NextDouble() < 0.5)
                    {
                    }

                    skipMushroomCheck = true;
                }
                else if (random.NextDouble() < 0.044 && Game1.player.mailReceived.Contains("ccCraftsRoom") &&
                         mineLevel % 40 > 1)
                {
                    if (random.NextDouble() < 0.25)
                    {
                    }

                    skipMushroomCheck = true;
                }

                if (skipMushroomCheck) continue;

                // Reset the seed for checking Mushrooms. Note, there are a couple checks related to
                // darker than normal lighting. We don't care about the results but need to mimic them.
                random = new Random(((int) Game1.stats.DaysPlayed + relativeDay) * mineLevel + 4 * mineLevel +
                                    (int) Game1.uniqueIDForThisGame / 2);
                if (random.NextDouble() < 0.3 && mineLevel > 2) random.NextDouble(); // checked vs < 0.3 again
                random.NextDouble(); // checked vs < 0.15
                if (random.NextDouble() < 0.035 && mineLevel > 80) shroomLevels.Add(mineLevel);
            }

            return shroomLevels;
        }
    }
}