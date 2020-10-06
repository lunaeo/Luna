using System;

namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Replies;

    [EOMessageHandler(PacketFamily.Sit, PacketAction.Request, ClientState.Playing)]
    public class SitHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public SitHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            var commandId = packet.GetChar();

            if (!Enum.IsDefined(typeof(SitCommand), commandId))
                return;

            var action = (SitCommand)commandId;

            switch (action)
            {
                case SitCommand.Sitting when session.Character.SitState == (byte)SitState.Stand:
                    session.Character.Sit(SitState.Floor);
                    break;

                default:
                    if (session.Character.SitState == (byte)SitState.Floor)
                        session.Character.Stand();
                    break;
            }
        }
    }
}