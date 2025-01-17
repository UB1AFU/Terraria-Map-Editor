﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using TEdit.Geometry.Primitives;
using GalaSoft.MvvmLight;
using TEditXna.Editor.Clipboard;
using XNA = Microsoft.Xna.Framework;
using TEditXNA.Terraria.Objects;

namespace TEditXNA.Terraria
{
    public partial class World
    {
        
        private static readonly Dictionary<string, XNA.Color> _globalColors = new Dictionary<string, XNA.Color>();
        private static readonly Dictionary<string, int> _npcIds = new Dictionary<string, int>();
        private static readonly Dictionary<int, int> _npcFrames = new Dictionary<int, int>();
        private static readonly Dictionary<byte, string> _prefix = new Dictionary<byte, string>();
        private static readonly ObservableCollection<ItemProperty> _itemProperties = new ObservableCollection<ItemProperty>();
        private static readonly ObservableCollection<ChestProperty> _chestProperties = new ObservableCollection<ChestProperty>();
        private static readonly ObservableCollection<SignProperty> _signProperties = new ObservableCollection<SignProperty>();
        private static readonly ObservableCollection<TileProperty> _tileProperties = new ObservableCollection<TileProperty>();
        private static readonly ObservableCollection<TileProperty> _tileBricks = new ObservableCollection<TileProperty>();
        private static readonly ObservableCollection<WallProperty> _wallProperties = new ObservableCollection<WallProperty>();

        private static readonly ObservableCollection<Sprite> _sprites = new ObservableCollection<Sprite>();
        private static readonly Dictionary<Key, string> _shortcuts = new Dictionary<Key, string>();
        private static readonly Dictionary<int, ItemProperty> _itemLookup = new Dictionary<int, ItemProperty>();
        private static readonly Dictionary<int, string> _tallynames = new Dictionary<int, string>();
        private static Vector2 _appSize;
        internal static string AltC;
        internal static int? SteamUserId;
 
        static World()
        {
            if (ViewModelBase.IsInDesignModeStatic) return;

            var settingspath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase), "settings.xml");
            LoadObjectDbXml(settingspath);
            Sprites.Add(new Sprite());

            if (TileFrameImportant == null || TileFrameImportant.Length <= 0)
            {
                TileFrameImportant = new bool[TileCount];
                for (int i = 0; i < TileCount; i++)
                {
                    if (World.TileProperties.Count > i)
                    {
                        TileFrameImportant[i] = World.TileProperties[i].IsFramed;
                    }
                }
            }
        }

        private static IEnumerable<TOut> StringToList<TOut>(string xmlcsv)
        {
            if (!string.IsNullOrWhiteSpace(xmlcsv))
            {
                string[] split = xmlcsv.Split(',');
                foreach (var s in split)
                {
                    yield return (TOut)Convert.ChangeType(s, typeof(TOut));
                }
            }
        }

        private static T InLineEnumTryParse<T>(string str) where T : struct
        {
            T result;
            Enum.TryParse(str, true, out result);
            return result;
        }

        private static Vector2Short StringToVector2Short(string v, short defaultX = 0, short defaultY = 0)
        {
            if (!string.IsNullOrWhiteSpace(v))
            {

                short x = 0;
                short y = 0;
                var split = v.Split(',');
                if (split.Length == 2)
                {
                    if (short.TryParse(split[0], out x) && short.TryParse(split[1], out y))
                        return new Vector2Short(x, y);
                }
            }

            return new Vector2Short(defaultX, defaultY);
        }

        private static Color ColorFromString(string colorstring)
        {
            if (!string.IsNullOrWhiteSpace(colorstring))
            {
                var colorFromString = ColorConverter.ConvertFromString(colorstring);
                if (colorFromString != null)
                {
                    return (Color)colorFromString;
                }
            }
            return Colors.Magenta;
        }
        private static XNA.Color XnaColorFromString(string colorstring)
        {
            if (!string.IsNullOrWhiteSpace(colorstring))
            {
                var colorFromString = ColorConverter.ConvertFromString(colorstring);
                if (colorFromString != null)
                {
                    var c = (Color)colorFromString;
                    return XNA.Color.FromNonPremultiplied(c.R, c.G, c.B, c.A);
                }
            }
            return XNA.Color.Magenta;
        }

        private static void LoadObjectDbXml(string file)
        {
            var xmlSettings = XElement.Load(file);

            // Load Colors
            foreach (var xElement in xmlSettings.Elements("GlobalColors").Elements("GlobalColor"))
            {
                string name = (string)xElement.Attribute("Name");
                XNA.Color color = XnaColorFromString((string)xElement.Attribute("Color"));
                GlobalColors.Add(name, color);
            }

            foreach (var xElement in xmlSettings.Elements("Tiles").Elements("Tile"))
            {

                var curTile = new TileProperty();

                // Read XML attributes
                curTile.Color = ColorFromString((string)xElement.Attribute("Color"));
                curTile.Name = (string)xElement.Attribute("Name");
                curTile.Id = (int?)xElement.Attribute("Id") ?? 0;
                curTile.IsFramed = (bool?)xElement.Attribute("Framed") ?? false;
                curTile.IsSolid = (bool?)xElement.Attribute("Solid") ?? false;
                curTile.IsSolidTop = (bool?)xElement.Attribute("SolidTop") ?? false;
                curTile.IsLight = (bool?)xElement.Attribute("Light") ?? false;
                curTile.FrameSize = StringToVector2Short((string)xElement.Attribute("Size"), 1, 1);
                curTile.Placement = InLineEnumTryParse<FramePlacement>((string)xElement.Attribute("Placement"));
                curTile.TextureGrid = StringToVector2Short((string)xElement.Attribute("TextureGrid"), 16, 16);
                curTile.IsGrass = "Grass".Equals((string)xElement.Attribute("Special")); /* Heathtech */
                curTile.IsPlatform = "Platform".Equals((string)xElement.Attribute("Special")); /* Heathtech */
                curTile.IsCactus = "Cactus".Equals((string)xElement.Attribute("Special")); /* Heathtech */
                curTile.IsStone = (bool?)xElement.Attribute("Stone") ?? false; /* Heathtech */
                curTile.CanBlend = (bool?)xElement.Attribute("Blends") ?? false; /* Heathtech */
                curTile.MergeWith = (int?)xElement.Attribute("MergeWith") ?? null; /* Heathtech */
                foreach (var elementFrame in xElement.Elements("Frames").Elements("Frame"))
                {

                    var curFrame = new FrameProperty();
                    // Read XML attributes
                    curFrame.Name = (string)elementFrame.Attribute("Name");
                    curFrame.Variety = (string)elementFrame.Attribute("Variety");
                    curFrame.UV = StringToVector2Short((string)elementFrame.Attribute("UV"), 0, 0);
                    curFrame.Anchor = InLineEnumTryParse<FrameAnchor>((string)elementFrame.Attribute("Anchor"));

                    // Assign a default name if none existed
                    if (string.IsNullOrWhiteSpace(curFrame.Name))
                        curFrame.Name = curTile.Name;

                    curTile.Frames.Add(curFrame);
                    Sprites.Add(new Sprite
                                    {
                                        Anchor = curFrame.Anchor,
                                        IsPreviewTexture = false,
                                        Name = curFrame.Name + ", " + curFrame.Variety,
                                        Origin = curFrame.UV,
                                        Size = curTile.FrameSize,
                                        Tile = (ushort)curTile.Id, /* SBlogic */
                                        TileName = curTile.Name
                                    });
                }
                if (curTile.Frames.Count == 0 && curTile.IsFramed)
                {
                    var curFrame = new FrameProperty();
                    // Read XML attributes
                    curFrame.Name = curTile.Name;
                    curFrame.Variety = string.Empty;
                    curFrame.UV = new Vector2Short(0, 0);
                    //curFrame.Anchor = InLineEnumTryParse<FrameAnchor>((string)xElement.Attribute("Anchor"));

                    // Assign a default name if none existed
                    if (string.IsNullOrWhiteSpace(curFrame.Name))
                        curFrame.Name = curTile.Name;

                    curTile.Frames.Add(curFrame);
                    Sprites.Add(new Sprite
                                    {
                                        Anchor = curFrame.Anchor,
                                        IsPreviewTexture = false,
                                        Name = curFrame.Name + ", " + curFrame.Variety,
                                        Origin = curFrame.UV,
                                        Size = curTile.FrameSize,
                                        Tile = (ushort)curTile.Id,
                                        TileName = curTile.Name
                                    });
                }
                TileProperties.Add(curTile);
                if (!curTile.IsFramed)
                    TileBricks.Add(curTile);
            }
            for (int i = TileProperties.Count; i < 255; i++)
            {
                TileProperties.Add(new TileProperty(i, "UNKNOWN", Color.FromArgb(255, 255, 0, 255), true));
            }

            foreach (var xElement in xmlSettings.Elements("Walls").Elements("Wall"))
            {
                var curWall = new WallProperty();
                curWall.Color = ColorFromString((string)xElement.Attribute("Color"));
                curWall.Name = (string)xElement.Attribute("Name");
                curWall.Id = (int?)xElement.Attribute("Id") ?? -1;
                curWall.IsHouse = (bool?)xElement.Attribute("IsHouse") ?? false;
                WallProperties.Add(curWall);
            }

            foreach (var xElement in xmlSettings.Elements("Items").Elements("Item"))
            {
                var curItem = new ItemProperty();
                curItem.Id = (int?)xElement.Attribute("Id") ?? -1;
                curItem.Name = (string)xElement.Attribute("Name");
                ItemProperties.Add(curItem);
                _itemLookup.Add(curItem.Id, curItem);
                int tally = (int?)xElement.Attribute("Tally") ?? 0;
                if (tally > 0)
                    _tallynames.Add(tally, curItem.Name);
            }

            int chestId = 0;
            foreach (var tileElement in xmlSettings.Elements("Tiles").Elements("Tile"))
            {
                string tileName = (string)tileElement.Attribute("Name");
                if (tileName == "Chest" || tileName == "Dresser")
                {
                    ushort type = (ushort)((int?)tileElement.Attribute("Id") ?? 21);
                    foreach (var xElement in tileElement.Elements("Frames").Elements("Frame"))
                    {
                        var curItem = new ChestProperty();
                        curItem.Name = (string)xElement.Attribute("Name");
                        string variety = (string)xElement.Attribute("Variety");
                        if (variety != null)
                        {
                            if (tileName == "Dresser")
                            {
                                curItem.Name = variety + " " + "Dresser";
                            }
                            else
                            {
                                curItem.Name = curItem.Name + " " + variety;
                            }
                        }
                        curItem.ChestId = chestId++;
                        curItem.UV = StringToVector2Short((string)xElement.Attribute("UV"), 0, 0);
                        curItem.TileType = type;
                        ChestProperties.Add(curItem);
                    }
                }
            }

            int signId = 0;
            foreach (var tileElement in xmlSettings.Elements("Tiles").Elements("Tile"))
            {
                string tileName = (string)tileElement.Attribute("Name");
                if (tileName == "Sign" || tileName == "Grave Marker")
                {
                    ushort type = (ushort)((int?)tileElement.Attribute("Id") ?? 55);
                    foreach (var xElement in tileElement.Elements("Frames").Elements("Frame"))
                    {
                        var curItem = new SignProperty();
                        string variety = (string)xElement.Attribute("Variety");
                        if (variety != null)
                        {
                            if (tileName == "Sign")
                            {
                                curItem.Name = "Sign " + variety;
                            }
                            else
                            {
                                curItem.Name = variety;
                            }
                        }
                        curItem.SignId = signId++;
                        curItem.UV = StringToVector2Short((string)xElement.Attribute("UV"), 0, 0);
                        curItem.TileType = type;
                        SignProperties.Add(curItem);
                    }
                }
            }

            foreach (var xElement in xmlSettings.Elements("Npcs").Elements("Npc"))
            {
                int id = (int?)xElement.Attribute("Id") ?? -1;
                string name = (string)xElement.Attribute("Name");
                NpcIds.Add(name, id);
                NpcNames.Add(id, name);
                int frames = (int?)xElement.Attribute("Frames") ?? 16;
                NpcFrames.Add(id, frames);
            }

            foreach (var xElement in xmlSettings.Elements("ItemPrefix").Elements("Prefix"))
            {
                int id = (int?)xElement.Attribute("Id") ?? -1;
                string name = (string)xElement.Attribute("Name");
                ItemPrefix.Add((byte)id, name);
            }

            foreach (var xElement in xmlSettings.Elements("ShortCutKeys").Elements("Shortcut"))
            {
                var key = InLineEnumTryParse<Key>((string)xElement.Attribute("Key"));
                var tool = (string)xElement.Attribute("Tool");
                ShortcutKeys.Add(key, tool);
            }

            XElement appSettings = xmlSettings.Element("App");
            int appWidth = (int?)appSettings.Attribute("Width") ?? 800;
            int appHeight = (int?)appSettings.Attribute("Height") ?? 600;
            int clipboardSize = (int)XNA.MathHelper.Clamp((int?)appSettings.Attribute("ClipboardRenderSize") ?? 512, 64, 4096);

            _appSize = new Vector2(appWidth, appHeight);
            ClipboardBuffer.ClipboardRenderSize = clipboardSize;

            ToolDefaultData.LoadSettings(xmlSettings.Elements("Tools"));

            AltC = (string)xmlSettings.Element("AltC");
            SteamUserId = (int?)xmlSettings.Element("SteamUserId") ?? null;
        }

        public static TileProperty GetBrickFromColor(byte a, byte r, byte g, byte b)
        {
            for (int i = 0; i < TileBricks.Count; i++)
            {
                var curBrick = TileBricks[i];
                var aB = curBrick.Color.A;
                var rB = curBrick.Color.R;
                var gB = curBrick.Color.G;
                var bB = curBrick.Color.B;
                if (r == rB && g == gB && b == bB)
                    return curBrick;
            }

            return null;
        }

        public static WallProperty GetWallFromColor(byte a, byte r, byte g, byte b)
        {
            // if it is a global color, skip
            foreach (var global in GlobalColors)
            {
                var aB = global.Value.A;
                var rB = global.Value.R;
                var gB = global.Value.G;
                var bB = global.Value.B;
                if (r == rB && g == gB && b == bB)
                    return null;
            }
            for (int i = 0; i < WallProperties.Count; i++)
            {
                var curBrick = WallProperties[i];
                var aB = curBrick.Color.A;
                var rB = curBrick.Color.R;
                var gB = curBrick.Color.G;
                var bB = curBrick.Color.B;
                if (r == rB && g == gB && b == bB)
                    return curBrick;
            }
            return null;
        }

        public static Dictionary<string, XNA.Color> GlobalColors
        {
            get { return _globalColors; }
        }

        public static Dictionary<string, int> NpcIds
        {
            get { return _npcIds; }
        }

        private static Dictionary<int, string> _npcNames = new Dictionary<int, string>();
        public static Dictionary<int, string> NpcNames
        {
            get { return _npcNames; }
        }

        public static Dictionary<int, int> NpcFrames
        {
            get { return _npcFrames; }
        }
        
        public static Dictionary<byte, string> ItemPrefix
        {
            get { return _prefix; }
        }

        public static Dictionary<Key, string> ShortcutKeys
        {
            get { return _shortcuts; }
        }

        public static ObservableCollection<TileProperty> TileProperties
        {
            get { return _tileProperties; }
        }


        public static ObservableCollection<TileProperty> TileBricks
        {
            get
            {
                return _tileBricks;
            }
        }

        public static ObservableCollection<WallProperty> WallProperties
        {
            get { return _wallProperties; }
        }

        public static ObservableCollection<ItemProperty> ItemProperties
        {
            get { return _itemProperties; }
        }

        public static ObservableCollection<ChestProperty> ChestProperties
        {
            get { return _chestProperties; }
        }

        public static ObservableCollection<SignProperty> SignProperties
        {
            get { return _signProperties; }
        }

        public static Dictionary<int, ItemProperty> ItemLookupTable
        {
            get { return _itemLookup; }
        }

        public static ObservableCollection<Sprite> Sprites
        {
            get { return _sprites; }
        }

        public static Dictionary<int, string> TallyNames
        {
            get { return _tallynames;  }
        }

        internal static Vector2 AppSize
        {
            get { return _appSize; }
        }
    }
}
