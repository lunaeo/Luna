using System.Linq;

namespace LunaServer.Handlers
{
    using System.Net;
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Replies;

    [EOMessageHandler(PacketFamily.Init, PacketAction.Init, ClientState.Uninitialized)]
    public class InitHandler : EOMessageHandler
    {
        public GameServer GameServer { get; }

        public InitHandler(GameServer server)
        {
            this.GameServer = server;
        }

        public override void OnReceive(GameSession session, Packet packet)
        {
            // "secret" hash function the client uses to verify the server
            static uint StupidHash(uint i)
            {
                ++i;
                return 110905 + (i % 9 + 1) * ((11092004 - i) % ((i % 11 + 1) * 119)) * 119 + i % 2004;
            }

            var challenge = packet.GetThree();
            packet.GetChar(); // ?
            packet.GetChar(); // ?
            var version = packet.GetByte();
            packet.GetChar(); // ?
            packet.GetChar(); // ?

            var hdid = uint.Parse(packet.GetEndString());
            var response = StupidHash((uint)challenge);

            var connections_from_ip = this.GameServer.Sessions.Count(t =>
                ((IPEndPoint)t.Socket.RemoteEndPoint).Address.ToString() == ((IPEndPoint)session.Socket.RemoteEndPoint).Address.ToString());

            if (connections_from_ip > this.GameServer.GameServerConfiguration.MaxConnectionsPerIP)
            {
                var init_wait = new Packet(PacketFamily.Init, PacketAction.Init);
                init_wait.AddByte((byte)InitReply.Banned);
                init_wait.AddByte((byte)InitBanType.Temp);
                init_wait.AddByte(1); // 1 minute remaining
                session.Send(init_wait);
                session.Disconnect();
                return;
            }

            // assign a unique id to the session.
            session.PlayerId = this.GameServer.GeneratePlayerUID();
            session.HardDriveId = hdid;

            var init_success = new Packet(PacketFamily.Init, PacketAction.Init);
            init_success.AddByte((byte)InitReply.OK);
            init_success.AddByte(6); // "eID" starting value (1 = +7)
            init_success.AddByte(6); // "eID" starting value (1 = +1)
            init_success.AddByte(6); // e
            init_success.AddByte(6); // d
            init_success.AddShort(session.PlayerId);
            init_success.AddThree((int)response);

            session.Processor.SetMulti(6, 6);
            session.CreateCharacter();
            session.SetState(ClientState.Initialized);
            session.Send(init_success);
        }
    }
}