namespace LunaServer.Events
{
    using EndlessOnline.Replies;

    /// <summary>
    ///     Occurs when a character turns.
    /// </summary>
    public sealed class TurnEvent : PlayerEvent<TurnEvent>
    {
        public TurnEvent(GameSession session, Direction direction) : base(session)
        {
            this.Direction = direction;
        }

        public Direction Direction { get; }
    }
}