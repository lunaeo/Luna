namespace LunaServer.Handlers
{
    using System.Linq;
    using EndlessOnline;
    using EndlessOnline.Communication;
    using LunaServer.EndlessOnline.Replies;

    [EOMessageHandler(PacketFamily.PaperDoll, PacketAction.Request, ClientState.Playing)]
    public class PaperDollRequestHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public PaperDollRequestHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            var id = packet.GetShort();
            var target = session.GameServer.PlayingSessions.FirstOrDefault(t => t.PlayerId == id)?.Character;

            if (target == null)
                target = session.Character;

            var reply = new Packet(PacketFamily.PaperDoll, PacketAction.Reply);
            reply.AddBreakString(target.Name);
            reply.AddBreakString(target.Home);
            reply.AddBreakString(target.Partner);
            reply.AddBreakString(target.Title);
            reply.AddBreakString(""); // TODO: Guild name
            reply.AddBreakString(""); // TODO: Guild rank
            reply.AddShort(target.Session.PlayerId);
            reply.AddChar(target.Class);
            reply.AddChar((byte)target.Gender);
            reply.AddChar(0);

            foreach (var itemId in target.Paperdoll)
                reply.AddShort(itemId);

            reply.AddChar((byte)PaperdollIcon.Normal);
            session.Send(reply);
        }
    }
}