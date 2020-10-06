using System.Linq;

namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Replies;

    [EOMessageHandler(PacketFamily.Warp, PacketAction.Accept, ClientState.Playing)]
    public class WarpAcceptHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public WarpAcceptHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            var mapId = packet.GetShort();
            var warpAnimation = session.Character.WarpAnimation;

            var updateCharacters = from character in session.Character.Map.Characters
                                   where session.Character.InRange(character)
                                   select character;

            var updateNPCs = from npc in session.Character.Map.MapNPCs
                             where session.Character.InRange(npc) && npc.Alive
                             select npc;

            var updateItems = from item in session.Character.Map.MapItems
                              where session.Character.InRange(item)
                              select item;

            var reply = new Packet(PacketFamily.Warp, PacketAction.Agree);
            reply.AddChar(2); // ?
            reply.AddShort(mapId);
            reply.AddChar((byte)warpAnimation);
            reply.AddChar((byte)updateCharacters.Count());
            reply.AddBreak();

            foreach (var character in updateCharacters)
            {
                character.Session.BuildCharacterPacket(reply);
                reply.AddBreak();
            }

            foreach (var npc in updateNPCs)
            {
                reply.AddChar((byte)npc.Index);
                reply.AddShort((ushort)npc.Id);
                reply.AddChar((byte)npc.X);
                reply.AddChar((byte)npc.Y);
                reply.AddChar((byte)npc.Direction);
            }

            reply.AddBreak();

            foreach (var item in updateItems)
            {
                reply.AddShort(item.UniqueID);
                reply.AddShort((ushort)item.ItemID);
                reply.AddChar(item.X);
                reply.AddChar(item.Y);
                reply.AddThree(item.Amount);
            }

            session.Send(reply);
        }

        [EOMessageHandler(PacketFamily.Warp, PacketAction.Take, ClientState.Playing)]
        public class WarpTakeHandler : EOMessageHandler
        {
            public GameServer GameServer { get; }

            public WarpTakeHandler(GameServer server)
            {
                this.GameServer = server;
            }

            public override void OnReceive(GameSession session, Packet packet)
            {
                session.GameServer.UploadFile(session, FileType.Map, InitReply.Banned);
            }
        }
    }
}