namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;

    [EOMessageHandler(PacketFamily.Refresh, PacketAction.Request, ClientState.Playing)]
    public class RefreshRequestHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public RefreshRequestHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            session.Character.Refresh();
        }
    }
}