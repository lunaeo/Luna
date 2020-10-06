using System.Collections.Generic;
using System.Linq;

namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Domain.Map;
    using EndlessOnline.Replies;

    [EOMessageHandler(PacketFamily.Welcome, PacketAction.Request, ClientState.Initialized)]
    public class WelcomeRequestHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public WelcomeRequestHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
        }
    }

    [EOMessageHandler(PacketFamily.Welcome, PacketAction.Message, ClientState.Initialized)]
    public class WelcomeMessageHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public WelcomeMessageHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            var welcome_reply = new Packet(PacketFamily.Welcome, PacketAction.Reply);
            welcome_reply.AddShort((ushort)WelcomeReply.WorldInfo);
            welcome_reply.AddBreak();

            welcome_reply.AddBreakString("Luna - https://github.com/LunaEO");
            welcome_reply.AddBreakString("Welcome, this is a fresh installation of Luna Server.");
            welcome_reply.AddBreakString("");
            welcome_reply.AddBreakString("");
            welcome_reply.AddBreakString("");
            welcome_reply.AddBreakString("");
            welcome_reply.AddBreakString("");
            welcome_reply.AddBreakString("");
            welcome_reply.AddBreak();

            welcome_reply.AddChar(session.Character.Weight);
            welcome_reply.AddChar(session.Character.MaxWeight);

            foreach (var item in session.Character.Inventory)
            {
                welcome_reply.AddShort((ushort)item.ItemID);
                welcome_reply.AddInt(item.Amount);
            }

            welcome_reply.AddBreak();
            welcome_reply.AddBreak(); // spells

            session.EnterGame();

            var update_characters = new List<Character>();
            var update_npcs = new List<MapNPC>();
            var update_items = new List<MapItem>();

            foreach (var character in session.Character.Map.Characters)
            {
                if (session.Character.InRange(character))
                    update_characters.Add(character);
            }

            foreach (var npc in session.Character.Map.MapNPCs)
            {
                if (session.Character.InRange(npc))
                    update_npcs.Add(npc);
            }

            foreach (var item in session.Character.Map.MapItems)
            {
                if (session.Character.InRange(item))
                    update_items.Add(item);
            }

            welcome_reply.AddChar((byte)update_characters.Count);
            welcome_reply.AddBreak();

            // CHARACTERS
            foreach (var character in update_characters)
            {
                character.Session.BuildCharacterPacket(welcome_reply);
                welcome_reply.AddBreak();
            }

            // NPCS
            foreach (var npc in update_npcs)
            {
                if (npc.Alive)
                {
                    welcome_reply.AddChar((byte)npc.Index);
                    welcome_reply.AddShort((ushort)npc.Id);
                    welcome_reply.AddChar((byte)npc.X);
                    welcome_reply.AddChar((byte)npc.Y);
                    welcome_reply.AddChar((byte)npc.Direction);
                }
            }

            welcome_reply.AddBreak();

            // ITEMS
            foreach (var item in update_items)
            {
                welcome_reply.AddShort((ushort)item.UniqueID);
                welcome_reply.AddShort((ushort)item.ItemID);
                welcome_reply.AddChar(item.X);
                welcome_reply.AddChar(item.Y);
                welcome_reply.AddThree(item.Amount);
            }

            session.Send(welcome_reply);

            var players_agree = new Packet(PacketFamily.Players, PacketAction.Agree);
            players_agree.AddBreak();
            session.BuildCharacterPacket(players_agree);
            players_agree.AddChar(1);
            players_agree.AddBreak();
            players_agree.AddChar(1);

            foreach (var character in session.Character.Map.Characters)
            {
                if (character.Session.PlayerId == session.PlayerId)
                    continue;

                character.Session.Send(players_agree);
            }
        }
    }

    [EOMessageHandler(PacketFamily.Welcome, PacketAction.Agree, ClientState.Initialized)]
    public class WelcomeAgreeHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public WelcomeAgreeHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            var fileId = (FileType)packet.GetChar();

            switch (fileId)
            {
                case FileType.Map:
                    this.GameServer.UploadFile(session, FileType.Map, InitReply.FileMap);
                    break;

                case FileType.Item:
                    this.GameServer.UploadFile(session, FileType.Item, InitReply.FileEIF);
                    break;

                case FileType.NPC:
                    this.GameServer.UploadFile(session, FileType.NPC, InitReply.FileENF);
                    break;

                case FileType.Spell:
                    this.GameServer.UploadFile(session, FileType.Spell, InitReply.FileESF);
                    break;

                case FileType.Class:
                    this.GameServer.UploadFile(session, FileType.Class, InitReply.FileECF);
                    break;

                default:
                    session.GameServer.Console.Error("Player {player} requested an unknown file type with id: {fileId}", session.Id, fileId);
                    break;
            }
        }
    }
}