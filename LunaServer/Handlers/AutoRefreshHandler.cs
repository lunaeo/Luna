namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;

    [EOMessageHandler(PacketFamily.AutoRefresh, PacketAction.Request, ClientState.Playing)]
    public class AutoRefreshHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public AutoRefreshHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            session.Character.Refresh();
        }
    }
}