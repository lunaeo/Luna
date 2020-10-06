namespace LunaServer.Events
{
    using EndlessOnline.Replies;

    /// <summary>
    ///     Occurs when a character walks.
    /// </summary>
    public sealed class WalkEvent : PlayerEvent<WalkEvent>
    {
        public WalkEvent(GameSession session, Direction direction, byte x, byte y) : base(session)
        {
            this.Direction = direction;
            this.X = x;
            this.Y = y;
        }

        public Direction Direction { get; }
        public byte X { get; }
        public byte Y { get; }
    }
}