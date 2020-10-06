namespace LunaServer.Events
{
    /// <summary>
    ///     Occurs when a character warps.
    /// </summary>
    public sealed class WarpEvent : PlayerEvent<WarpEvent>
    {
        public WarpEvent(GameSession session) : base(session)
        {
        }
    }
}