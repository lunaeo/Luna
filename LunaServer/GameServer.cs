using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using CliWrap;
using NetCoreServer;
using Serilog;
using Serilog.Events;

namespace LunaServer
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Data;
    using EndlessOnline.Replies;
    using Utilities;

    public class GameServer : TcpServer
    {
        public ILogger Console { get; }
        public Random RNG { get; }
        public EIF ItemData { get; }
        public ENF NPCData { get; }
        public ECF ClassData { get; }
        public ESF SpellData { get; }
        public World World { get; }
        public IDictionary<ushort, Map> Maps { get; }
        public GameServerConfiguration GameServerConfiguration { get; }
        public ConsoleConfiguration ConsoleConfiguration { get; }
        public List<EOMessageHandler> MessageHandlers { get; }
        public new List<TcpSession> Sessions { get; }

        /// <summary>
        /// The addon protocol version this version of the server is compatible with.
        /// </summary>
        public int AddonProtocolVersion => 1;

        /// <summary>
        /// An enumerable collection of game sessions whose state is <see cref="ClientState.Playing"/>.
        /// </summary>
        public IEnumerable<GameSession> PlayingSessions =>
            this.Sessions.Where(t => (t as GameSession).State == ClientState.Playing).Cast<GameSession>();

        public GameServer(IPAddress address, int port, GameServerConfiguration server_config, ConsoleConfiguration console_config) : base(address, port)
        {
            this.GameServerConfiguration = server_config;
            this.ConsoleConfiguration = console_config;

            this.RNG = new Random();
            this.Sessions = new List<TcpSession>();

            this.Console = new LoggerConfiguration()
                .MinimumLevel.Is((LogEventLevel)Enum.Parse(typeof(LogEventLevel), console_config.ConsoleMode, true))
                .WriteTo.Console(theme: LunaConsoleTheme.Theme)
                .CreateLogger();

            this.MessageHandlers = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes())
                .Where(n => typeof(EOMessageHandler).IsAssignableFrom(n) && !n.IsAbstract)
                .Select(instance => Activator.CreateInstance(instance, this) as EOMessageHandler).ToList();

            // build new pub files if any changes were made, or if none already exist.
            this.UpdatePubFiles();

            this.ItemData = new EIF(Path.Combine("data", "dat001.eif"));
            this.NPCData = new ENF(Path.Combine("data", "dtn001.enf"));
            this.ClassData = new ECF(Path.Combine("data", "dat001.ecf"));
            this.SpellData = new ESF(Path.Combine("data", "dsl001.esf"));
            this.Maps = new Dictionary<ushort, Map>();

            foreach (var filename in new DirectoryInfo(Path.Combine("data", "maps")).GetFiles("*.emf"))
            {
                var mapId = ushort.Parse(filename.Name[0..^4]);
                var mapInstance = new Map(this, mapId, filename.FullName);

                this.Maps.Add(mapId, mapInstance);
            }

            this.Console.Information("{0} items loaded.", this.ItemData.Count);
            this.Console.Information("{0} npcs loaded.", this.NPCData.Count);
            this.Console.Information("{0} spells loaded.", this.SpellData.Count);
            this.Console.Information("{0} classes loaded.", this.ClassData.Count);
            this.Console.Information("{0} maps loaded.", this.Maps.Count);

            this.World = new World(this);
            this.Console.Information("{0} global scripts loaded.", this.World.GlobalScripts.Count);

            // When everything is starting up,
            foreach (var page in this.World.GlobalScripts)
                page.Execute(null, null, 0);
        }

        private void UpdatePubFiles()
        {
            foreach (var filename in new[] { "dat001.eif", "dtn001.enf", "dat001.ecf", "dsl001.esf" })
            {
                var extension = Path.GetExtension(filename)[1..];

                var input_directory_name =
                    extension == "eif" ? "items" :
                    extension == "enf" ? "npcs" :
                    extension == "ecf" ? "classes" :
                    extension == "esf" ? "spells" :
                    throw new Exception();

                if (File.Exists(Path.Combine("data", filename)))
                {
                    var current = new FileInfo(Path.Combine("data", filename));
                    var latest = new DirectoryInfo(Path.Combine("data", input_directory_name)).GetFiles().OrderByDescending(t => t.LastWriteTimeUtc).First();

                    if (current.LastWriteTimeUtc >= latest.LastWriteTimeUtc)
                        continue;
                }

                // Generate PUB files from the respective JSON files.
                var exit_code = Cli.Wrap(Path.Combine("data", "BuildPub.exe"))
                    .WithWorkingDirectory("data")
                    .WithArguments(new[] { extension.ToUpper(), filename, input_directory_name }).ExecuteAsync().GetAwaiter().GetResult().ExitCode;

                this.Console.Information("{extension} pub file updated.", extension.ToUpper());

                if (exit_code != 0)
                    throw new Exception($"Unable to build '{extension.ToUpper()}' pub file.");
            }
        }

        internal void UploadFile(GameSession session, FileType type, InitReply reply)
        {
            string fileName;

            switch (type)
            {
                case FileType.Map:
                    fileName = string.Format("{0}//{1:d5}.emf", Path.Combine("data", "maps"), session.Character.MapId);
                    break;

                case FileType.Item:
                    fileName = Path.Combine("data", "dat001.eif");
                    break;

                case FileType.NPC:
                    fileName = Path.Combine("data", "dtn001.enf");
                    break;

                case FileType.Spell:
                    fileName = Path.Combine("data", "dsl001.esf");
                    break;

                case FileType.Class:
                    fileName = Path.Combine("data", "dat001.ecf");
                    break;

                default:
                    session.GameServer.Console.Warning("An unknown upload file type was requested: {type}", type);
                    return;
            }

            var data = File.ReadAllBytes(fileName);

            if (type == FileType.Map && session.Character.Map.Type == MapType.PK)
            {
                data[0x03] = 0xFF;
                data[0x04] = 0x01;
            }

            if (type != FileType.Map)
            {
                var builder = new Packet(PacketFamily.Init, PacketAction.Init);
                builder.AddChar((byte)reply);
                builder.AddChar(1);
                builder.AddBytes(data);
                session.Send(builder);
            }
            else
            {
                var builder = new Packet(PacketFamily.Init, PacketAction.Init);
                builder.AddChar((byte)reply);
                builder.AddBytes(data);
                session.Send(builder);
            }
        }

        internal ushort GeneratePlayerUID()
        {
            var i = (ushort)this.RNG.Next(1, 64000);

            restart:
            foreach (var session in this.Sessions.Cast<GameSession>())
            {
                if (session.PlayerId == i)
                {
                    ++i;

                    if (i > ushort.MaxValue)
                        i = 1;

                    goto restart;
                }
            }

            return i;
        }

        protected override void OnConnected(TcpSession session)
            => this.Sessions.Add(session);

        protected override void OnDisconnected(TcpSession session) =>
            this.Sessions.RemoveAll(t => t.Id == session.Id);

        protected override void OnError(SocketError error) =>
            this.Console.Error(error.ToString());

        protected override TcpSession CreateSession() =>
            new GameSession(this) { };
    }
}