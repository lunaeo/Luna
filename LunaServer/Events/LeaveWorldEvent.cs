namespace LunaServer.Events
{
    /// <summary>
    ///     Occurs when a character leaves a world.
    /// </summary>
    public sealed class LeaveWorldEvent : PlayerEvent<LeaveWorldEvent>
    {
        public LeaveWorldEvent(GameSession session) : base(session)
        {
        }
    }
}