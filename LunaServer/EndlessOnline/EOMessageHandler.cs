namespace LunaServer.EndlessOnline
{
    using EndlessOnline.Communication;

    public abstract class EOMessageHandler
    {
        public abstract void OnReceive(GameSession session, Packet packet);
    }
}