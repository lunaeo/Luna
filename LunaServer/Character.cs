using System;
using System.Collections.Generic;
using System.Linq;

namespace LunaServer
{
    using EndlessOnline.Communication;
    using EndlessOnline.Data;
    using EndlessOnline.Domain.Character;
    using EndlessOnline.Replies;

    public class Character
    {
        public GameSession Session { get; }
        public List<InventoryItem> Inventory { get; private set; }
        public ushort[] Paperdoll { get; private set; }
        public string Name { get; private set; } = "";
        public string Title { get; private set; } = "";
        public string Home { get; private set; } = "";
        public string Fiance { get; private set; } = "";
        public string Partner { get; private set; } = "";
        public byte Class { get; private set; }
        public Gender Gender { get; private set; }
        public Skin Skin { get; private set; }
        public byte HairStyle { get; set; } = 1;
        public byte HairColor { get; set; } = 0;
        public ushort MapId { get; private set; }
        public int PX { get; set; } = -1;
        public int PY { get; set; } = -1;
        public byte X { get; set; } = 0;
        public byte Y { get; set; } = 0;
        public Direction Direction { get; set; }
        public byte Level { get; private set; }
        public int Exp { get; private set; }
        public ushort Health { get; set; } = 1;
        public ushort Mana { get; set; } = 1;
        public ushort MaxHealth { get; private set; } = 1;
        public ushort MaxMana { get; private set; } = 1;
        public ushort MaxStamina { get; private set; } = 1;
        public byte SitState { get; internal set; }
        public WarpAnimation WarpAnimation { get; private set; }
        public ushort StatPoints { get; internal set; }
        public ushort SkillPoints { get; internal set; }
        public ushort Karma { get; internal set; }
        public ushort MinDamage { get; internal set; }
        public ushort MaxDamage { get; internal set; }
        public ushort Accuracy { get; internal set; }
        public ushort Evade { get; internal set; }
        public ushort Armor { get; internal set; }
        public ushort Strength { get; internal set; }
        public ushort Intellect { get; internal set; }
        public ushort Wisdom { get; internal set; }
        public ushort Agility { get; internal set; }
        public ushort Constitution { get; internal set; }
        public ushort Charisma { get; internal set; }
        public byte Weight { get; internal set; }
        public byte MaxWeight { get; internal set; }
        public bool Hidden { get; internal set; }

        public Map Map
        {
            get
            {
                if (this.Session.GameServer.Maps.ContainsKey(this.MapId))
                    return this.Session.GameServer.Maps[this.MapId];
                else throw new Exception("Map Not Defined.");
            }
        }

        internal (int id, DateTime time) LastEmoteUsed { get; set; }
        internal (int x, int y, MapItem item) LastItemDropped { get; set; }
        internal (int x, int y, MapItem item) LastItemPickedUp { get; set; }
        internal MapNPC LastNPCAttacked { get; set; }
        internal string LastMessageSpoken { get; set; }

        public Character(GameSession session)
        {
            this.Session = session;
            this.Inventory = new List<InventoryItem>();
            this.Paperdoll = new ushort[15] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            this.HairStyle = 1;
            this.HairColor = 1;
            this.MaxWeight = 20;
            this.MapId = 1;
            this.X = 0;
            this.Y = 0;

            this.LastNPCAttacked = new MapNPC(this.Map, 0, 0, 0, 0, 0, 0);
            this.LastEmoteUsed = (-1, DateTime.MinValue);
            this.LastItemDropped = (-1, -1, new MapItem(0, 0, 0, 0));
            this.LastItemPickedUp = (-1, -1, new MapItem(0, 0, 0, 0));
        }

        public void Logout()
        {
            if (this.Map != null)
                this.Map.Leave(this, WarpAnimation.None, false);
        }

        public void Attack(Direction direction)
            => this.Map.Attack(this, direction);

        public void Emote(Emote emote, bool echo)
            => this.Map.Emote(this, emote, echo);

        public bool Walk(Direction direction, bool admin)
            => this.Map.Walk(this, direction, admin);

        public void Face(Direction direction)
            => this.Map.Face(this, direction);

        public void SetName(string name)
            => this.Name = name;

        public void SetSkin(Skin skin)
            => this.Skin = skin;

        public bool InRange(Character character)
            => this.InRange(character.X, character.Y);

        public bool InRange(MapItem item)
            => this.InRange(item.X, item.Y);

        public bool InRange(MapNPC npc) =>
            this.InRange((byte)npc.X, (byte)npc.Y);

        public void Sit(SitState type)
            => this.Map.Sit(this, type);

        public void Stand()
            => this.Map.Stand(this);

        public bool InRange(byte x, byte y)
        {
            var xDistance = (byte)Math.Abs((sbyte)(this.X - x));
            var yDistance = (byte)Math.Abs((sbyte)(this.Y - y));

            return (xDistance + yDistance) < 11;
        }

        public void SetMap(ushort mapId, byte x, byte y)
        {
            this.MapId = mapId;
            this.X = x;
            this.Y = y;
        }

        public void SetHP(ushort health, ushort max_health)
        {
            this.Health = health;
            this.MaxHealth = max_health;
        }

        public void SetMP(ushort mana, ushort max_mana)
        {
            this.Health = mana;
            this.MaxHealth = max_mana;
        }

        public int HasItem(ushort id)
        {
            var item = this.Inventory.FirstOrDefault(t => t.ItemID == id);
            return item != null ? item.Amount : 0;
        }

        public void GiveItem(InventoryItem item) => this.GiveItem((MapItem)new MapItem(0, item.ItemID, 0, 0).WithAmount(item.Amount));

        public void GiveItem(MapItem item)
        {
            this.AddItem((ushort)item.ItemID, item.Amount);

            var reply = new Packet(PacketFamily.Item, PacketAction.Get);
            reply.AddShort(0); // item uid
            reply.AddShort((ushort)item.ItemID);
            reply.AddThree(item.Amount);
            reply.AddChar(this.Weight);
            reply.AddChar(this.MaxWeight);
            this.Session.Send(reply);
        }

        public void TakeItem(InventoryItem item) => this.TakeItem((MapItem)new MapItem(0, item.ItemID, 0, 0).WithAmount(item.Amount));

        public void TakeItem(MapItem item)
        {
            this.DeleteItem((ushort)item.ItemID, item.Amount);

            var reply = new Packet(PacketFamily.Item, PacketAction.Junk);
            reply.AddShort((ushort)item.ItemID);
            reply.AddThree(item.Amount);
            reply.AddInt(this.HasItem((ushort)item.ItemID));
            reply.AddChar(this.Weight);
            reply.AddChar(this.MaxWeight);
            this.Session.Send(reply);
        }

        public void Refresh()
        {
            var updateCharacters = new List<Character>();
            var updateNPCs = new List<MapNPC>();
            var updateItems = new List<MapItem>();

            foreach (var character in this.Map.Characters)
            {
                if (this.InRange(character))
                {
                    updateCharacters.Add(character);
                }
            }

            foreach (var npc in this.Map.MapNPCs)
            {
                if (this.InRange(npc))
                {
                    updateNPCs.Add(npc);
                }
            }

            foreach (var item in this.Map.MapItems)
            {
                if (this.InRange(item))
                {
                    updateItems.Add(item);
                }
            }

            var builder = new Packet(PacketFamily.Refresh, PacketAction.Reply);
            builder.AddChar((byte)updateCharacters.Count);
            builder.AddBreak();

            foreach (var character in updateCharacters)
            {
                character.Session.BuildCharacterPacket(builder);
                builder.AddBreak();
            }

            foreach (var npc in updateNPCs)
            {
                builder.AddChar((byte)npc.Index);
                builder.AddShort((ushort)npc.Id);
                builder.AddChar((byte)npc.X);
                builder.AddChar((byte)npc.Y);
                builder.AddChar((byte)npc.Direction);
            }

            builder.AddBreak();

            foreach (var item in updateItems)
            {
                builder.AddShort((ushort)item.UniqueID);
                builder.AddShort((ushort)item.ItemID);
                builder.AddChar(item.X);
                builder.AddChar(item.Y);
                builder.AddThree(item.Amount);
            }

            this.Session.Send(builder);
        }

        public void Warp(ushort map, byte x, byte y, WarpAnimation animation)
        {
            if (!this.Session.GameServer.Maps.ContainsKey(map))
                return;

            var builder = new Packet(PacketFamily.Warp, PacketAction.Request);

            if (this.MapId == map)
            {
                builder.AddChar((byte)WarpReply.Local);
                builder.AddShort(map);
                builder.AddChar(x);
                builder.AddChar(y);
            }
            else
            {
                builder.AddChar((byte)WarpReply.Switch);
                builder.AddShort(map);

                if (this.Session.GameServer.Maps[map].Type == MapType.PK)
                {
                    builder.AddByte(0xFF);
                    builder.AddByte(0x01);
                }
                else
                {
                    builder.AddByte(this.Session.GameServer.Maps[map].RevisionId[0]);
                    builder.AddByte(this.Session.GameServer.Maps[map].RevisionId[1]);
                }

                builder.AddByte(this.Session.GameServer.Maps[map].RevisionId[2]);
                builder.AddByte(this.Session.GameServer.Maps[map].RevisionId[3]);
                builder.AddThree((int)this.Session.GameServer.Maps[map].FileSize);
                builder.AddChar(0); // ?
                builder.AddChar(0); // ?
            }

            if (this.Map != null)
                this.Map.Leave(this, animation, false);

            this.MapId = map;
            this.X = x;
            this.Y = y;
            this.SitState = 0;

            // TODO: TRADING

            this.WarpAnimation = animation;
            this.Map.Enter(this, animation);
            this.Session.Send(builder);

            // TODO: ARENA
        }

        private bool DeleteItem(ushort id, int amount)
        {
            if (amount <= 0)
                return false;

            for (var i = 0; i < this.Inventory.Count; ++i)
            {
                if (this.Inventory[i].ItemID == id)
                {
                    var item = this.Inventory[i];
                    item = (InventoryItem)item.WithAmount(item.Amount - amount);
                    this.Inventory[i] = item;

                    if (item.Amount <= 0)
                        this.Inventory.RemoveAt(i);

                    return true;
                }
            }
            return true;
        }

        private bool AddItem(ushort id, int amount)
        {
            if (amount <= 0)
                return false;

            InventoryItem item;
            for (var i = 0; i < this.Inventory.Count; ++i)
            {
                if (this.Inventory[i].ItemID == id)
                {
                    item = this.Inventory[i];
                    item = new InventoryItem(item.ItemID, item.Amount + amount);
                    this.Inventory[i] = item;
                    return true;
                }
            }

            item = new InventoryItem((short)id, amount);

            this.Inventory.Add(item);
            return true;
        }

    }
}