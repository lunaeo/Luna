using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using NetCoreServer;

namespace LunaServer.Addons
{
    /// <summary>
    /// Used to add a message handler to the OnMessage event of an instance of AddonConnection.
    /// </summary>
    public delegate void MessageReceivedEventHandler(object sender, AddonMessage e);

    public class AddonSession : TcpSession
    {
        public GameServer GameServer { get; }
        public GameSession GameSession { get; private set; }

        internal BinarySerializer Serializer { get; }
        internal BinaryDeserializer Deserializer { get; }

        /// <summary>
        /// If the session has been initialized with their EO connection.
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// A property used to add a message handler to the OnMessage event of an instance of Connection.
        /// </summary>
        public event MessageReceivedEventHandler OnMessage;

        public AddonSession(TcpServer server, GameServer gameServer) : base(server)
        {
            this.GameServer = gameServer;
            this.Serializer = new BinarySerializer();
            this.Deserializer = new BinaryDeserializer();

            this.Deserializer.OnDeserializedMessage += (e) =>
            {
                if (this.Initialized)
                {
                    this.GameServer.Console.Debug("An addon message was received from {pid} ({name}):\n" + e.ToString(), 
                        this.GameSession.PlayerId, this.GameSession.Character.Name);

                    this.OnMessage?.Invoke(this, e);
                }

                if (e.Type == "init")
                {
                    var version = e.GetInt(0);
                    var sessionId = e.GetString(1);

                    var gameSession = this.GameServer.Sessions.FirstOrDefault(t => t.Id.ToString() == sessionId);
                    if (gameSession == null)
                        this.Disconnect();

                    this.Initialized = true;
                    this.GameSession = (GameSession)gameSession;
                    this.GameSession.AddonConnection = this;
                    this.Send("init", this.GameServer.AddonProtocolVersion);
                }

            };
        }

        public void Send(string type, params object[] parameters) =>
            this.Send(new AddonMessage(type, parameters));

        public void Send(AddonMessage message) => 
            this.Send(this.Serializer.Serialize(message));

        protected override void OnConnected()
        {
        }

        protected override void OnDisconnected()
        {
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            this.Deserializer.AddBytes(buffer.Skip((int)offset).Take((int)size).ToArray());
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Addon TCP session caught an error with code {error}");
        }
    }
}
