namespace LunaServer.Handlers
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Domain.Map;
    using Events;

    [EOMessageHandler(PacketFamily.Item, PacketAction.Drop, ClientState.Playing)]
    public class ItemDropHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public ItemDropHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            var id = packet.GetShort();
            var amount = (packet.Length == 10) ? packet.GetThree() : packet.GetInt();
            var x = packet.GetChar();
            var y = packet.GetChar();

            if (x == 254 && y == 254)
            {
                x = session.Character.X;
                y = session.Character.Y;
            }

            if (!session.Character.Map.Walkable(x, y, false))
                return;

            session.Character.LastItemDropped = (x, y, (MapItem)new MapItem(0, (short)id, 0, 0).WithAmount(amount));

            this.GameServer.World.ExecuteTrigger(new EndlessContext(session), new ItemDropEvent(session, id, amount, x, y), 100);
            this.GameServer.World.ExecuteTrigger(new EndlessContext(session), new ItemDropEvent(session, id, amount, x, y), 101);
            this.GameServer.World.ExecuteTrigger(new EndlessContext(session), new ItemDropEvent(session, id, amount, x, y), 102);
        }
    }

    [EOMessageHandler(PacketFamily.Item, PacketAction.Get, ClientState.Playing)]
    public class ItemGetHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public ItemGetHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            var uid = packet.GetShort();
            var item = session.Character.Map.GetItem(uid);

            session.Character.LastItemPickedUp = (item.X, item.Y, item);

            this.GameServer.World.ExecuteTrigger(new EndlessContext(session), new ItemPickupEvent(session, item.ItemID, item.Amount, item.X, item.Y), 103);
            this.GameServer.World.ExecuteTrigger(new EndlessContext(session), new ItemPickupEvent(session, item.ItemID, item.Amount, item.X, item.Y), 104);
        }
    }
}