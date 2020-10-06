namespace LunaServer.Events
{
    /// <summary>
    ///     Occurs when a character stands up.
    /// </summary>
    public sealed class StandEvent : PlayerEvent<StandEvent>
    {
        public StandEvent(GameSession session) : base(session)
        {
        }
    }
}