using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;
using System.IO;
using System.Linq;
using System.Net;

namespace WindowsGSM.Plugins
{
    public class LastOasis : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.LastOasis", // WindowsGSM.XXXX
            author = "kessef",
            description = "WindowsGSM plugin for supporting LastOasis Dedicated Server",
            version = "1.1",
            url = "https://github.com/dkdue/WindowsGSM.LastOasis", // Github repository link (Best practice)
            color = "#34c9eb" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => false;
        public override string AppId => "920720"; // Game server appId, LastOasis is 920720

        // - Standard Constructor and properties
        public LastOasis(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        public override string StartPath => @"Mist\Binaries\Win64\MistServer-Win64-Shipping.exe"; // Game server start path
        public string FullName = "LastOasis Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 2; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "5555"; // Default port
        public string QueryPort = "27015"; // Default query port
        public string Defaultmap = "neon_server1"; // Default map name
        public string Maxplayers = "100"; // Default maxplayers
        public string Additional = "-CustomerKey=GameServerRegistrationKey -ProviderKey=SelfHostedGameServersRegistrationKey"; // Additional server start parameter


        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
             //Not needed. All config web based from https://myrealm.lastoasis.gg/
        }

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {

			//Get WAN IP from net
            string externalIpString = new WebClient().DownloadString("https://ipv4.icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
            var externalIp = IPAddress.Parse(externalIpString);



            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);

            // Prepare start parameter
			string param = $" -log -force_steamclient_link -messaging -NoLiveServer -EnableCheats -backendapiurloverride=backend-production.last-oasis.com"; // Set basic parameters
			param += string.IsNullOrWhiteSpace(_serverData.ServerMap) ? string.Empty : $" -identifier={_serverData.ServerMap}"; //Use GUI Map config to set server name
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -port={_serverData.ServerPort}"; 
			param += string.IsNullOrWhiteSpace(_serverData.ServerParam) ? string.Empty : $" {_serverData.ServerParam}"; 
			param += string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer) ? string.Empty : $" -slots={_serverData.ServerMaxPlayer}";
			param += string.IsNullOrWhiteSpace(_serverData.ServerQueryPort) ? string.Empty : $" -QueryPort={_serverData.ServerQueryPort}";
            //param += string.IsNullOrWhiteSpace(_serverData.ServerIP) ? string.Empty : $" -OverrideConnectionAddress={_serverData.ServerIP}";
			param += string.IsNullOrWhiteSpace(_serverData.ServerIP) ? string.Empty : $" -OverrideConnectionAddress={externalIp.ToString()}";


			// Saw this in another plugin from Kickbut101. Comment on it is right, this was useful. 
            // Output the startupcommands used. Helpful for troubleshooting server commands and testing them out - leaving this in because it's helpful af.			
			var startupCommandsOutputTxtFile = ServerPath.GetServersServerFiles(_serverData.ServerID, "startupCommandsUsed.log");
            File.WriteAllText(startupCommandsOutputTxtFile, $"{param}");

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath),
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                // Start Process
                try
                {
                    p.Start();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return null; // return null if fail to start
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;
            }

            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }


		// - Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (p.StartInfo.CreateNoWindow)
                {
                    Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                    Functions.ServerConsole.SendWaitToMainWindow("^c");
					
                }
                else
                {
                    Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                    Functions.ServerConsole.SendWaitToMainWindow("^c");
                }
            });
			await Task.Delay(20000);
        }

    }
}
