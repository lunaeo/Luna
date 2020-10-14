using System.Threading;

namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Replies;

    [EOMessageHandler(PacketFamily.Connection, PacketAction.Accept, ClientState.Initialized)]
    public class ConnectionAcceptHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public ConnectionAcceptHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            this.GameServer.UploadFile(session, FileType.Item, InitReply.FileEIF);
            this.GameServer.UploadFile(session, FileType.NPC, InitReply.FileENF);
            this.GameServer.UploadFile(session, FileType.Spell, InitReply.FileESF);
            this.GameServer.UploadFile(session, FileType.Class, InitReply.FileECF);

            // Reference: MEOW (sordie.co.uk)
            // For some reason the client can't handle all this data at once, slow down a bit
            Thread.Sleep(750);

            session.SendGameState(session);

            Thread.Sleep(250);

            // send a packet telling the client to connect to the addon server.
            var addon_announce = new Packet(PacketFamily.Talk, PacketAction.Announce);
            addon_announce.AddBreakString("LunaAddons");
            addon_announce.AddBreakString(this.GameServer.GameServerConfiguration.AddonServerPort + " " +session.Id.ToString());
            session.Send(addon_announce);
        }
    }

    [EOMessageHandler(PacketFamily.Connection, PacketAction.Ping, ClientState.Playing)]
    public class ConnectionPingHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public ConnectionPingHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            session.NeedsPong = false;
        }
    }
}