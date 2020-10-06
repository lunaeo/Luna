namespace LunaServer.Events
{
    using EndlessOnline.Replies;

    /// <summary>
    ///     Occurs when a character uses an emote.
    /// </summary>
    public sealed class EmoteEvent : PlayerEvent<TurnEvent>
    {
        public EmoteEvent(GameSession session, Emote emote) : base(session)
        {
            this.Emote = emote;
        }

        public Emote Emote { get; }
    }

    /// <summary>
    ///     Occurs when a character drops an item.
    /// </summary>
    public sealed class ItemPickupEvent : PlayerEvent<ItemPickupEvent>
    {
        public ItemPickupEvent(GameSession session, int id, int amount, int x, int y) : base(session)
        {
            this.Id = id;
            this.Amount = amount;
            this.X = x;
            this.Y = y;
        }

        public int Id { get; }
        public int Amount { get; }
        public int X { get; }
        public int Y { get; }
    }

    /// <summary>
    ///     Occurs when a character drops an item.
    /// </summary>
    public sealed class ItemDropEvent : PlayerEvent<ItemDropEvent>
    {
        public ItemDropEvent(GameSession session, int id, int amount, int x, int y) : base(session)
        {
            this.Id = id;
            this.Amount = amount;
            this.X = x;
            this.Y = y;
        }

        public int Id { get; }
        public int Amount { get; }
        public int X { get; }
        public int Y { get; }
    }
}