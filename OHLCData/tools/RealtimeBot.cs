using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.skender_strategies.entry_conditions;
using MarketBot.skender_strategies.entry_signals;
using MarketBot.skender_strategies.exit_strategy;
using MarketBot.skender_strategies.price_setter;
using MarketBot.skender_strategies;
using MarketBot.interfaces;
using Skender.Stock.Indicators;

namespace MarketBot.tools
{
	static class RealtimeBot
	{
		public static List<SymbolData> Symbols = new List<SymbolData>();
		public static Dictionary<Exchanges, Dictionary<string, bool>> PositionsLoaded = new Dictionary<Exchanges, Dictionary<string, bool>>();
		public static Dictionary<Exchanges, Dictionary<string, bool>> TradingPairs = new Dictionary<Exchanges, Dictionary<string, bool>>();
		public static Dictionary<Exchanges, Dictionary<string, List<skender_strategies.BaseStrategy>>> Entry_Strategies = new Dictionary<Exchanges, Dictionary<string, List<skender_strategies.BaseStrategy>>>();
		public static Dictionary<Exchanges, Dictionary<string, List<ExitStrategy>>> Exit_Strategies = new Dictionary<Exchanges, Dictionary<string, List<ExitStrategy>>>();
		public static Dictionary<Exchanges, Dictionary<string, List<BaseCondition>>> Entry_Conditions = new Dictionary<Exchanges, Dictionary<string, List<BaseCondition>>>();
		public static Dictionary<Exchanges, Dictionary<string, List<PriceSetter>>> Price_Setters = new Dictionary<Exchanges, Dictionary<string, List<PriceSetter>>>();
		public static Dictionary<Exchanges, Wallet> Wallets = new Dictionary<Exchanges, Wallet>();
		public static Dictionary<Exchanges, List<Position>> Positions = new Dictionary<Exchanges, List<Position>>();
		public static Dictionary<Exchanges, int> Trades = new Dictionary<Exchanges, int>();
		public static Dictionary<Exchanges, int> Wins = new Dictionary<Exchanges, int>();
		public static Dictionary<Exchanges, int> Losses = new Dictionary<Exchanges, int>();
		public static OHLCVInterval Interval = OHLCVInterval.OneMinute;
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

			Program.Print("Loading Symbols");
			SequentialSymbolDownload(Exchanges.Binance);
		}

		private static void SequentialSymbolDownload(Exchanges ex)
		{
			if(Loaded < TradingPairs[ex].Count)
			{
				new SymbolData(ex, Interval, TradingPairs[ex].ElementAt(Loaded++).Key, 2000, SymbolLoaded, true);
			}
			else
			{
				Console.WriteLine("");
			}
		}

		private static void SymbolLoaded(SymbolData data)
		{
			if (data.Data.CollectionFailed)
			{
				return;
			}

			Symbols.Add(data);

			Console.Write(".");

			if (!Entry_Strategies.ContainsKey(data.Exchange))
			{
				Entry_Strategies.Add(data.Exchange, new Dictionary<string, List<skender_strategies.BaseStrategy>>());
				Price_Setters.Add(data.Exchange, new Dictionary<string, List<PriceSetter>>());
				Entry_Conditions.Add(data.Exchange, new Dictionary<string, List<BaseCondition>>());
				Exit_Strategies.Add(data.Exchange, new Dictionary<string, List<ExitStrategy>>());
				PositionsLoaded.Add(data.Exchange, new Dictionary<string, bool>());
			}

			if (!Entry_Strategies[data.Exchange].ContainsKey(data.Symbol))
			{
				Entry_Strategies[data.Exchange][data.Symbol] = new List<skender_strategies.BaseStrategy>();
				Entry_Conditions[data.Exchange][data.Symbol] = new List<BaseCondition>();
				Price_Setters[data.Exchange][data.Symbol] = new List<PriceSetter>();
				Exit_Strategies[data.Exchange][data.Symbol] = new List<ExitStrategy>();
				PositionsLoaded[data.Exchange][data.Symbol] = true;
			}
			
			//Entry_Strategies[data.Exchange][data.Symbol].Add(new CMFCrossover(data, 55, 21));
			//Entry_Strategies[data.Exchange][data.Symbol].Add(new MACDCrossover(data.Data.Periods));
			//Entry_Strategies[data.Exchange][data.Symbol].Add(new Star(data.Data.Periods));
			//Entry_Strategies[data.Exchange][data.Symbol].Add(new ThreelineStrike(data.Data.Periods));
			Entry_Strategies[data.Exchange][data.Symbol].Add(new DICrossover(data.Data.Periods));
			Entry_Strategies[data.Exchange][data.Symbol].Add(new GoldenCross(data.Data.Periods, 9, 48));


			Entry_Conditions[data.Exchange][data.Symbol].Add(new EMACondition(data.Data.Periods, 800));
			Entry_Conditions[data.Exchange][data.Symbol].Add(new SuperTrendCondition(data.Data.Periods));
			//Entry_Conditions[data.Exchange][data.Symbol].Add(new ADXCondition(data.Data.Periods, 30));
			//Entry_Conditions[data.Exchange][data.Symbol].Add(new CMFCondition(data.Data.Periods));
			Price_Setters[data.Exchange][data.Symbol].Add(new ATRRisk(data.Data.Periods, true));
			Price_Setters[data.Exchange][data.Symbol].Add(new ReversedRSIProfit(data.Data.Periods, 14, 77));

			//Exit_Strategies[data.Exchange][data.Symbol].Add(new RSIExit(data.Data.Periods, 77, 40, false));
			Position.LoadSymbolPositions(data.Symbol);
			foreach (var pos in Position.FindPositions(data.Exchange, data.Symbol))
			{
				pos.RiskSetter = Price_Setters[data.Exchange][pos.Symbol][0];
				pos.ProfitSetter = Price_Setters[data.Exchange][pos.Symbol][1];
				pos.PriceHistory = Symbols.FirstOrDefault(s => s.Symbol == pos.Symbol)?.Data;
				pos.InitPos();
			}

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

			SymbolData sym = data as SymbolData;

			int period = sym.Data.Periods.Count - 1;
			//Console.WriteLine($"{sym.Symbol}: Close");
			// If not blacklisted and no discrepency in the symbol data
			if (!TradingPairs[sym.Exchange][sym.Symbol] && !sym.Data.CollectionFailed)
			{
				foreach(var strat in Entry_Strategies[sym.Exchange][sym.Symbol])
				{
					SignalType signal = strat.Run(period);
					if (signal != SignalType.None)
					{
						OnEntrySignal(sym, period, signal);
					}
				}
			}
		}

		public static void OnEntrySignal(SymbolData symbol, int period, SignalType signal)
		{
			if(PositionsLoaded.ContainsKey(symbol.Exchange) == false || PositionsLoaded[symbol.Exchange].ContainsKey(symbol.Symbol) == false)
			{
				Program.Print("Ignored signal: PositionsLoaded = false");
				return;
			}
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

			foreach (var condition in Entry_Conditions[symbol.Exchange][symbol.Symbol])
			{
				if (!condition.GetAllowed(period).Contains(signal))
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

			if(!int.TryParse(Program.GetConfigSetting("NUM_ORDERS_BEFORE_STOPPING_BOT"), out int max_orders))
			{
				throw new Exception($"Config setting not correct: 'NUM_ORDERS_BEFORE_STOPPING_BOT'");
			}

			if (Position.Positions.Count >= max_orders && max_orders != 0)
			{
				Finish = true;
				Program.Print("Stopping bot. Max buy orders reached.");
				return;
			}

			decimal entry_price = ExchangeTasks.GetTickSizeAdjustedValue(Exchanges.Binance, symbol.Symbol, symbol[period].Close);
			decimal profit_price = Price_Setters[symbol.Exchange][symbol.Symbol][1].GetPrice(period, signal);
			decimal risk_price = Price_Setters[symbol.Exchange][symbol.Symbol][0].GetPrice(period, signal);

			//if (Math.Abs(((double)profit_price - (double)entry_price) / (double)entry_price) > 0.2)
			//{
			//	return;
			//}

			if (risk_price >= entry_price || profit_price <= entry_price)
				throw new Exception($"Invalid entry or risk on symbol {symbol.Symbol}: Entry: {entry_price:.00}, Risk: {risk_price:.00}, Profit: {profit_price:.00}");

			// Hardcoded prevention of impossible positions
			if (entry_price - risk_price == 0)
				return;

			// Create position
			decimal quantity = ExchangeTasks.GetStepSizeAdjustedQuantity(Exchanges.Binance, symbol.Symbol, bet_amount / entry_price);
			var pos = new Position(symbol.Exchange, symbol.Symbol, Interval, signal, symbol[period].CloseTime, entry_price, quantity)
			{
				ProfitSetter = Price_Setters[symbol.Exchange][symbol.Symbol][1],
				PriceHistory = symbol.Data,
				Risk = ExchangeTasks.GetTickSizeAdjustedValue(symbol.Exchange, symbol.Symbol, risk_price),
				Profit = ExchangeTasks.GetTickSizeAdjustedValue(symbol.Exchange, symbol.Symbol, profit_price)
			};
			pos.EnterMarket();
		}
	}
}
