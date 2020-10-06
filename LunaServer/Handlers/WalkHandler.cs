using System;

namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Replies;
    using Events;

    public static class CommonWalker
    {
        public static void CommonWalk(Packet packet, GameSession session, bool admin)
        {
            var directionId = packet.GetChar();
            packet.GetThree(); // timestamp
            var x = packet.GetChar();
            var y = packet.GetChar();

            if (!Enum.IsDefined(typeof(Direction), directionId))
                return;

            if ((SitState)session.Character.SitState != SitState.Stand)
                return;

            var direction = (Direction)directionId;

            if (session.Character.Walk(direction, admin))
            {
                session.Character.PX = x;
                session.Character.PY = y;
                session.GameServer.World.ExecuteTrigger(new EndlessContext(session), new WalkEvent(session, direction, x, y), 1);

                switch (direction)
                {
                    case Direction.Down:
                        session.GameServer.World.ExecuteTrigger(new EndlessContext(session), new WalkEvent(session, direction, x, y), 62);
                        break;

                    case Direction.Left:
                        session.GameServer.World.ExecuteTrigger(new EndlessContext(session), new WalkEvent(session, direction, x, y), 63);
                        break;

                    case Direction.Up:
                        session.GameServer.World.ExecuteTrigger(new EndlessContext(session), new WalkEvent(session, direction, x, y), 60);
                        break;

                    case Direction.Right:
                        session.GameServer.World.ExecuteTrigger(new EndlessContext(session), new WalkEvent(session, direction, x, y), 61);
                        break;
                }
            }
            else return;

            // TODO: Check whether the player is currently being warped, otherwise this leads to a graphical glitch.
            //if (session.Character.X != x || session.Character.Y != y)
            //    session.Character.Refresh();
        }
    }

    [EOMessageHandler(PacketFamily.Walk, PacketAction.Admin, ClientState.Playing)]
    public class WalkAdminHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public WalkAdminHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            CommonWalker.CommonWalk(packet, session, true);
        }
    }

    [EOMessageHandler(PacketFamily.Walk, PacketAction.Player, ClientState.Playing)]
    public class WalkHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public WalkHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            CommonWalker.CommonWalk(packet, session, false);
        }
    }
}