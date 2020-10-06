using System;

namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Replies;
    using Events;

    [EOMessageHandler(PacketFamily.Face, PacketAction.Player, ClientState.Playing)]
    public class FaceHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public FaceHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            var directionId = packet.GetChar();

            if (!Enum.IsDefined(typeof(Direction), directionId))
                return;

            var direction = (Direction)directionId;

            if (session.Character.SitState != (byte)SitState.Stand)
                return;

            session.Character.Face(direction);
            session.GameServer.World.ExecuteTrigger(new EndlessContext(session), new TurnEvent(session, direction), 10);
        }
    }

    [EOMessageHandler(PacketFamily.Door, PacketAction.Open, ClientState.Playing)]
    public class DoorOpenHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public DoorOpenHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            var x = packet.GetChar();
            var y = packet.GetChar();

            session.GameServer.World.ExecuteTrigger(new EndlessContext(session), new DoorOpenEvent(session, x, y), 150);
            session.GameServer.World.ExecuteTrigger(new EndlessContext(session), new DoorOpenEvent(session, x, y), 151);
        }
    }
}