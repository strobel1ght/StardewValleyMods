﻿using Entoarox.Framework;
using Entoarox.Framework.ContentManager;
using Entoarox.Framework.Extensions;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.IO;
using TehPers.Stardew.DataInjector.NBT;

namespace TehPers.Stardew.DataInjector {
    public class ModEntry : Mod {
        internal static ModEntry INSTANCE;

        public List<ModEvent> Events = new List<ModEvent>();
        public ContentMerger merger;
        public ModConfig config;

        private List<Type> injectedTypes = new List<Type>();
        private CustomContentManager tmpManager;

        private bool loaded = false;

        public ModEntry() {
            if (INSTANCE == null) INSTANCE = this;
        }

        public override void Entry(IModHelper helper) {
            config = helper.ReadConfig<ModConfig>();
            if (!config.ModEnabled) return;

            GameEvents.UpdateTick += UpdateTick;
            LocationEvents.CurrentLocationChanged += CurrentLocationChanged;
            EntoFramework.onContentInit += onContentInit;

            // Testing NBT

            // Writing
            NBTTagCompound tag = new NBTTagCompound();
            tag.Set("Name", "Test");
            tag.Set("Success", (byte) 1);

            using (FileStream stream = new FileStream(Path.Combine(Helper.DirectoryPath, "test.dat"), FileMode.OpenOrCreate, FileAccess.Write)) {
                NBTBase.WriteStream(stream, tag);
            }

            // Reading
            using (FileStream stream = new FileStream(Path.Combine(Helper.DirectoryPath, "test.dat"), FileMode.Open, FileAccess.Read)) {
                tag = (NBTBase.ReadStream(stream) as NBTTagCompound) ?? null;
            }
            
        }

        private void onContentInit() {
            this.Monitor.Log("Loading content injector");
            this.merger = this.merger ?? new ContentMerger(Path.Combine(this.Helper.DirectoryPath, "Content"));
            SmartContentManager.ContentHandlers.Add(this.merger);
        }

        #region Event Handlers
        private void UpdateTick(object sender, EventArgs e) {
            if (!loaded && Game1.content != null && Game1.content.GetType() == typeof(LocalizedContentManager)) {
                this.merger = this.merger ?? new ContentMerger(Path.Combine(this.Helper.DirectoryPath, "Content"));
                tmpManager = tmpManager ?? new CustomContentManager(Game1.content.RootDirectory, Game1.content.ServiceProvider);
                Game1.content = tmpManager;
            } else if (!loaded && Game1.content is SmartContentManager) {
                this.loaded = true;
                this.reloadContent();

                this.Monitor.Log("Loading event injections");
                this.loadEvents();
            }
        }

        private void CurrentLocationChanged(object sender, EventArgsCurrentLocationChanged e) {
            GameLocation loc = e.NewLocation;
            if (!Game1.killScreen && Game1.farmEvent == null && loc.currentEvent == null) {
                foreach (ModEvent ev in this.Events) {
                    int? id = ev.getID();
                    if (id == null) continue;
                    if (id < 0) continue;
                    if (ev.Location == loc.name) {
                        this.Monitor.Log(id.ToString(), LogLevel.Trace);
                        if (ev.Repeatable) Game1.player.eventsSeen.Remove((int) id);
                        int eventID = -1;

                        try {
                            eventID = this.Helper.Reflection.GetPrivateMethod(loc, "checkEventPrecondition").Invoke<int>(ev.Condition);
                        } catch {
                            this.Monitor.Log("Failed to check condition for event " + id + "(at '" + loc.name + "')", LogLevel.Error);
                        }

                        if (eventID != -1) {
                            loc.currentEvent = new Event(ev.Data, eventID);

                            if (Game1.player.getMount() != null) {
                                loc.currentEvent.playerWasMounted = true;
                                Game1.player.getMount().dismount();
                            }
                            foreach (NPC character in loc.characters)
                                character.clearTextAboveHead();
                            Game1.eventUp = true;
                            Game1.displayHUD = false;
                            Game1.player.CanMove = false;
                            Game1.player.showNotCarrying();

                            IPrivateField<List<Critter>> crittersF = this.Helper.Reflection.GetPrivateField<List<Critter>>(loc, "critters");
                            List<Critter> critters = crittersF.GetValue();
                            if (critters == null)
                                return;
                            critters.Clear();
                            crittersF.SetValue(critters);
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        #region Loaders
        private void loadEvents() {
            string path = Path.Combine(this.Helper.DirectoryPath, "Events");

            if (!Directory.Exists(path)) {
                try {
                    this.Monitor.Log("Creating directory " + path);
                    Directory.CreateDirectory(path);
                } catch (Exception ex) {
                    this.Monitor.Log("Could not create directory " + path + "! Please create it yourself.", LogLevel.Error);
                    this.Monitor.Log(ex.Message, LogLevel.Error);
                    return;
                }
            }

            string[] configList = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string configPath in configList) {
                try {
                    EventConfig config = this.Helper.ReadJsonFile<EventConfig>(configPath);
                    if (config != null && config.Enabled) {
                        this.Monitor.Log("Loading " + Path.GetFileName(configPath), LogLevel.Info);
                        this.Events.AddRange(config.Events);
                    }
                    this.Helper.WriteJsonFile(configPath, config);
                } catch (Exception ex) {
                    this.Monitor.Log("Failed to load " + Path.GetFileName(configPath) + ".", LogLevel.Error);
                    this.Monitor.Log(ex.Message, LogLevel.Error);
                    this.Monitor.Log("Maybe your format is invalid?", LogLevel.Warn);
                }
            }
        }

        private void reloadContent() {
            Game1.daybg = Game1.content.Load<Texture2D>("LooseSprites\\daybg");
            Game1.nightbg = Game1.content.Load<Texture2D>("LooseSprites\\nightbg");
            Game1.menuTexture = Game1.content.Load<Texture2D>("Maps\\MenuTiles");
            Game1.lantern = Game1.content.Load<Texture2D>("LooseSprites\\Lighting\\lantern");
            Game1.windowLight = Game1.content.Load<Texture2D>("LooseSprites\\Lighting\\windowLight");
            Game1.sconceLight = Game1.content.Load<Texture2D>("LooseSprites\\Lighting\\sconceLight");
            Game1.cauldronLight = Game1.content.Load<Texture2D>("LooseSprites\\Lighting\\greenLight");
            Game1.indoorWindowLight = Game1.content.Load<Texture2D>("LooseSprites\\Lighting\\indoorWindowLight");
            Game1.shadowTexture = Game1.content.Load<Texture2D>("LooseSprites\\shadow");
            Game1.mouseCursors = Game1.content.Load<Texture2D>("LooseSprites\\Cursors");
            Game1.animations = Game1.content.Load<Texture2D>("TileSheets\\animations");
            Game1.achievements = Game1.content.Load<Dictionary<int, string>>("Data\\Achievements");
            Game1.eventConditions = Game1.content.Load<Dictionary<string, bool>>("Data\\eventConditions");
            Game1.NPCGiftTastes = Game1.content.Load<Dictionary<string, string>>("Data\\NPCGiftTastes");
            Game1.dialogueFont = Game1.content.Load<SpriteFont>("Fonts\\SpriteFont1");
            Game1.smallFont = Game1.content.Load<SpriteFont>("Fonts\\SmallFont");
            Game1.borderFont = Game1.content.Load<SpriteFont>("Fonts\\BorderFont");
            Game1.tinyFont = Game1.content.Load<SpriteFont>("Fonts\\tinyFont");
            Game1.tinyFontBorder = Game1.content.Load<SpriteFont>("Fonts\\tinyFontBorder");
            Game1.smoothFont = Game1.content.Load<SpriteFont>("Fonts\\smoothFont");
            Game1.objectSpriteSheet = Game1.content.Load<Texture2D>("Maps\\springobjects");
            Game1.toolSpriteSheet = Game1.content.Load<Texture2D>("TileSheets\\tools");
            Game1.cropSpriteSheet = Game1.content.Load<Texture2D>("TileSheets\\crops");
            Game1.emoteSpriteSheet = Game1.content.Load<Texture2D>("TileSheets\\emotes");
            Game1.debrisSpriteSheet = Game1.content.Load<Texture2D>("TileSheets\\debris");
            Game1.bigCraftableSpriteSheet = Game1.content.Load<Texture2D>("TileSheets\\Craftables");
            Game1.rainTexture = Game1.content.Load<Texture2D>("TileSheets\\rain");
            Game1.buffsIcons = Game1.content.Load<Texture2D>("TileSheets\\BuffsIcons");
            Game1.objectInformation = Game1.content.Load<Dictionary<int, string>>("Data\\ObjectInformation");
            Game1.bigCraftablesInformation = Game1.content.Load<Dictionary<int, string>>("Data\\BigCraftablesInformation");
        }
        #endregion

        #region API
        /**
         * <summary>Attempts to inject data into the specified dictionary asset. Returns false if the asset is not a dictionary of the correct types</summary>
         * <param name="assetName">The asset to inject the data into</param>
         * <param name="key">The key to inject into the dictionary</param>
         * <param name="value">The data to inject into the key</param>
         **/
        public static bool MergeData<TKey, TVal>(string assetName, TKey key, TVal value) {
            ModEntry mod = ModEntry.INSTANCE;

            try {
                Dictionary<TKey, TVal> r = Game1.content.Load<Dictionary<TKey, TVal>>(assetName);
                if (!mod.merger.cache.ContainsKey(assetName))
                    mod.merger.cache[assetName] = r;
            } catch (Exception) {
                // Asset didn't exist
            }

            if (!mod.merger.cache.ContainsKey(assetName))
                mod.merger.cache.Add(assetName, new Dictionary<TKey, TVal>());

            Dictionary<TKey, TVal> dict = mod.merger.cache[assetName] as Dictionary<TKey, TVal>;
            if (dict == null) return false;
            dict[key] = value;

            return true;
        }

        public static bool MergeTexture(string assetName, Texture tex) {
            /*ModEntry mod = ModEntry.INSTANCE;

            try {
                T r = Game1.content.Load<T>(assetName);
                if (!mod.merger.cache.ContainsKey(assetName))
                    mod.merger.cache[assetName] = r;
            } catch (Exception ex) {
                // Asset didn't exist
            }

            if (!mod.merger.cache.ContainsKey(assetName) || mod.merger.cache[assetName].GetType().IsAssignableFrom(typeof(T)))
                mod.merger.cache[assetName] = obj;
            else
                return false;*/

            return true;
        }

        /**
         * <summary>Attempts to inject object as the specified asset. Returns false if an asset with that name exists and they are not compatible types</summary>
         * <param name="assetName">The asset to inject or override</param>
         * <param name="obj">The object to inject</param>
         **/
        public static bool Override<T>(string assetName, T obj) {
            ModEntry mod = INSTANCE;

            try {
                T r = Game1.content.Load<T>(assetName);
                if (!mod.merger.cache.ContainsKey(assetName))
                    mod.merger.cache[assetName] = r;
            } catch (Exception) {
                // Asset didn't exist
            }

            if (!mod.merger.cache.ContainsKey(assetName) || mod.merger.cache[assetName].GetType().IsAssignableFrom(typeof(T)))
                mod.merger.cache[assetName] = obj;
            else
                return false;

            return true;
        }
        #endregion
    }
}