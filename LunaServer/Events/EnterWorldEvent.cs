namespace LunaServer.Events
{
    /// <summary>
    ///     Occurs when a character enters a world.
    /// </summary>
    public sealed class EnterWorldEvent : PlayerEvent<EnterWorldEvent>
    {
        public EnterWorldEvent(GameSession session) : base(session)
        {
        }
    }
}