namespace LunaServer.Addons
{
    using System.Linq;
    using EndlessOnline;
    using EndlessOnline.Communication;

    [EOMessageHandler(PacketFamily.AutoRefresh, PacketAction.Init, ClientState.Initialized)]
    public class AddonMessageHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public AddonMessageHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            // skip 3 bytes (family, action, sequence)
            var buffer = packet.Get().Skip(3).ToArray();

            session.AddonConnection.MessageDeserializer.AddBytes(buffer);
        }
    }
}
