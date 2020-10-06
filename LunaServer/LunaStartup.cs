using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LunaServer
{
    internal class LunaStartup
    {
        private static readonly string _header =
@"      _.._
    .' .-'`                Luna
   /  /        Endless Online Server Emulator
   |  |
   \  '.___.;  ""It's hard to light a candle,
    '._  _.'      easy to curse the dark instead.""
       ``";

        private static async Task Main(string[] args)
        {
            static void write_subheader(string header, string message)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(header);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(": " + message + "\n");
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Write(_header);
            Console.WriteLine();
            write_subheader("version", "v" + FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).FileVersion[..5]);
            write_subheader("maintainer", FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).CompanyName);
            write_subheader("repository", "https://github.com/LunaEO/LunaServer");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(new string('-', Console.BufferWidth));
            Console.ResetColor();

            var server_config = JsonConvert.DeserializeObject<GameServerConfiguration>(File.ReadAllText(Path.Combine("config", "server.json")));
            var console_config = JsonConvert.DeserializeObject<ConsoleConfiguration>(File.ReadAllText(Path.Combine("config", "console.json")));
            var server = new GameServer(IPAddress.Parse(server_config.Host), server_config.Port, server_config, console_config);

            if (server.Start())
                server.Console.Information("Listening on {address}:{port}", server.Endpoint.Address, server.Endpoint.Port);
            else server.Console.Error("Unable to start game server. The specified address and port may already be in use.");

            await Task.Delay(-1);
        }
    }
}