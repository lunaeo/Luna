using MoonScript;

namespace LunaServer
{
    public class EndlessContext : IContext
    {
        public GameSession GameSession { get; set; }
        public Character Character => this.GameSession.Character;

        public EndlessContext(GameSession session)
        {
            this.GameSession = session;
        }
    }
}