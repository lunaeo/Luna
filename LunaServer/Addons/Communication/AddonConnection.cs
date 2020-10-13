using System.IO;

namespace LunaServer.Addons
{
    using EndlessOnline.Communication;
    using LunaServer.Utilities;

    /// <summary> Used to add a message handler to the OnMessage event of an instance of AddonConnection. </summary>
    public delegate void MessageReceivedEventHandler(object sender, AddonMessage e);

    public class AddonConnection
    {
        public GameSession Session { get; }

        /// <summary>
        /// A property used to add a message handler to the OnMessage event of an instance of Connection.
        /// </summary>
        public event MessageReceivedEventHandler OnMessage;

        internal AddonConnection(GameSession session)
        {
            this.MessageDeserializer = new BinaryDeserializer();
            this.Session = session;

            this.MessageDeserializer.OnDeserializedMessage += (message) => {
                this.OnMessage?.Invoke(this, message);
            };
        }

        public void Send(AddonMessage message)
        {
            var serialized = new BinarySerializer().Serialize(message);

            var chunks = serialized.AsChunks(2048);

            foreach (var chunk in chunks)
            {
                var reply = new Packet(PacketFamily.AutoRefresh, PacketAction.Init);
                reply.AddBytes(chunk);
                this.Session.Send(reply);
            }
        }

        internal readonly BinaryDeserializer MessageDeserializer;
    }
}
