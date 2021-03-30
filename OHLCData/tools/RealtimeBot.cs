using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.strategies.signals;
using MarketBot.interfaces;
using MarketBot.strategies.position;
using MarketBot.indicators;
using MarketBot.strategies.condition;

namespace MarketBot.tools
{
	static class RealtimeBot
	{
		public static List<SymbolData> Symbols = new List<SymbolData>();
		public static Dictionary<Exchanges, Dictionary<string, bool>> TradingPairs = new Dictionary<Exchanges, Dictionary<string, bool>>();
		public static Dictionary<Exchanges, Dictionary<string, List<EntrySignaler>>> Entry_Strategies = new Dictionary<Exchanges, Dictionary<string, List<EntrySignaler>>>();
		public static Dictionary<Exchanges, Dictionary<string, List<RiskStrategy>>> Risk_Strategies = new Dictionary<Exchanges, Dictionary<string, List<RiskStrategy>>>();
		public static Dictionary<Exchanges, Wallet> Wallets = new Dictionary<Exchanges, Wallet>();
		public static Dictionary<Exchanges, List<Position>> Positions = new Dictionary<Exchanges, List<Position>>();
		public static Dictionary<Exchanges, int> Trades = new Dictionary<Exchanges, int>();
		public static Dictionary<Exchanges, int> Wins = new Dictionary<Exchanges, int>();
		public static Dictionary<Exchanges, int> Losses = new Dictionary<Exchanges, int>();
		public static bool Finish = false;
		public static int BuyCount = 0;
		private static decimal BetAmount = 0;
		private static decimal BetPct = (decimal)0.05;

		public static int ResetBetAmountEvery = 1;

		private static int Loaded = 0;

		public static void Start()
		{
			if(!decimal.TryParse(Program.GetConfigSetting("BET_PERCENTAGE"), out BetPct))
			{
				throw new Exception("Config setting not correct: 'BET_PERCENTAGE'");
			}

			Trades.Add(Exchanges.Binance, 0);
			Wins.Add(Exchanges.Binance, 0);
			Losses.Add(Exchanges.Binance, 0);

			ExchangeTasks.UpdateExchangeInfo(Exchanges.Binance);
			ExchangeTasks.GetWallet(Exchanges.Binance, OnWalletLoaded);
			ExchangeTasks.LoadPositions(Exchanges.Binance);
			ExchangeTasks.GetTradingPairs(Exchanges.Binance, Program.GetConfigSetting("BINANCE_SYMBOL_REGEX"), TradingPairsRetrieved);
			ExchangeTasks.CreateUserStream(Exchanges.Binance);
		}

		public static void TradingPairsRetrieved(Exchanges ex, List<string> data)
		{
			// Save trading pairs to list including whether or not the pair is blacklisted in the config file
			Dictionary<string, bool> pairs_blacklist_combined = new Dictionary<string, bool>();

			foreach(var pair in data)
			{
				pairs_blacklist_combined.Add(pair, Blacklist.IsBlacklisted(ex, pair));
			}

			TradingPairs.Add(ex, pairs_blacklist_combined);

			// Create SymbolData objects for all the pairs, regardless of whether or not they are blacklisted
			List<string> unloaded_symbols = new List<string>();
			foreach(var exchange_list in TradingPairs)
			{
				if(exchange_list.Key == ex)
				{
					foreach (var symbol in exchange_list.Value)
					{
						unloaded_symbols.Add(symbol.Key);
					}

					break;
				}
			}

			SequentialSymbolDownload(Exchanges.Binance);
		}

		private static void SequentialSymbolDownload(Exchanges ex)
		{
			if(Loaded < TradingPairs[ex].Count)
			{
				new SymbolData(ex, OHLCVInterval.ThirtyMinute, TradingPairs[ex].ElementAt(Loaded++).Key, 2000, SymbolLoaded, true);
			}
		}

		private static void SymbolLoaded(SymbolData data)
		{
			if (data.Data.CollectionFailed)
			{
				return;
			}

			Symbols.Add(data);

			if (!Entry_Strategies.ContainsKey(data.Exchange))
			{
				Entry_Strategies.Add(data.Exchange, new Dictionary<string, List<EntrySignaler>>());
				Risk_Strategies.Add(data.Exchange, new Dictionary<string, List<RiskStrategy>>());
			}

			if (!Entry_Strategies[data.Exchange].ContainsKey(data.Symbol))
			{
				Entry_Strategies[data.Exchange][data.Symbol] = new List<EntrySignaler>();
				Risk_Strategies[data.Exchange][data.Symbol] = new List<RiskStrategy>();
			}

			//Entry_Strategies[data.Exchange][data.Symbol].Add(new CMFCrossover(data, 55, 21));
			//Entry_Strategies[data.Exchange][data.Symbol].Add(new MACDCrossover(data, 12, 26, 9));
			Entry_Strategies[data.Exchange][data.Symbol].Add(new Star(data));
			//Entry_Strategies[data.Exchange][data.Symbol].Add(new ThreelineStrike(data));

			EMACondition cond1 = new EMACondition(data, 200);
			ADXCondition cond2 = new ADXCondition(data, 14);
			foreach(var strat in Entry_Strategies[data.Exchange][data.Symbol])
			{
				strat.Add(cond1);
				strat.Add(cond2);
			}

			Risk_Strategies[data.Exchange][data.Symbol].Add(new ATRRisk(data));

			SequentialSymbolDownload(data.Exchange);

			data.PeriodClosed += OnClose;
		}

		public static void OnWalletLoaded(object sender, EventArgs e)
		{
			Wallet w = (Wallet)sender;

			if (Wallets.ContainsKey(w.Exchange))
			{
				Wallets[w.Exchange] = w;
			}
			else
			{
				Wallets.Add(w.Exchange, w);
			}

			SetBetAmount(w.Exchange);
		}

		public static void SetBetAmount(Exchanges ex)
		{
			if (!Wallets.ContainsKey(ex))
			{
				throw new Exception($"SetBetAmount failed: Wallet does not contain key '{ex}'");
			}

			BetAmount = Wallets[ex].Total * BetPct;
		}

		public static void OnClose(object data, EventArgs e)
		{
			if (Finish == true)
			{
				return;
			}

			SymbolData sym = (SymbolData)data;
			//Console.WriteLine($"{sym.Symbol}: Close");
			// If not blacklisted and no discrepency in the symbol data
			if (!TradingPairs[sym.Exchange][sym.Symbol] && !sym.Data.CollectionFailed)
			{
				foreach(var strat in Entry_Strategies[sym.Exchange][sym.Symbol])
				{
					strat.Run(sym.Data.Periods.Count - 1, OnEntrySignal);
				}
			}
		}

		public static void OnEntrySignal(SymbolData symbol, int period, SignalType signal)
		{
			// Disable short positions for now
			if(signal == SignalType.Short)
			{
				return;
			}

			// Make sure the positions list exists for this exchange
			if (!Positions.ContainsKey(symbol.Exchange))
			{
				Positions.Add(symbol.Exchange, new List<Position>());
			}

			foreach (var position in Position.Positions)
			{
				if (position.Symbol == symbol.Symbol)
				{
					return;
				}
			}

			decimal bet_amount = BetAmount;
			if(BetAmount > Wallets[symbol.Exchange].Available)
			{
				if(Wallets[symbol.Exchange].Available > bet_amount)
				{
					return;
				}
			}

			int max_orders = int.Parse(Program.GetConfigSetting("NUM_ORDERS_BEFORE_STOPPING_BOT"));
			if (BuyCount >= max_orders && max_orders != 0)
			{
				Finish = true;
				Program.Print("Stopping bot. Max buy orders reached.");
				return;
			}

			// Calculate position
			decimal entry_price = symbol.Data[period].Close;
			decimal risk_price = Risk_Strategies[symbol.Exchange][symbol.Symbol][0].GetRiskPrice(period, signal);
			decimal profit = 3;
			decimal profit_price = ((entry_price - risk_price) * profit) + entry_price;

			// Hardcoded prevention of impossible positions
			if (entry_price - risk_price == 0)
				return;

			// Create position
			var pos = new Position(symbol, period, signal, entry_price, risk_price, profit_price, bet_amount / entry_price, true);
			pos.CreateOrder();
		}
	}
}
