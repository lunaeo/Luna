using System;

namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Replies;
    using Events;

    [EOMessageHandler(PacketFamily.Emote, PacketAction.Report, ClientState.Playing)]
    public class EmoteHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public EmoteHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            var emoteId = packet.GetChar();

            if (!Enum.IsDefined(typeof(Emote), emoteId))
                return;

            var emote = (Emote)emoteId;

            session.Character.LastEmoteUsed = ((int)emote, DateTime.UtcNow);

            this.GameServer.World.ExecuteTrigger(new EndlessContext(session), new EmoteEvent(session, emote), 20);
            this.GameServer.World.ExecuteTrigger(new EndlessContext(session), new EmoteEvent(session, emote), 21);
        }
    }
}