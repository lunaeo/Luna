using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LunaServer.EndlessOnline.Data
{
    /// <summary>
    /// Map type
    /// </summary>
    public enum MapType : byte
    {
        Default = 0,
        PK = 3
    }

    /// <summary>
    /// Timed map effect
    /// </summary>
    public enum MapEffect : byte
    {
        None = 0,
        HPDrain = 1,
        TPDrain = 2,
        Quake = 3
    }

    /// <summary>
    /// Endless Map File reader/writer
    /// </summary>
    public class EMF
    {
        /// <summary>
        /// Special property of a tile
        /// </summary>
        public enum TileSpec : byte
        {
            Wall = 0,
            ChairDown = 1,
            ChairLeft = 2,
            ChairRight = 3,
            ChairUp = 4,
            ChairDownRight = 5,
            ChairUpLeft = 6,
            ChairAll = 7,
            JammedDoor = 8,
            Chest = 9,
            BankVault = 16,
            NPCBoundary = 17,
            MapEdge = 18,
            FakeWall = 19,
            Board1 = 20,
            Board2 = 21,
            Board3 = 22,
            Board4 = 23,
            Board5 = 24,
            Board6 = 25,
            Board7 = 26,
            Board8 = 27,
            Jukebox = 28,
            Jump = 29,
            Water = 30,
            Arena = 32,
            AmbientSource = 33,
            Spikes = 34,
            SpikesTrap = 35,
            SpikesTimed = 36,

            None = 255
        }

        /// <summary>
        /// NPC spawn entry
        /// </summary>
        public struct NPC
        {
            /// <summary>
            /// X coordinate NPC spawns near
            /// </summary>
            public byte x;

            /// <summary>
            /// Y coordinate NPC spawns near
            /// </summary>
            public byte y;

            /// <summary>
            /// NPC's ENF ID
            /// </summary>
            public ushort id;

            /// <summary>
            ///
            /// </summary>
            public byte spawnType;

            public ushort spawnTime;
            public byte amount;
        }

        public struct Unknown
        {
            public byte[] data;
        }

        /// <summary>
        /// Chest item spawn entry
        /// </summary>
        public struct Chest
        {
            public byte x;
            public byte y;
            public ushort key;
            public byte slot;
            public ushort item;
            public ushort time;
            public int amount;
        }

        /// <summary>
        /// Tile special property entry
        /// </summary>
        public struct Tile
        {
            public byte x;
            public TileSpec spec;
        }

        /// <summary>
        /// Tile row
        /// </summary>
        public struct TileRow
        {
            public byte y;
            public List<Tile> tiles;
        }

        /// <summary>
        /// Tile warp entry
        /// </summary>
        public struct Warp
        {
            public byte x;
            public ushort warpMap;
            public byte warpX;
            public byte warpY;
            public byte levelRequirement;
            public ushort door;
            public bool doorOpened;

            public static readonly Warp Empty = new Warp();
        }

        /// <summary>
        /// Warp row
        /// </summary>
        public struct WarpRow
        {
            public byte y;
            public List<Warp> tiles;
        }

        /// <summary>
        /// Tile graphic entry
        /// </summary>
        public class GFX
        {
            public byte x;
            public ushort tile;
        }

        /// <summary>
        /// Graphic row
        /// </summary>
        public struct GFXRow
        {
            public byte y;
            public List<GFX> tiles;
        }

        /// <summary>
        /// Sign entry
        /// </summary>
        public struct Sign
        {
            public byte x;
            public byte y;
            public string title;
            public string message;
        }

        /// <summary>
        /// Special key value meaning the warp has no door
        /// </summary>
        public const ushort NoDoor = 0;

        /// <summary>
        /// Special key value meaning there is an unlocked door
        /// </summary>
        public const ushort Door = 1;

        /// <summary>
        /// Number of graphic layers a map has
        /// </summary>
        public const int GFXLayers = 9;

        /// <summary>
        /// Size of file upon opening it
        /// </summary>
        public long FileSize { get; private set; }

        /// <summary>
        /// A unique number for the version of the file.
        /// </summary>
        /// <remarks>
        /// EOHax defacto standard is to set this to the CRC32 of the entire file output with this field to 0x00000000
        /// 0x00 bytes are represented in 0x01 to avoid problems with file transfer
        /// </remarks>
        public byte[] RevisionId { get; private set; }

        /// <summary>
        /// Name of the map
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Map type
        /// </summary>
        public MapType Type { get; set; }

        /// <summary>
        /// Timed map effect
        /// </summary>
        public MapEffect Effect { get; set; }

        /// <summary>
        /// MFX ID to play
        /// </summary>
        public byte Music { get; set; }

        /// <summary>
        /// Background music playing instructions
        /// </summary>
        public byte MusicExtra { get; set; }

        /// <summary>
        /// SFX ID that ambient noise tiles play
        /// </summary>
        public ushort AmbientNoise { get; set; }

        /// <summary>
        /// Width of the map in tiles
        /// </summary>
        public byte Width { get; set; }

        /// <summary>
        /// Height of the map in tiles
        /// </summary>
        public byte Height { get; set; }

        /// <summary>
        /// Tile graphic ID that fills tiles with no graphic set
        /// </summary>
        public ushort FillTile { get; set; }

        /// <summary>
        /// Whether a player can view the mini-map
        /// </summary>
        public byte MapAvailable { get; set; }

        /// <summary>
        /// Whether a player can teleport out of the map
        /// </summary>
        public byte CanScroll { get; set; }

        /// <summary>
        /// X coordinate a player is moved to on logging in to the map.
        /// If relogX and relogY are both 0 the player is not moved.
        /// </summary>
        public byte RelogX { get; set; }

        /// <summary>
        /// Y coordinate a player is move to on logging in to the map.
        /// If relogX and relogY are both 0 the player is not moved.
        /// </summary>
        public byte RelogY { get; set; }

        public byte Unknown2 { get; set; }

        /// <summary>
        /// NPC spawn entry list
        /// </summary>
        public List<NPC> Npcs { get; set; }

        public List<Unknown> Unknowns { get; set; }

        /// <summary>
        /// Chest item spawn entry list
        /// </summary>
        public List<Chest> Chests { get; set; }

        /// <summary>
        /// List of tile property rows
        /// </summary>
        public List<TileRow> TileRows { get; set; }

        /// <summary>
        /// List of tile warp entry rows
        /// </summary>
        public List<WarpRow> WarpRows { get; set; }

        /// <summary>
        /// Tile graphic information map
        /// </summary>
        public List<GFXRow>[] GfxRows { get; private set; }

        /// <summary>
        /// Sign entry list
        /// </summary>
        public List<Sign> Signs { get; private set; }

        /// <summary>
        /// Encodes a string to EMF format
        /// </summary>
        /// <param name="s">String to be encoded</param>
        public byte[] EncodeString(string s)
        {
            return Encoding.ASCII.GetBytes(this.DecodeString(Encoding.ASCII.GetBytes(s)));
        }

        /// <summary>
        /// Decodes an EMF format string
        /// </summary>
        /// <param name="chars">EMF encoded string</param>
        public string DecodeString(byte[] chars)
        {
            Array.Reverse(chars);

            var flippy = (chars.Length % 2) == 1;

            for (var i = 0; i < chars.Length; ++i)
            {
                var c = chars[i];

                if (flippy)
                {
                    if (c >= 0x22 && c <= 0x4F)
                        c = (byte)(0x71 - c);
                    else if (c >= 0x50 && c <= 0x7E)
                        c = (byte)(0xCD - c);
                }
                else
                {
                    if (c >= 0x22 && c <= 0x7E)
                        c = (byte)(0x9F - c);
                }

                chars[i] = c;
                flippy = !flippy;
            }

            return ASCIIEncoding.ASCII.GetString(chars);
        }

        /// <summary>
        /// Create an empty EMF file
        /// </summary>
        public EMF()
        {
            this.RevisionId = new byte[4];
            this.Name = string.Empty;
            this.Npcs = new List<NPC>();
            this.Unknowns = new List<Unknown>();
            this.Chests = new List<Chest>();
            this.TileRows = new List<TileRow>();
            this.WarpRows = new List<WarpRow>();
            this.GfxRows = new List<GFXRow>[GFXLayers];
            this.Signs = new List<Sign>();

            for (var layer = 0; layer < GFXLayers; ++layer)
            {
                this.GfxRows[layer] = new List<GFXRow>();
            }
        }

        /// <summary>
        /// Load from an EMF file
        /// </summary>
        /// <param name="fileName">File to read the EMF data from</param>
        public EMF(string fileName)
        {
            this.RevisionId = new byte[4];
            this.Name = string.Empty;
            this.Npcs = new List<NPC>();
            this.Unknowns = new List<Unknown>();
            this.Chests = new List<Chest>();
            this.TileRows = new List<TileRow>();
            this.WarpRows = new List<WarpRow>();
            this.GfxRows = new List<GFXRow>[GFXLayers];
            this.Signs = new List<Sign>();

            this.Read(fileName);
        }

        /// <summary>
        /// Read an EMF file
        /// </summary>
        /// <param name="fileName">File to read the EMF data from</param>
        public void Read(string fileName)
        {
            int outersize;
            int innersize;

            using (var file = new EOFile(File.Open(fileName, FileMode.Open, FileAccess.Read)))
            {
                if (file.GetFixedString(3) != "EMF")
                    throw new Exception("Corrupt or not an EMF file");

                this.FileSize = file.FileSize;

                this.RevisionId = file.GetBytes(4);
                var rawname = file.GetBytes(24);

                for (var i = 0; i < 24; ++i)
                {
                    if (rawname[i] == 0xFF)
                    {
                        Array.Resize(ref rawname, i);
                        break;
                    }
                }

                this.Name = this.DecodeString(rawname);

                this.Type = (MapType)file.GetChar();
                this.Effect = (MapEffect)file.GetChar();
                this.Music = file.GetChar();
                this.MusicExtra = file.GetChar();
                this.AmbientNoise = file.GetShort();
                this.Width = file.GetChar();
                this.Height = file.GetChar();
                this.FillTile = file.GetShort();
                this.MapAvailable = file.GetChar();
                this.CanScroll = file.GetChar();
                this.RelogX = file.GetChar();
                this.RelogY = file.GetChar();
                this.Unknown2 = file.GetChar();

                outersize = file.GetChar();

                for (var i = 0; i < outersize; ++i)
                {
                    this.Npcs.Add(new NPC()
                    {
                        x = file.GetChar(),
                        y = file.GetChar(),
                        id = file.GetShort(),
                        spawnType = file.GetChar(),
                        spawnTime = file.GetShort(),
                        amount = file.GetChar()
                    });
                }

                outersize = file.GetChar();

                for (var i = 0; i < outersize; ++i)
                {
                    this.Unknowns.Add(new Unknown()
                    {
                        data = file.GetBytes(5)
                    });
                }

                outersize = file.GetChar();

                for (var i = 0; i < outersize; ++i)
                {
                    this.Chests.Add(new Chest()
                    {
                        x = file.GetChar(),
                        y = file.GetChar(),
                        key = file.GetShort(),
                        slot = file.GetChar(),
                        item = file.GetShort(),
                        time = file.GetShort(),
                        amount = file.GetThree()
                    });
                }

                outersize = file.GetChar();

                for (var i = 0; i < outersize; ++i)
                {
                    var y = file.GetChar();
                    innersize = file.GetChar();

                    var row = new TileRow()
                    {
                        y = y,
                        tiles = new List<Tile>(innersize)
                    };

                    for (var ii = 0; ii < innersize; ++ii)
                    {
                        row.tiles.Add(new Tile()
                        {
                            x = file.GetChar(),
                            spec = (TileSpec)file.GetChar()
                        });
                    }

                    this.TileRows.Add(row);
                }

                outersize = file.GetChar();

                for (var i = 0; i < outersize; ++i)
                {
                    var y = file.GetChar();
                    innersize = file.GetChar();

                    var row = new WarpRow()
                    {
                        y = y,
                        tiles = new List<Warp>(innersize)
                    };

                    for (var ii = 0; ii < innersize; ++ii)
                    {
                        row.tiles.Add(new Warp()
                        {
                            x = file.GetChar(),
                            warpMap = file.GetShort(),
                            warpX = file.GetChar(),
                            warpY = file.GetChar(),
                            levelRequirement = file.GetChar(),
                            door = file.GetShort()
                        });
                    }

                    this.WarpRows.Add(row);
                }

                for (var layer = 0; layer < GFXLayers; ++layer)
                {
                    outersize = file.GetChar();
                    this.GfxRows[layer] = new List<GFXRow>(outersize);

                    for (var i = 0; i < outersize; ++i)
                    {
                        var y = file.GetChar();
                        innersize = file.GetChar();

                        var row = new GFXRow()
                        {
                            y = y,
                            tiles = new List<GFX>(innersize)
                        };

                        row.tiles = new List<GFX>(innersize);

                        for (var ii = 0; ii < innersize; ++ii)
                        {
                            row.tiles.Add(new GFX()
                            {
                                x = file.GetChar(),
                                tile = (ushort)file.GetShort()
                            });
                        }

                        this.GfxRows[layer].Add(row);
                    }
                }

                outersize = file.GetChar();

                for (var i = 0; i < outersize; ++i)
                {
                    var sign = new Sign
                    {
                        x = file.GetChar(),
                        y = file.GetChar()
                    };
                    var msgLength = file.GetShort() - 1;
                    var data = this.DecodeString(file.GetBytes(msgLength));
                    int titleLength = file.GetChar();
                    sign.title = data.Substring(0, titleLength);
                    sign.message = data.Substring(titleLength);

                    this.Signs.Add(sign);
                }
            }
        }

        public void Write(string fileName)
        {
            using (var file = new EOFile(File.Open(fileName, FileMode.Create, FileAccess.Write)))
            {
                file.AddString("EMF");
                file.AddInt(0);

                var padName = new byte[24];

                for (var i = 0; i < 24; ++i)
                    padName[i] = 0xFF;

                var encName = this.EncodeString(this.Name);

                Array.Resize(ref encName, Math.Min(24, encName.Length));
                Array.Copy(encName, 0, padName, 24 - encName.Length, encName.Length);

                file.AddBytes(padName);
                file.AddChar((byte)this.Type);
                file.AddChar((byte)this.Effect);
                file.AddChar(this.Music);
                file.AddChar(this.MusicExtra);
                file.AddShort(this.AmbientNoise);
                file.AddChar(this.Width);
                file.AddChar(this.Height);
                file.AddShort(this.FillTile);
                file.AddChar(this.MapAvailable);
                file.AddChar(this.CanScroll);
                file.AddChar(this.RelogX);
                file.AddChar(this.RelogY);
                file.AddChar(this.Unknown2);

                file.AddChar((byte)this.Npcs.Count);

                foreach (var npc in this.Npcs)
                {
                    file.AddChar(npc.x);
                    file.AddChar(npc.y);
                    file.AddShort(npc.id);
                    file.AddChar(npc.spawnType);
                    file.AddShort(npc.spawnTime);
                    file.AddChar(npc.amount);
                }

                file.AddChar((byte)this.Unknowns.Count);

                foreach (var unknown_ in this.Unknowns)
                {
                    file.AddBytes(unknown_.data);
                }

                file.AddChar((byte)this.Chests.Count);

                foreach (var chest in this.Chests)
                {
                    file.AddChar(chest.x);
                    file.AddChar(chest.y);
                    file.AddShort(chest.key);
                    file.AddChar(chest.slot);
                    file.AddShort(chest.item);
                    file.AddShort(chest.time);
                    file.AddThree(chest.amount);
                }

                file.AddChar((byte)this.TileRows.Count);

                foreach (var row in this.TileRows)
                {
                    file.AddChar(row.y);
                    file.AddChar((byte)row.tiles.Count);

                    foreach (var tile in row.tiles)
                    {
                        file.AddChar(tile.x);
                        file.AddChar((byte)tile.spec);
                    }
                }

                file.AddChar((byte)this.WarpRows.Count);

                foreach (var row in this.WarpRows)
                {
                    file.AddChar(row.y);
                    file.AddChar((byte)row.tiles.Count);

                    foreach (var warp in row.tiles)
                    {
                        file.AddChar(warp.x);
                        file.AddShort(warp.warpMap);
                        file.AddChar(warp.warpX);
                        file.AddChar(warp.warpY);
                        file.AddChar(warp.levelRequirement);
                        file.AddShort(warp.door);
                    }
                }

                for (var layer = 0; layer < GFXLayers; ++layer)
                {
                    file.AddChar((byte)this.GfxRows[layer].Count);

                    foreach (var row in this.GfxRows[layer])
                    {
                        file.AddChar(row.y);
                        file.AddChar((byte)row.tiles.Count);

                        foreach (var gfx in row.tiles)
                        {
                            file.AddChar(gfx.x);
                            file.AddShort(gfx.tile);
                        }
                    }
                }

                file.AddChar((byte)this.Signs.Count);

                foreach (var sign in this.Signs)
                {
                    file.AddChar(sign.x);
                    file.AddChar(sign.y);
                    file.AddShort((ushort)(sign.title.Length + sign.message.Length + 1));
                    file.AddBytes(this.EncodeString(sign.title + sign.message));
                    file.AddChar((byte)sign.title.Length);
                }

                this.RevisionId = file.WriteHash(3, SeekOrigin.Begin);
            }
        }
    }
}