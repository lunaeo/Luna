namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using Events;
    using Addons;

    [EOMessageHandler(PacketFamily.Talk, PacketAction.Report, ClientState.Playing)]
    public class TalkHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public TalkHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            var message = packet.GetEndString();

            if (string.IsNullOrEmpty(message))
                return;

            if (message.Length > 128)
                return;

            if (message.Contains("$r"))
            {
                session.Character.Map.Reload();
            }

            if (message.Contains("$d"))
            {
                session.AddonConnection.Send(new AddonMessage("mutate", 0, 2, 2, 69));
            }

            session.Character.LastMessageSpoken = message;
            this.GameServer.World.ExecuteTrigger(new EndlessContext(session), new TalkEvent(session, message), 30);
            this.GameServer.World.ExecuteTrigger(new EndlessContext(session), new TalkEvent(session, message), 31);
        }
    }
}