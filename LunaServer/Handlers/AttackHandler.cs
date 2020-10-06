namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Replies;
    using Events;

    [EOMessageHandler(PacketFamily.Attack, PacketAction.Use, ClientState.Playing)]
    public class AttackHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public AttackHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            if (session.Character.SitState != (byte)SitState.Stand)
                return;

            session.Character.Attack(session.Character.Direction);
            session.GameServer.World.ExecuteTrigger(new EndlessContext(session), new AttackEvent(session), 16);
        }
    }
}