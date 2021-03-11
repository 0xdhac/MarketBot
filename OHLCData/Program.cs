using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Binance.Net.Objects.Spot;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using System.IO;
using dotenv.net;
using MarketBot.indicators;
using MarketBot.tools;
using System.Drawing;

namespace MarketBot
{
	class Program
	{
		static void Main(string[] args)
		{
			DotEnv.Config(true, "./cfg/api.env");

			string api_key = Environment.GetEnvironmentVariable("BINANCE_API_KEY");
			string secret_key = Environment.GetEnvironmentVariable("BINANCE_SECRET_KEY");

			BinanceClient.SetDefaultOptions(new BinanceClientOptions()
			{
				ApiCredentials = new ApiCredentials(api_key, secret_key),
				LogVerbosity = LogVerbosity.Info,
				LogWriters = new List<TextWriter> { Console.Out }
			});

			BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
			{
				ApiCredentials = new ApiCredentials(api_key, secret_key),
				LogVerbosity = LogVerbosity.Info,
				LogWriters = new List<TextWriter> { Console.Out }
			});

			Commands.Register("screener", ScreenerCommand);
			Commands.Register("backtest", BacktestCommand);
			Commands.Register("bot", BotCommand);
			Commands.Register("help", HelpCommand);

			//new Replay(Exchanges.Binance, "EGLDUSDT", OHLCVInterval.OneMinute, 100000, DateTime.UtcNow);
			//new Replay(Exchanges.Localhost, "./ADAUSDT_OneMinute.csv", OHLCVInterval.OneMinute, 0, null);

			//ExchangeTasks.Screener(Exchanges.Binance, "USDT$", OHLCVInterval.OneMinute, Save);
			//SymbolData data = new SymbolData(Exchanges.Binance, OHLCVInterval.OneMinute, "EGLDUSDT", 150000, Save);



			while (true)
			{
				string command = Console.ReadLine();

				if (!Commands.Execute(command))
				{
					Console.WriteLine($"Command not found: {command}");
				}
			}
		}

		private static void Save(SymbolData data)
		{
			Console.WriteLine("Saving");
			OHLCDataToCSV.Convert(data.Data.Data, $"./{data.Symbol}_{data.Interval}.csv");
			Console.WriteLine("Saved");
		}

		private static void HelpCommand(string[] args)
		{
			foreach(var command in Commands.RegisteredCommands)
			{
				Console.WriteLine($"- {command.Key}");
			}
		}

		private static void BotCommand(string[] args)
		{
			if(args.Length == 1)
			{
				Console.WriteLine(@"
Commands:
- entry <strategy name> (Example: bot entry CMFCrossover 200 20 20)
- exit <strategy name> (Example: bot exit Swing 2)
- profit<value> (Example: bot profit 2)
- start (Starts the bot)");
			}
			else
			{
				if(args[1].Equals("start", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Starting bot. Ctrl+C to STOP.");
					RealtimeBot.Start();
				}
			}
		}

		private static void ScreenerCommand(string[] args)
		{
			if (args.Length == 1)
			{

			}
			else
			{
				if(args[1].Equals("blacklist", StringComparison.OrdinalIgnoreCase))
				{
					if(args.Length <= 4)
					{
						Console.WriteLine("Usage: screener blacklist <remove:add> <exchange> <symbol>");
					}
					else
					{
						Exchanges ex;
						bool success = Enum.TryParse(args[3], out ex);

						if (success)
						{
							string symbol = args[4].ToUpper();
							if(args[2].Equals("remove", StringComparison.OrdinalIgnoreCase))
							{
								if(Blacklist.RemoveSymbol(ex, symbol))
								{
									Console.WriteLine($"Symbol {symbol} removed from blacklist.");
								}
								else
								{
									Console.WriteLine($"Symbol {symbol} not on blacklist.");
								}
							}
							else if(args[2].Equals("add", StringComparison.OrdinalIgnoreCase))
							{
								if(Blacklist.AddSymbol(ex, symbol))
								{
									Console.WriteLine($"Symbol {symbol} blacklisted.");
								}
								else
								{
									Console.WriteLine($"Symbol {symbol} already on blacklist.");
								}
							}
						}
					}
				}
			}
		}

		private static void BacktestCommand(string[] args)
		{
			if (args.Length == 1)
			{

			}
			else
			{

			}
		}
	}
}
