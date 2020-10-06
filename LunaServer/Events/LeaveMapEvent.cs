namespace LunaServer.Events
{
    /// <summary>
    ///     Occurs when a character leaves a map.
    /// </summary>
    public sealed class LeaveMapEvent : PlayerEvent<LeaveMapEvent>
    {
        public LeaveMapEvent(GameSession session) : base(session)
        {
        }
    }
}