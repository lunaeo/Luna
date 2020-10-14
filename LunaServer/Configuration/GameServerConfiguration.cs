namespace LunaServer
{
    public class GameServerConfiguration
    {
        /// <summary>
        /// The IP address the server should listen on.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The port the server should listen on.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// The port the addon server should listen on.
        /// </summary>
        public int AddonServerPort { get; set; }

        /// <summary>
        /// The maximum amount of connections that can be established per IP.
        /// </summary>
        public int MaxConnectionsPerIP { get; set; } = 5;

        /// <summary>
        /// How often (in milliseconds) to send a ping to connected players.
        /// </summary>
        public int PingRate { get; set; } = 60 * 1000;
    }
}