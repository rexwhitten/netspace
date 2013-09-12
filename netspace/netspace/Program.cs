using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using System.IO;

namespace netspace
{
    class Program
    {
        static void C(string message, params string[] args)
        {
            Console.WriteLine(message, args);
        }
        
        static void Main(string[] args)
        {
            string server_ini_file = @"Ini\servers.ini";

            C("HTTP Server Starting");

            if (File.Exists(server_ini_file))
            {
                C("Using File {0} for configuration", server_ini_file);

                FileIniDataParser parser = new FileIniDataParser();
                var servers = parser.LoadFile(server_ini_file);

                servers.Sections
                       .ToList()
                       .ForEach(server_config =>
                       {
                           NSServer server = new NSServer(server_config.Keys["listenon"], Int32.Parse(server_config.Keys["port"]), "/");
                           server.CreateHttpServer();
                           C("Starting Server {0} : listening on http://{1}:{2}/", server_config.SectionName, server_config.Keys["listenon"], server_config.Keys["port"]);
                       });
                C("Servers have been started.");
            }


            Console.WriteLine("Waiting...");
            Console.Read();
        }
    }
}
