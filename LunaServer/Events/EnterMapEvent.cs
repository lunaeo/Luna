namespace LunaServer.Events
{
    /// <summary>
    ///     Occurs when a character enters a map.
    /// </summary>
    public sealed class EnterMapEvent : PlayerEvent<EnterMapEvent>
    {
        public EnterMapEvent(GameSession session) : base(session)
        {
        }
    }
}