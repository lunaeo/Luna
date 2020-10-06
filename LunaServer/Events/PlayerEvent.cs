namespace LunaServer.Events
{
    public abstract class PlayerEvent<T>
    {
        public GameSession Session { get; protected set; }
        public Character Character => this.Session.Character;

        internal PlayerEvent(GameSession session)
        {
            this.Session = session;
        }
    }
}