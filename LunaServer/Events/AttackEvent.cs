namespace LunaServer.Events
{
    /// <summary>
    ///     Occurs when a character attacks.
    /// </summary>
    public sealed class AttackEvent : PlayerEvent<AttackEvent>
    {
        public AttackEvent(GameSession session) : base(session)
        {
        }
    }
}