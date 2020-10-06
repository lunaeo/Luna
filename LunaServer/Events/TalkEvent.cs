namespace LunaServer.Events
{
    /// <summary>
    ///     Occurs when a character sends a chat message.
    /// </summary>
    public sealed class TalkEvent : PlayerEvent<TalkEvent>
    {
        public TalkEvent(GameSession session, string message) : base(session)
        {
            this.Message = message;
        }

        public string Message { get; }
    }
}