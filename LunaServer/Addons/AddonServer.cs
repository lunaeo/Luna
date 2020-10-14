using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace LunaServer.Addons
{
    internal class AddonServer : TcpServer
    {
        public GameServer GameServer { get; }

        public AddonServer(IPAddress address, int port, GameServer gameServer) : base(address, port)
        {
            this.GameServer = gameServer;
        }

        protected override TcpSession CreateSession() { return new AddonSession(this, this.GameServer); }

        protected override void OnError(SocketError error)
        {
            this.GameServer.Console.Error("A socket error occured in AddonServer: " + error);
        }
    }
}
