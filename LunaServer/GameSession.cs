using System;
using System.Linq;
using System.Diagnostics;
using NetCoreServer;

namespace LunaServer
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Data;
    using EndlessOnline.Replies;
    using Addons;
    using Utilities;

    public class GameSession : TcpSession
    {
        public GameServer GameServer { get; }
        public PacketAction PacketActions { get; }
        public ClientState State { get; private set; }
        public PacketProcessor Processor { get; private set; }
        public AddonSession AddonConnection { get; internal set; }
        public Character Character { get; private set; }
        public ushort PlayerId { get; internal set; }
        public uint HardDriveId { get; internal set; }
        public bool NeedsPong { get; internal set; }

        public GameSession(TcpServer server) : base(server)
        {
            this.GameServer = (GameServer)server;
            this.Processor = new ServerPacketProcessor();
            this.SetState(ClientState.Uninitialized);
        }

        internal void CreateCharacter()
        {
            this.Character = new Character(this);

            retry_name_generation:
            var temporary_name = UniqueNameGenerator.Generate();

            if (this.GameServer.PlayingSessions.Any(c => c.Character.Name == temporary_name))
                goto retry_name_generation;

            this.Character.SetName(temporary_name);
        }

        internal void BuildCharacterPacket(Packet packet)
        {
            packet.AddBreakString(this.Character.Name);
            packet.AddShort(this.PlayerId);
            packet.AddShort(this.Character.MapId);
            packet.AddShort(this.Character.X);
            packet.AddShort(this.Character.Y);
            packet.AddChar((byte)this.Character.Direction);
            packet.AddChar((byte)this.Character.Class);
            packet.AddString("   ");
            packet.AddChar((byte)this.Character.Level);
            packet.AddChar((byte)this.Character.Gender);
            packet.AddChar((byte)this.Character.HairStyle);
            packet.AddChar((byte)this.Character.HairColor);
            packet.AddChar((byte)this.Character.Skin);
            packet.AddShort((byte)this.Character.MaxHealth);
            packet.AddShort((byte)this.Character.Health);
            packet.AddShort((byte)this.Character.MaxMana);
            packet.AddShort((byte)this.Character.Mana);
            packet.AddShort((ushort)this.Character.Session.GameServer.ItemData[this.Character.Paperdoll[(ushort)EquipLocation.Boots]].special1);
            packet.AddShort(0);
            packet.AddShort(0);
            packet.AddShort(0);
            packet.AddShort((ushort)this.Character.Session.GameServer.ItemData[this.Character.Paperdoll[(ushort)EquipLocation.Armor]].special1);
            packet.AddShort(0);
            packet.AddShort((ushort)this.Character.Session.GameServer.ItemData[this.Character.Paperdoll[(ushort)EquipLocation.Hat]].special1);
            packet.AddShort((ushort)this.Character.Session.GameServer.ItemData[this.Character.Paperdoll[(ushort)EquipLocation.Shield]].special1);
            packet.AddShort((ushort)this.Character.Session.GameServer.ItemData[this.Character.Paperdoll[(ushort)EquipLocation.Weapon]].special1);
            packet.AddChar((byte)this.Character.SitState); // Sitting
            packet.AddChar((byte)(this.Character.Hidden ? 1 : 0)); // Hidden
        }

        internal void SendGameState(GameSession session)
        {
            var reply = new Packet(PacketFamily.Welcome, PacketAction.Reply);
            reply.AddShort((ushort)WelcomeReply.CharacterInfo);
            reply.AddShort(session.PlayerId);
            reply.AddInt(session.PlayerId);
            reply.AddShort(session.Character.MapId);

            if (session.Character.Map != null && session.GameServer.Maps[session.Character.MapId].Type == MapType.PK)
            {
                reply.AddByte(0xFF);
                reply.AddByte(0x01);
            }
            else
            {
                reply.AddByte(session.Character.Map.RevisionId[0]);
                reply.AddByte(session.Character.Map.RevisionId[1]);
            }

            reply.AddByte(session.Character.Map.RevisionId[2]);
            reply.AddByte(session.Character.Map.RevisionId[3]);
            reply.AddThree((int)session.Character.Map.FileSize);

            for (var i = 0; i < 4; ++i)
                reply.AddByte(session.GameServer.ItemData.RevisionId[i]);
            reply.AddShort((ushort)(session.GameServer.ItemData.Count - 1));

            for (var i = 0; i < 4; ++i)
                reply.AddByte(session.GameServer.NPCData.RevisionId[i]);
            reply.AddShort((ushort)(session.GameServer.NPCData.Count - 1));

            for (var i = 0; i < 4; ++i)
                reply.AddByte(session.GameServer.SpellData.RevisionId[i]);
            reply.AddShort((ushort)(session.GameServer.SpellData.Count - 1));

            for (var i = 0; i < 4; ++i)
                reply.AddByte(session.GameServer.ClassData.RevisionId[i]);
            reply.AddShort((ushort)(session.GameServer.ClassData.Count - 1));

            reply.AddBreakString(session.Character.Name);
            reply.AddBreakString(session.Character.Title);
            reply.AddBreakString(""); // Guild Name
            reply.AddBreakString(""); // Guild Rank
            reply.AddChar(0); // class index
            reply.AddString("   "); // Guild tag
            reply.AddChar(1); // Admin
            reply.AddChar(session.Character.Level); // Level
            reply.AddInt(session.Character.Exp); // EXP
            reply.AddInt(0); // Usage
            reply.AddShort(session.Character.Health);
            reply.AddShort(session.Character.MaxHealth);
            reply.AddShort(session.Character.Mana);
            reply.AddShort(session.Character.MaxMana);
            reply.AddShort(session.Character.MaxStamina);
            reply.AddShort(session.Character.StatPoints);
            reply.AddShort(session.Character.SkillPoints);
            reply.AddShort(session.Character.Karma);
            reply.AddShort(session.Character.MinDamage);
            reply.AddShort(session.Character.MaxDamage);
            reply.AddShort(session.Character.Accuracy);
            reply.AddShort(session.Character.Evade);
            reply.AddShort(session.Character.Armor);
            reply.AddShort(session.Character.Strength);
            reply.AddShort(session.Character.Intellect);
            reply.AddShort(session.Character.Wisdom);
            reply.AddShort(session.Character.Agility);
            reply.AddShort(session.Character.Constitution);
            reply.AddShort(session.Character.Charisma);

            reply.AddShort(0); // Elements
            reply.AddShort(0);
            reply.AddShort(0);
            reply.AddShort(0);
            reply.AddShort(0);
            reply.AddShort(0);
            reply.AddShort(0);

            reply.AddChar(0); // TODO: Guild rank
            reply.AddShort(0); // TODO: Jail Map
            reply.AddShort(4); // ?
            reply.AddChar(0xF0); // ?
            reply.AddChar(0xF0); // ?
            reply.AddShort(0xFFF0); // ?
            reply.AddShort(0xFFF0); // ?
            reply.AddShort(1); // Admin command flood rate
            reply.AddShort(1); // ?
            reply.AddChar(0); // Login warning message
            reply.AddBreak();

            session.Send(reply);
        }

        internal void EnterGame()
        {
            if (this.State >= ClientState.Playing)
                throw new InvalidOperationException("The client has already entered the game.");

            this.SetState(ClientState.Playing);
            this.Character.Map.Enter(this.Character, WarpAnimation.None);
        }

        internal void SetState(ClientState state)
            => this.State = state;

        internal void Send(Packet packet)
        {
            var session_identifier = this.State == ClientState.Playing ?
                string.Format("{0} {1}", this.PlayerId, this.Character?.Name ?? "") :
                this.Socket.RemoteEndPoint.ToString();

            this.GameServer.Console.Debug("(server->client) | {client} ({state}) ({family} {action} len: {length})",
                session_identifier,
                this.State,
                packet.Family,
                packet.Action,
                packet.Length);

            if (this.IsConnected)
            {
                var data = packet.Get();

                this.Processor.Encode(ref data);
                this.Send(Packet.EncodeNumber(data.Length, 2).Concat(data).ToArray());
            }
        }

        protected override void OnDisconnected()
        {
            if (this.State == ClientState.Playing)
                if (this.Character != null)
                    this.Character.Logout();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                var length = Packet.DecodeNumber(buffer.Take(2).ToArray());
                var data = buffer.Skip(2).Take(length).ToArray();

                this.Processor.Decode(ref data);
                var message = new Packet(data);

                this.GameServer.Console.Debug("(client->server) {client} ({state}) ({family} {action} len: {length})",
                    this.State == ClientState.Playing ? $"{this.PlayerId} ({this.Character?.Name ?? "N/A"})" : this.Socket.RemoteEndPoint.ToString(),
                    this.State,
                    message.Family,
                    message.Action,
                    message.Length);

                if (message.Family != PacketFamily.Init)
                    message.GetChar();

                foreach (var handler in this.GameServer.MessageHandlers)
                {
                    var attributes = handler.GetType().GetCustomAttributes(typeof(EOMessageHandlerAttribute), false);

                    if (attributes.Any())
                    {
                        var attribute = (EOMessageHandlerAttribute)attributes.First();

                        if (message.Family == attribute.Family && message.Action == attribute.Action && this.State == attribute.State)
                        {
                            handler.OnReceive(this, message);
                            break;
                        }
                    }
                }
            }
            catch (Exception exception) when (Debugger.IsAttached)
            {
                this.GameServer.Console.Error("The client {guid} was disconnected for sending an invalid packet. Details: {details}", this.Id, exception);
            }
        }
    }
}