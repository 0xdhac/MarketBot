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
using MarketBot.indicators;
using MarketBot.tools;
using MarketBot.exchanges.binance;
using System.Drawing;
using System.Configuration;
using MarketBot.interfaces;
using NLog;

namespace MarketBot
{
	class Program
	{
		static void SetupLogger()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "./logs/log.txt" };

			// Rules for mapping loggers to targets         
			config.AddRuleForAllLevels(logfile);

			// Apply config           
			LogManager.Configuration = config;
		}

		public static void Log(string log)
		{
			LogManager.GetCurrentClassLogger().Info(log);
		}
		
		public static void LogError(string log)
		{
			LogManager.GetCurrentClassLogger().Error(log);
		}

		static void Main(string[] args)
		{
			// Initialize logger settings
			SetupLogger();

			string api_key = ConfigurationManager.AppSettings["BINANCE_API_KEY"];
			string secret_key = ConfigurationManager.AppSettings["BINANCE_SECRET_KEY"];

			BinanceClient.SetDefaultOptions(new BinanceClientOptions()
			{
				ApiCredentials = new ApiCredentials(api_key, secret_key),
				LogVerbosity = LogVerbosity.Error,
				LogWriters = new List<TextWriter> { Console.Out }
			});

			BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
			{
				ApiCredentials = new ApiCredentials(api_key, secret_key),
				LogVerbosity = LogVerbosity.Error,
				LogWriters = new List<TextWriter> { Console.Out }
			});

			Commands.Register("screener", ScreenerCommand);
			Commands.Register("backtest", BacktestCommand);
			Commands.Register("bot", BotCommand);
			Commands.Register("help", HelpCommand);

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("MarketBot\n");

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine("Commands:");
			Commands.Execute("help");
			Console.WriteLine("");

			//new Replay(Exchanges.Binance, "DOGEUSDT", OHLCVInterval.OneMinute, 1000, DateTime.UtcNow);
			//new Replay(Exchanges.Localhost, "./DOTUSDT_OneMinute.csv", OHLCVInterval.FiveMinute, 0, null);

			while (true)
			{
				//Console.Write("> ");
				//Console.SetCursorPosition(2, Console.CursorTop);
				string command = Console.ReadLine();

				if (!Commands.Execute(command))
				{
					Console.WriteLine($"- Command not found: {command}");
				}
			}
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
				Console.WriteLine(@"Commands:
- entry <strategy name> (Example: bot entry ""CMFCrossover 200 20 20"")
- exit <strategy name> (Example: bot exit Swing 2)
- profit <value> (Example: bot profit 2)
- start (Starts the bot)");
			}
			else
			{
				if(args[1].Equals("start", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Starting bot. Ctrl+C to STOP.");
					RealtimeBot.Start();
				}
				if(args[1].Equals("finish", StringComparison.OrdinalIgnoreCase))
				{
					RealtimeBot.Finish = true;
					Console.WriteLine("Now ignoring all signals.");
				}
			}
		}

		private static void ScreenerCommand(string[] args)
		{
			if (args.Length == 1)
			{
				Console.WriteLine(@"- blacklist <add:remove> <exchange> <symbol> (Example: screener blacklist remove binance spot BTCUSDT)
- filter <exchange> <regex pattern> (Default USDT$)");
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
						bool success = Enum.TryParse(args[3], true, out ex);

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
				else if(args[1].Equals("filter", StringComparison.OrdinalIgnoreCase))
				{					
					if(args.Length < 4)
					{
						Console.WriteLine("Usage: screener filter <exchange> <expression>\nExample: screener filter binance \"USDT$\"");
					}
					else
					{
						string config_setting = args[2].ToUpper() + "_SYMBOL_REGEX";
						if(UpdateConfigSetting(config_setting, args[3]))
						{
							Console.WriteLine($"Setting {config_setting} updated to value {args[3]}.");
						}
						//Program.UpdateConfigSetting()
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

		public static bool UpdateConfigSetting(string setting, string value)
		{
			Configuration configManager = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			KeyValueConfigurationCollection confCollection = configManager.AppSettings.Settings;
			if(confCollection.AllKeys.Contains(setting))
			{
				confCollection[setting].Value = value;
				configManager.Save();
				return true;
			}

			return false;
		}

		public static string GetConfigSetting(string setting)
		{
			return ConfigurationManager.AppSettings[setting];
		}

		public static void Print(string input)
		{
			Console.Write($"{input}\n");
			
			Console.Write("> ");
			Console.SetCursorPosition(2, Console.CursorTop);
		}
	}
}
