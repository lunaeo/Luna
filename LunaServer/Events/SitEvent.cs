namespace LunaServer.Events
{
    /// <summary>
    ///     Occurs when a character sits down.
    /// </summary>
    public sealed class SitEvent : PlayerEvent<SitEvent>
    {
        public SitEvent(GameSession session) : base(session)
        {
        }
    }
}