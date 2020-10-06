namespace LunaServer.Events
{
    /// <summary>
    ///     Occurs when a character opens a door.
    /// </summary>
    public sealed class DoorOpenEvent : PlayerEvent<DoorOpenEvent>
    {
        public DoorOpenEvent(GameSession session, int x, int y) : base(session)
        {
            this.X = x;
            this.Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }
}