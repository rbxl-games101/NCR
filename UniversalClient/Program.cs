using NetBlox.Instances.Services;
using Raylib_cs;
using System.Buffers.Text;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NetBlox.Client
{
	public static class Program
	{
		public static int Main(string[] args)
		{
			LogManager.LogInfo($"NCR Client ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");

			Raylib.SetTraceLogLevel(TraceLogLevel.None);

			var v = Rlgl.GetVersion();
			if (v == GlVersion.OpenGl11 || v == GlVersion.OpenGl21)
			{
				Console.WriteLine("NCR cannot run on your device, because OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
				Environment.Exit(1);
			}

#if _WINDOWS
			AppManager.LibraryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "BIG BLOCKS").Replace("\\", "/");
#endif

			// Handle base64 argument
			if (args.Length > 0 && args[0] == "base64")
			{
				if (args.Length > 1)
				{
					string base64str = args[1];
					byte[] base64Bytes = Convert.FromBase64String(base64str); // Use Convert.FromBase64String
					string argstr = Encoding.UTF8.GetString(base64Bytes);
					args = argstr.Split(' ');
				}
				else
				{
					Console.WriteLine("Error: Base64 string is missing.");
					return 1;
				}
			}

			// Handle "check" argument
			if (args.Length == 1 && args[0] == "check")
			{
				return 0;
			}

			// Handle no arguments
			if (args.Length == 0)
			{
				LogManager.LogInfo("No arguments given, redirecting to Public Service's website...");
				if (!File.Exists("./ReferenceData.json"))
				{
					Console.WriteLine("Error: ReferenceData.json file is missing.");
					return 1;
				}

				try
				{
					var referenceData = JsonSerializer.Deserialize<Dictionary<string, string>>(
						File.ReadAllText("./ReferenceData.json"));

					if (referenceData != null && referenceData.TryGetValue("PublicServiceAddress", out var publicServiceAddress))
					{
						Process.Start(new ProcessStartInfo
						{
							FileName = publicServiceAddress,
							UseShellExecute = true
						});
					}
					else
					{
						Console.WriteLine("Error: PublicServiceAddress not found in ReferenceData.json.");
						return 1;
					}
				}
				catch (JsonException ex)
				{
					Console.WriteLine($"Error reading ReferenceData.json: {ex.Message}");
					return 1;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Unexpected error: {ex.Message}");
					return 1;
				}

				return 0;
			}

			// Set up teleportation
			PlatformService.QueuedTeleport = (xo) =>
			{
				var gm = AppManager.GameManagers[0];

				gm.NetworkManager.ClientReplicator = Task.Run(async delegate ()
				{
					try
					{
						gm.NetworkManager.ConnectToServer(IPAddress.Parse(xo));
						return new object();
					}
					catch (Exception ex)
					{
						gm.RenderManager.Status = "Could not connect to the server: " + ex.Message;
						return new();
					}
				}).AsCancellable(gm.NetworkManager.ClientReplicatorCanceller.Token);
			};

			// Create game and set up the application
			GameManager cg = AppManager.CreateGame(new()
			{
				AsClient = true,
				GameName = "CLIENT NCR"
			},
			args, (x) => { });
			cg.MainManager = true;

			AppManager.PlatformOpenBrowser = x =>
			{
				ProcessStartInfo psi = new()
				{
					FileName = x,
					UseShellExecute = true
				};
				Process.Start(psi);
			};

			AppManager.SetRenderTarget(cg);
			AppManager.Start();
			

			return 0;
		}
	}
}
