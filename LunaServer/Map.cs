using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace LunaServer
{
    using EndlessOnline.Communication;
    using EndlessOnline.Data;
    using EndlessOnline.Replies;
    using Events;
    using Utilities;

    public class Map : EMF
    {
        public GameServer GameServer { get; }
        public List<Character> Characters { get; }
        public List<MapItem> MapItems { get; internal set; }
        public List<MapNPC> MapNPCs { get; internal set; }
        public List<IDisposable> ScriptTimers { get; }
        public string FileName { get; }
        public ushort Id { get; }

        public Map(GameServer server, ushort mapId, string fileName)
        {
            this.Id = mapId;
            this.GameServer = server;
            this.Characters = new List<Character>();
            this.MapItems = new List<MapItem>();
            this.MapNPCs = new List<MapNPC>();
            this.ScriptTimers = new List<IDisposable>();
            this.FileName = fileName;

            base.Read(this.FileName);
        }

        internal void OpenDoor(byte x, byte y)
        {
            var builder = new Packet(PacketFamily.Door, PacketAction.Open);
            builder.AddChar(x);
            builder.AddChar(y);

            foreach (var mapCharacter in this.Characters)
            {
                if (mapCharacter.InRange(x, y))
                    mapCharacter.Session.Send(builder);
            }
        }

        internal void RemoveNPC(ushort uid)
        {
            var npc = this.MapNPCs.First(t => t.Index == uid);

            foreach (var character in this.Characters)
                npc.RemoveFromView(character);

            this.MapNPCs.RemoveAll(t => t.Index == uid);
        }

        internal MapNPC AddNPC(ushort id, byte x, byte y)
        {
            var npc = new MapNPC(this, id, x, y, this.GenerateNPCUID(), this.GameServer.NPCData[id].hp, this.GameServer.NPCData[id].hp);

            this.MapNPCs.Add(npc);
            npc.Spawn();

            return npc;
        }

        internal MapItem AddItem(ushort id, int amount, byte x, byte y, Character owner)
        {
            var item = new MapItem(this.GenerateItemUID(), (short)id, x, y).WithAmount(amount);

            if (owner != null)
                item = item.WithOwningPlayerID(owner.Session.PlayerId);

            var addItem = new Packet(PacketFamily.Item, PacketAction.Add);
            addItem.AddShort((ushort)item.ItemID);
            addItem.AddShort((ushort)item.UniqueID);
            addItem.AddThree(item.Amount);
            addItem.AddChar(item.X);
            addItem.AddChar(item.Y);

            foreach (var mapCharacter in this.Characters)
            {
                if ((owner != null && mapCharacter == owner) || !mapCharacter.InRange(item.X, item.Y))
                    continue;

                mapCharacter.Session.Send(addItem);
            }

            if (owner != null)
            {
                var dropItem = new Packet(PacketFamily.Item, PacketAction.Drop);
                dropItem.AddShort(id);
                dropItem.AddThree(amount);
                dropItem.AddInt(owner.HasItem(id));
                dropItem.AddShort((ushort)item.UniqueID);
                dropItem.AddChar(x);
                dropItem.AddChar(y);
                dropItem.AddChar(owner.Weight);
                dropItem.AddChar(owner.MaxWeight);
                owner.Session.Send(dropItem);
            }

            this.MapItems.Add((MapItem)item);
            return (MapItem)item;
        }

        internal bool Walk(MapNPC npc, Direction direction)
        {
            var targetX = npc.X;
            var targetY = npc.Y;

            switch (direction)
            {
                case Direction.Down:
                    targetY += 1;
                    break;

                case Direction.Left:
                    targetX -= 1;
                    break;

                case Direction.Up:
                    targetY -= 1;
                    break;

                case Direction.Right:
                    targetX += 1;
                    break;
            }

            if (!this.Walkable((byte)targetX, (byte)targetY, false))
                return false;

            npc.X = targetX;
            npc.Y = targetY;

            var newX = new List<int>();
            var newY = new List<int>();
            var oldX = new List<int>();
            var oldY = new List<int>();

            var newCharacters = new List<Character>();
            var oldCharacters = new List<Character>();
            var seeDistance = 11;

            switch (direction)
            {
                case Direction.Down:
                    for (var i = -seeDistance; i <= seeDistance; ++i)
                    {
                        newX.Add(npc.X + i);
                        newY.Add(npc.Y + seeDistance - Math.Abs(i));
                        oldX.Add(npc.X + i);
                        oldY.Add(npc.Y - seeDistance - 1 + Math.Abs(i));
                    }
                    break;

                case Direction.Left:
                    for (var i = -seeDistance; i <= seeDistance; ++i)
                    {
                        newX.Add(npc.X - seeDistance + Math.Abs(i));
                        newY.Add(npc.Y + i);
                        oldX.Add(npc.X + seeDistance + 1 - Math.Abs(i));
                        oldY.Add(npc.Y + i);
                    }
                    break;

                case Direction.Up:
                    for (var i = -seeDistance; i <= seeDistance; ++i)
                    {
                        newX.Add(npc.X + i);
                        newY.Add(npc.Y - seeDistance + Math.Abs(i));
                        oldX.Add(npc.X + i);
                        oldY.Add(npc.Y + seeDistance + 1 - Math.Abs(i));
                    }
                    break;

                case Direction.Right:
                    for (var i = -seeDistance; i <= seeDistance; ++i)
                    {
                        newX.Add(npc.X + seeDistance - Math.Abs(i));
                        newY.Add(npc.Y + i);
                        oldX.Add(npc.X - seeDistance - 1 + Math.Abs(i));
                        oldY.Add(npc.Y + i);
                    }
                    break;
            }

            npc.Direction = direction;

            if (newX.Count != newY.Count || oldX.Count != oldY.Count || newX.Count != oldX.Count)
                return false;

            foreach (var checkCharacter in this.Characters)
            {
                for (var i = 0; i < oldX.Count; ++i)
                {
                    if (checkCharacter.X == oldX[i] && checkCharacter.Y == oldY[i])
                    {
                        oldCharacters.Add(checkCharacter);
                    }
                    else if (checkCharacter.X == newX[i] && checkCharacter.Y == newY[i])
                    {
                        newCharacters.Add(checkCharacter);
                    }
                }
            }

            var NPCAppear = new Packet(PacketFamily.Appear, PacketAction.Reply);
            NPCAppear.AddChar(0);
            NPCAppear.AddBreak();
            NPCAppear.AddChar((byte)npc.Index);
            NPCAppear.AddShort((ushort)npc.Id);
            NPCAppear.AddChar((byte)npc.X);
            NPCAppear.AddChar((byte)npc.Y);
            NPCAppear.AddChar((byte)npc.Direction);

            foreach (var mapCharacter in newCharacters)
            {
                mapCharacter.Session.Send(NPCAppear);
            }

            var NPCWalk = new Packet(PacketFamily.NPC, PacketAction.Player);
            NPCWalk.AddChar((byte)npc.Index);
            NPCWalk.AddChar((byte)npc.X);
            NPCWalk.AddChar((byte)npc.Y);
            NPCWalk.AddChar((byte)npc.Direction);
            NPCWalk.AddBreak();
            NPCWalk.AddBreak();
            NPCWalk.AddBreak();

            foreach (var mapCharacter in this.Characters)
            {
                if (!mapCharacter.InRange(npc))
                    continue;

                mapCharacter.Session.Send(NPCWalk);
            }

            foreach (var mapCharacter in oldCharacters)
                npc.RemoveFromView(mapCharacter);

            return true;
        }

        internal MapItem GetItem(ushort uid)
        {
            foreach (var item in this.MapItems)
            {
                if (item.UniqueID == uid)
                    return item;
            }

            return new MapItem(0, 0, 0, 0);
        }

        internal void DeleteItem(ushort uid, Character from)
        {
            foreach (var item in this.MapItems)
            {
                if (item.UniqueID == uid)
                {
                    var reply = new Packet(PacketFamily.Item, PacketAction.Remove);
                    reply.AddShort(uid);

                    foreach (var mapCharacter in this.Characters)
                    {
                        if ((from != null && mapCharacter == from) || !mapCharacter.InRange(item))
                            continue;

                        mapCharacter.Session.Send(reply);
                    }
                    this.MapItems.Remove(item);
                    return;
                }
            }
        }

        internal void Attack(Character character, Direction direction)
        {
            var builder = new Packet(PacketFamily.Attack, PacketAction.Player);
            builder.AddShort(character.Session.PlayerId);
            builder.AddChar((byte)direction);

            foreach (var mapCharacter in this.Characters)
            {
                if (character.Session.PlayerId == mapCharacter.Session.PlayerId || !character.InRange(mapCharacter))
                    continue;

                mapCharacter.Session.Send(builder);
            }

            character.Direction = direction;
        }

        internal void Enter(Character character, WarpAnimation animation)
        {
            if (!this.GameServer.World.MapScripts.Any(t => t.Name == this.Id.ToString()))
            {
                var script_filename = Path.Combine("config", "scripts", "maps", character.MapId + ".ms");

                if (File.Exists(script_filename))
                {
                    var page = this.GameServer.World.LoadScript(script_filename, (s, paragraph, trigger) =>
                    {
                        // When # seconds have passed, offset by #,
                        if (trigger.Id == 250)
                        {
                            var seconds = trigger.Get<int>(0);
                            var offset = trigger.Get<int>(1);

                            this.ScriptTimers.Add(SimpleTimer.SetInterval(() =>
                            {
                                Thread.Sleep(offset * 1000);
                                s.ExecuteParagraph(paragraph, new EndlessContext(character.Session), null);
                            }, 1000 * seconds));
                        }
                    });

                    this.GameServer.World.MapScripts.Add(page);
                    this.GameServer.Console.Debug("MoonScript map script loaded: {name}", script_filename);

                    // When everything is starting up,
                    page.Execute(new EndlessContext(character.Session), null, 0);
                }
                else
                {
                    this.GameServer.Console.Warning("The map {id} does not have a default map script associated with it.", this.Id);
                }
            }

            this.Characters.Add(character);
            character.SetMap(this.Id, character.X, character.Y);

            var builder = new Packet(PacketFamily.Players, PacketAction.Agree);
            builder.AddBreak();
            character.Session.BuildCharacterPacket(builder);
            builder.AddChar((byte)animation);
            builder.AddBreak();
            builder.AddChar(1); // 0 = NPC, 1 = player

            // check if character is in range
            foreach (var checkCharacter in this.Characters)
            {
                if (checkCharacter == character || !character.InRange(checkCharacter))
                    continue;

                checkCharacter.Session.Send(builder);
            }

            character.Session.GameServer.World.ExecuteTrigger(new EndlessContext(character.Session), new EnterMapEvent(character.Session), 13);
        }

        internal void Leave(Character character, WarpAnimation animation, bool silent)
        {
            if (!silent)
            {
                var builder = new Packet(PacketFamily.Avatar, PacketAction.Remove);
                builder.AddShort(character.Session.PlayerId);

                if (animation != WarpAnimation.None)
                    builder.AddChar((byte)animation);

                foreach (var mapCharacter in this.Characters)
                {
                    if (mapCharacter == character || !character.InRange(mapCharacter))
                        continue;

                    mapCharacter.Session.Send(builder);
                }
            }

            this.Characters.Remove(character);

            character.Session.GameServer.World.ExecuteTrigger(new EndlessContext(character.Session), new EnterMapEvent(character.Session), 14);
        }

        internal void Sit(Character character, SitState type)
        {
            if (type == SitState.Stand)
                return;

            character.SitState = (byte)type;

            var builder = new Packet((type == SitState.Chair) ? PacketFamily.Chair : PacketFamily.Sit, PacketAction.Player);
            builder.AddShort(character.Session.PlayerId);
            builder.AddChar(character.X);
            builder.AddChar(character.Y);
            builder.AddChar((byte)character.Direction);
            builder.AddChar(0); // ?

            foreach (var mapCharacters in this.Characters)
            {
                if (character.InRange(mapCharacters))
                {
                    mapCharacters.Session.Send(builder);
                }
            }

            character.Session.GameServer.World.ExecuteTrigger(new EndlessContext(character.Session), new SitEvent(character.Session), 18);
        }

        internal void Stand(Character character)
        {
            if (character.SitState == (byte)SitState.Stand)
                return;

            character.SitState = (byte)SitState.Stand;

            var builder = new Packet(PacketFamily.Sit, PacketAction.Remove);
            builder.AddShort(character.Session.PlayerId);
            builder.AddChar(character.X);
            builder.AddChar(character.Y);

            foreach (var mapCharacters in this.Characters)
            {
                if (character.InRange(mapCharacters))
                {
                    mapCharacters.Session.Send(builder);
                }
            }

            character.Session.GameServer.World.ExecuteTrigger(new EndlessContext(character.Session), new StandEvent(character.Session), 17);
        }

        internal void Emote(Character character, Emote emote, bool echo)
        {
            var builder = new Packet(PacketFamily.Emote, PacketAction.Player);
            builder.AddShort(character.Session.PlayerId);
            builder.AddChar((byte)emote);

            foreach (var mapCharacter in this.Characters)
            {
                if (!echo && (mapCharacter == character || !character.InRange(mapCharacter)))
                    continue;

                mapCharacter.Session.Send(builder);
            }
        }

        internal TileSpec GetSpec(byte x, byte y)
        {
            if (!((x >= 0 && x <= this.Width) && (y >= 0 && y <= this.Height)))
                return TileSpec.None;

            foreach (var row in this.TileRows)
            {
                if (row.y != y)
                    continue;

                foreach (var tile in row.tiles)
                {
                    if (tile.x != x)
                        continue;

                    return tile.spec;
                }
                break;
            }
            return TileSpec.None;
        }

        internal bool Walkable(byte x, byte y, bool npc)
        {
            if (npc)
                return false;

            switch (this.GetSpec(x, y))
            {
                case TileSpec.Wall:
                case TileSpec.ChairDown:
                case TileSpec.ChairLeft:
                case TileSpec.ChairRight:
                case TileSpec.ChairUp:
                case TileSpec.ChairDownRight:
                case TileSpec.ChairUpLeft:
                case TileSpec.ChairAll:
                case TileSpec.Chest:
                case TileSpec.BankVault:
                case TileSpec.MapEdge:
                case TileSpec.Board1:
                case TileSpec.Board2:
                case TileSpec.Board3:
                case TileSpec.Board4:
                case TileSpec.Board5:
                case TileSpec.Board6:
                case TileSpec.Board7:
                case TileSpec.Board8:
                case TileSpec.Jukebox:
                    return false;

                case TileSpec.NPCBoundary:
                    return !npc;

                default:
                    return true;
            }
        }

        internal bool Walk(Character character, Direction direction, bool admin)
        {
            var targetX = character.X;
            var targetY = character.Y;

            switch (direction)
            {
                case Direction.Down:
                    targetY += 1;
                    break;

                case Direction.Left:
                    targetX -= 1;
                    break;

                case Direction.Up:
                    targetY -= 1;
                    break;

                case Direction.Right:
                    targetX += 1;
                    break;
            }

            if (!admin && !this.Walkable(targetX, targetY, false))
                return false;

            character.X = targetX;
            character.Y = targetY;
            character.Direction = direction;

            var newX = new List<int>();
            var newY = new List<int>();
            var oldX = new List<int>();
            var oldY = new List<int>();

            var newCharacters = new List<Character>();
            var oldCharacters = new List<Character>();
            var newNPCs = new List<MapNPC>();
            var oldNPCs = new List<MapNPC>();
            var newItems = new List<MapItem>();
            var seeDistance = 11;

            switch (direction)
            {
                case Direction.Down:
                    for (var i = -seeDistance; i <= seeDistance; ++i)
                    {
                        newX.Add(character.X + i);
                        newY.Add(character.Y + seeDistance - Math.Abs(i));
                        oldX.Add(character.X + i);
                        oldY.Add(character.Y - seeDistance - 1 + Math.Abs(i));
                    }
                    break;

                case Direction.Left:
                    for (var i = -seeDistance; i <= seeDistance; ++i)
                    {
                        newX.Add(character.X - seeDistance + Math.Abs(i));
                        newY.Add(character.Y + i);
                        oldX.Add(character.X + seeDistance + 1 - Math.Abs(i));
                        oldY.Add(character.Y + i);
                    }
                    break;

                case Direction.Up:
                    for (var i = -seeDistance; i <= seeDistance; ++i)
                    {
                        newX.Add(character.X + i);
                        newY.Add(character.Y - seeDistance + Math.Abs(i));
                        oldX.Add(character.X + i);
                        oldY.Add(character.Y + seeDistance + 1 - Math.Abs(i));
                    }
                    break;

                case Direction.Right:
                    for (var i = -seeDistance; i <= seeDistance; ++i)
                    {
                        newX.Add(character.X + seeDistance - Math.Abs(i));
                        newY.Add(character.Y + i);
                        oldX.Add(character.X - seeDistance - 1 + Math.Abs(i));
                        oldY.Add(character.Y + i);
                    }
                    break;
            }

            if (newX.Count != newY.Count || oldX.Count != oldY.Count || newX.Count != oldX.Count)
                return false;

            foreach (var checkCharacter in this.Characters)
            {
                if (checkCharacter == character)
                    continue;

                for (var i = 0; i < oldX.Count; ++i)
                {
                    if (checkCharacter.X == oldX[i] && checkCharacter.Y == oldY[i])
                    {
                        oldCharacters.Add(checkCharacter);
                    }
                    else if (checkCharacter.X == newX[i] && checkCharacter.Y == newY[i])
                    {
                        newCharacters.Add(checkCharacter);
                    }
                }
            }

            foreach (var checkNPC in this.MapNPCs)
            {
                if (!checkNPC.Alive)
                    continue;

                for (var i = 0; i < oldX.Count; ++i)
                {
                    if (checkNPC.X == oldX[i] && checkNPC.Y == oldY[i])
                    {
                        oldNPCs.Add(checkNPC);
                    }
                    else if (checkNPC.X == newX[i] && checkNPC.Y == newY[i])
                    {
                        newNPCs.Add(checkNPC);
                    }
                }
            }

            foreach (var checkItem in this.MapItems)
            {
                for (var i = 0; i < newX.Count; ++i)
                {
                    if (checkItem.X == newX[i] && checkItem.Y == newY[i])
                    {
                        newItems.Add(checkItem);
                    }
                }
            }

            var builder = new Packet(PacketFamily.Avatar, PacketAction.Remove);
            builder.AddShort(character.Session.PlayerId);

            foreach (var oldCharacter in oldCharacters)
            {
                var reply = new Packet(PacketFamily.Avatar, PacketAction.Remove);
                reply.AddShort(oldCharacter.Session.PlayerId);

                oldCharacter.Session.Send(builder);
                character.Session.Send(reply);
            }

            builder.Clear();
            builder.SetID(PacketFamily.Players, PacketAction.Agree);
            builder.AddBreak();
            character.Session.BuildCharacterPacket(builder);
            builder.AddBreak();
            builder.AddChar(1); // 0 = NPC, 1 = player

            foreach (var newCharacter in newCharacters)
            {
                var rbuilder = new Packet(PacketFamily.Players, PacketAction.Agree);
                rbuilder.AddBreak();
                newCharacter.Session.BuildCharacterPacket(rbuilder);
                rbuilder.AddBreak();
                rbuilder.AddChar(1); // 0 = NPC, 1 = player

                newCharacter.Session.Send(builder);
                character.Session.Send(rbuilder);
            }

            builder.Clear();
            builder.SetID(PacketFamily.Walk, PacketAction.Player);
            builder.AddShort(character.Session.PlayerId);
            builder.AddChar((byte)direction);
            builder.AddChar(character.X);
            builder.AddChar(character.Y);

            foreach (var checkCharacter in this.Characters)
            {
                if (checkCharacter == character || !character.InRange(checkCharacter))
                    continue;

                checkCharacter.Session.Send(builder);
            }

            builder.Clear();
            builder.SetID(PacketFamily.Walk, PacketAction.Reply);
            builder.AddBreak();
            builder.AddBreak();
            foreach (var item in newItems)
            {
                builder.AddShort((ushort)item.UniqueID);
                builder.AddShort((ushort)item.ItemID);
                builder.AddChar(item.X);
                builder.AddChar(item.Y);
                builder.AddThree(item.Amount);
            }
            character.Session.Send(builder);

            foreach (var npc in newNPCs)
            {
                builder.Clear();
                builder.SetID(PacketFamily.Appear, PacketAction.Reply);

                builder.AddChar(0);
                builder.AddBreak();
                builder.AddChar((byte)npc.Index);
                builder.AddShort((ushort)npc.Id);
                builder.AddChar((byte)npc.X);
                builder.AddChar((byte)npc.Y);
                builder.AddChar((byte)npc.Direction);

                character.Session.Send(builder);
            }

            foreach (var npc in oldNPCs)
                npc.RemoveFromView(character);

            return true;
        }

        internal void Reload()
        {
            var temp = this.Characters.ToList();

            this.MapNPCs = new List<MapNPC>();
            this.MapItems = new List<MapItem>();

            base.Read(this.FileName);

            foreach (var character in temp)
            {
                this.GameServer.UploadFile(character.Session, FileType.Map, InitReply.MapMutation);
                character.Refresh();
            }
        }

        internal void Face(Character character, Direction direction)
        {
            character.Direction = direction;

            var packet = new Packet(PacketFamily.Face, PacketAction.Player);
            packet.AddShort(character.Session.PlayerId);
            packet.AddChar((byte)direction);

            foreach (var mapCharacter in this.Characters)
            {
                if (mapCharacter == character || !character.InRange(mapCharacter))
                    continue;

                mapCharacter.Session.Send(packet);
            }
        }

        private ushort GenerateNPCUID()
        {
            ushort lowest_free_id = 1;

            restart:
            foreach (var npc in this.MapNPCs)
            {
                if (npc.Index == lowest_free_id)
                {
                    ++lowest_free_id;

                    if (lowest_free_id > 64000)
                        lowest_free_id = 1;

                    goto restart;
                }
            }

            return lowest_free_id;
        }

        private ushort GenerateItemUID()
        {
            ushort lowest_free_id = 1;

            restart:
            foreach (var item in this.MapItems)
            {
                if (item.UniqueID == lowest_free_id)
                {
                    ++lowest_free_id;

                    if (lowest_free_id > 64000)
                        lowest_free_id = 1;

                    goto restart;
                }
            }
            return lowest_free_id;
        }
    }
}