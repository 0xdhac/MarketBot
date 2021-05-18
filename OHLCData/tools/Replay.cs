using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using System.IO;
using MarketBot.tools;
using MarketBot.indicators;
using MarketBot.skender_strategies.exit_strategy;
using MarketBot.skender_strategies.entry_signals;
using MarketBot.skender_strategies.price_setter;
using MarketBot.skender_strategies.entry_conditions;
using Skender.Stock.Indicators;
using System.Diagnostics;
using MarketBot.skender_strategies;
using MarketBot.exchanges.localhost;

namespace MarketBot
{
	enum BetResizeMode
	{
		Counter,
		Win,
		Loss
	}
	class Replay
	{
		public List<BaseStrategy> Entry_Strategy = new List<BaseStrategy>();
		public PriceSetter Risk_Setter;
		public PriceSetter Profit_Setter;
		//public PriceSetter Reward_Setter;
		public List<BaseCondition> Entry_Conditions = new List<BaseCondition>();
		public List<ExitStrategy> Exit_Strategies = new List<ExitStrategy>();
		public SymbolData Symbol;

		private int Wins = 0;
		private int Losses = 0;
		private int Trades = 0;
		private int TradePeriodsTotal = 0;
		public Balance Balance = new Balance()
		{
			Total = 10000,
			Starting = 10000,
			Available = 10000
		};

		public BetSizer BetSizer = new BetSizer()
		{
			BetPercent = (decimal)0.05,
			ResizeEvery = 1,
		};

		private decimal TotalRisk = 0;
		private decimal TotalProfit = 0;
		private TimeSpan TimeInLosing = new TimeSpan();
		private TimeSpan TimeInWinning = new TimeSpan();
		private TimeSpan TimeInTrades = new TimeSpan();
		public int NextPeriod
		{
			get;
			private set;
		} = 0;

		public bool Finished
		{
			get;
			private set;
		} = false;

		public EventHandler OnReplayFinished = null;
		private OHLCVInterval Interval;
		private bool BuyInPos = bool.Parse(Program.GetConfigSetting("BUY_WHEN_IN_POSITION"));

		public Replay(string symbol, OHLCVInterval interval)
		{
			Interval = interval;
			BetSizer.Balance = Balance;
			BetSizer.UpdateBetAmount();
			var pattern = symbol + "-" + BinanceAnalyzer.GetKlineInterval(interval) + "*";
			var files = Directory.GetFiles(Program.GetConfigSetting("KLINE_DATA_FOLDER"), pattern).OrderBy(v1 => v1).ToArray();

			if(files.Length == 0)
			{
				Console.WriteLine($"No files for symbol {symbol} found on interval {interval}.");
				return;
			}

			Symbol = new SymbolData(symbol, files, interval, CSVConversionMethod.BinanceVision);
		}

		public Replay(string symbol, string[] files, OHLCVInterval interval)
		{
			Interval = interval;
			BetSizer.Balance = Balance;
			BetSizer.UpdateBetAmount();
			Symbol = new SymbolData(symbol, files, interval, CSVConversionMethod.BinanceVision);
		}

		public bool RunNextPeriod()
		{
			if (Finished)
			{
				throw new Exception("Can't run next replay period. Replay is finished");
			}

			var positions = Position.FindPositions(Exchanges.Localhost, Symbol.Symbol);
			if (NextPeriod >= Symbol.Data.Periods.Count)
			{
				foreach(var pos in positions)
				{
					Balance.Available += pos.Entry * pos.DesiredQuantity;
				}

				Position.Positions.RemoveAll(p => p.Symbol == Symbol.Symbol);
				Finished = true;

				if(OnReplayFinished != null)
					OnReplayFinished(this, null);

				return false;
			}

			foreach (var strat in Entry_Strategy)
			{
				var signal = strat.Run(NextPeriod);
				if (signal != SignalType.None)
				{
					OnReplaySignal(NextPeriod, signal);
				}
			}

			foreach (var position in positions)
			{
				((OCOExit)position.ExitStrategies[0]).Profit = Profit_Setter.GetPrice(NextPeriod, position.Type);
				CheckForExit(NextPeriod, position);
			}

			NextPeriod++;

			return true;
		}

		public void Reset()
		{
			NextPeriod = 0;
			Finished = false;
			Wins = 0;
			Losses = 0;
			Trades = 0;
			TradePeriodsTotal = 0;
			TotalRisk = 0;
			TotalProfit = 0;
			TimeInLosing = new TimeSpan();
			TimeInWinning = new TimeSpan();
			TimeInTrades = new TimeSpan();
		}

		public void Run()
		{
			while (RunNextPeriod());

			ReplayResults r = Results();
			
			TimeSpan days = new TimeSpan(ExchangeTasks.GetOHLCVIntervalTimeSpan(Interval).Ticks * Symbol.Data.Periods.Count);

			Console.WriteLine("--------------------------------");
			PaddedPrint("Symbol", Symbol.Symbol);
			PaddedPrint("Interval", $"{Symbol.Interval}");
			PaddedPrint("Periods", $"{Symbol.Data.Periods.Count} ({days.TotalDays:.00}d)");
			PaddedPrint("Entry Strategy", Entry_Strategy.ToString());
			//PaddedPrint("Exit Strategy", Exit_Strategy.ToString());
			PaddedPrint("Average Trade Periods", $"{(TradePeriodsTotal / (float)Trades):.00}");
			PaddedPrint("Time In Trades", $"{TimeInTrades.TotalDays:.00}d");
			//PaddedPrint("Average Risk", $"{(TotalRisk / (trades) * 100):0.0000}%");
			PaddedPrint("Wins", $"{Wins}");
			PaddedPrint("Losses", $"{Losses}");
			PaddedPrint("Profitability", $"{r.Profitability:.00}");
			PaddedPrint("AccountTotal@Start", $"${Balance.Starting}");
			PaddedPrint("AccountTotal@End", $"${Balance.Total:.00}");
			Console.WriteLine("--------------------------------");
		}

		public ReplayResults Results()
		{
			decimal profitratio;
			decimal? profitability = null;

			if (Losses != 0 && Wins != 0)
			{
				profitratio = (TotalProfit / Wins) / (TotalRisk / Losses);
				profitability = (Wins / (decimal)Losses) / (1 / profitratio);
			}

			return new ReplayResults()
			{
				Profitability = profitability,
				Symbol = Symbol.Symbol,
				Trades = Wins + Losses,
				EndTotal = Balance.Total,
				StartTotal = Balance.Starting,
				TimeInLosingTrades = TimeInLosing,
				TimeInWinningTrades = TimeInWinning,
				TimeInTrades = TimeInTrades,
				Wins = Wins,
				Losses = Losses
			};
		}

		void PaddedPrint(string description, string value)
		{
			string padded_description = (description + ":").PadRight(23);

			Console.WriteLine($"{padded_description}{value}");
		}

		public void SetupStrategies()
		{
			var periods = Symbol.Data.Periods;

			try
			{
				//Risk_Setter = new ATRRisk(periods, true);
				Risk_Setter = new SuperTrendRisk(periods);
				Profit_Setter = new ReversedRSIProfit(periods, 14, 78);
				//Risk_Setter = new PercentBasedRisk(periods, (decimal)0.02);
				//Exit_Strategy.Add()
				//Exit_Strategy = new TrailingStopLossExit(periods, Risk_Setter.GetPrice);
				//Exit_Strategy = new ADXExit(periods); // OnPeriodClose
				//Exit_Strategy = new EMAExit(periods, 50);
				//Entry_Strategy.Add(new MACDCrossover(periods)); //*
				//Entry_Strategy.Add(new Star(periods)); //*
				Entry_Strategy.Add(new DICrossover(periods)); //*
				//Entry_Strategy.Add(new ThreelineStrike(periods));
				Entry_Strategy.Add(new GoldenCross(periods, 9, 48));

				Entry_Conditions.Add(new EMACondition(periods, 800));
				Entry_Conditions.Add(new SuperTrendCondition(periods));
				//Entry_Conditions.Add(new CMFCondition(periods));
				//Entry_Conditions.Add(new RSICondition(periods, 80, 40));
				//Entry_Conditions.Add(new ADXCondition(periods, 15));
				//Exit_Strategies.Add(new RSIExit(periods, 77, 30, false));
				//Exit_Strategies.Add(new EMAExit(periods, 800));
				//Exit_Strategies.Add(new ADXExit(periods));
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		void OnReplaySignal(int period, SignalType signal)
		{
			if(BuyInPos == false)
			{
				foreach(var pos in Position.Positions)
				{
					if (pos.Symbol == Symbol.Symbol)
						return;
				}
			}

			// Disallow shorting for now
			if (signal == SignalType.Short)
				return;

			// Check to make sure signal also passes required entry conditions
			foreach (var condition in Entry_Conditions)
			{
				if(!condition.GetAllowed(period).Contains(signal))
				{
					return;
				}
			}

			if (Risk_Setter != null && !Risk_Setter.CanSetPrice(period, signal))
			{
				return;
			}

			List<ExitStrategy> exits = new List<ExitStrategy>()
			{
				new OCOExit(Symbol.Data.Periods, Risk_Setter.GetPrice, Profit_Setter.GetPrice),
				//new TimeLimitExit(Symbol.Data.Periods, new TimeSpan(7, 0, 0, 0), Symbol[period].CloseTime, true),
				//new StoplossExit(Symbol.Data.Periods, Risk_Setter.GetPrice)
			};

			exits.AddRange(Exit_Strategies);

			foreach(var strat in exits)
			{
				strat.Update(period, signal);

				if (strat.ShouldExit(period, signal, out decimal scratch))
					return;
			}

			// Clamp bet amount to not be higher than total balance. And don't continue the signal if we have no money left at all
			if (!GetBetAmount(out decimal bet_amount))
				return;

			// Don't enter trade if we don't have the 'Available' balance to trade with
			if (bet_amount > Balance.Available)
				return;

			/*
			bool use_max_risk = false;
			double max_risk = 0.09;
			bool use_min_risk = false;
			double min_risk = 0.02;
			bool use_max_profit = false;
			double max_profit = 0.2;
			bool use_min_profit = false;
			double min_profit = 0.06;

			if (use_max_risk && GetPctDiff((double)risk_price, (double)entry_price) > max_risk)
				return;

			if (use_min_risk && GetPctDiff((double)risk_price, (double)entry_price) < min_risk)
				return;

			if (use_max_profit && GetPctDiff((double)profit_price, (double)entry_price) > max_profit)
				return;

			if (use_min_profit && GetPctDiff((double)profit_price, (double)entry_price) < min_profit)
				return;

			if (!IsPositionValid(entry_price, risk_price, profit_price))
				throw new Exception($"Invalid position: Entry: {entry_price}, Risk: {risk_price}, Profit: {profit_price}");

			if (!GetBetAmount(out decimal bet_amount))
				return;

			TotalRisk += (decimal)Math.Abs(((double)risk_price - (double)entry_price) / (double)entry_price);
			TotalProfit += (decimal)Math.Abs(((double)profit_price - (double)entry_price) / (double)entry_price);
			*/

			Balance.Available -= bet_amount;
			//Console.WriteLine($"[{Symbol.Symbol}] Available(Entry) -= {bet_amount:.00}");
			decimal entry = Symbol[period].Close;
			new Position(Symbol.Exchange, Symbol.Symbol, Interval, signal, Symbol[period].CloseTime, entry, bet_amount / entry)
			{
				ExitStrategies = exits
			};
		}

		private double GetPctDiff(double one, double two)
		{
			return Math.Abs((one - two) / two);
		}

		private bool IsPositionValid(decimal entry, decimal risk, decimal profit)
		{
			return risk < entry && entry < profit;
		}

		private bool GetBetAmount(out decimal bet_amount)
		{
			bet_amount = BetSizer.BetAmount > Balance.Total ? Balance.Total : BetSizer.BetAmount;

			return bet_amount > 0;
		}

		private bool WonTrade(decimal entry, decimal exit, SignalType signal)
		{
			switch (signal)
			{
				case SignalType.Long:
					if (exit > entry)
						return true;
					else
						return false;
				case SignalType.Short:
					if (exit < entry)
						return true;
					else
						return false;
			}

			throw new ArgumentException("Invalid signal type in", "pos");
		}

		public void ExitCallback(int period, Position pos, decimal exit)
		{
			Debug.Assert(Finished == false);

			if (!Position.Positions.Contains(pos))
				return;

			decimal entry = pos.Entry;
			bool TradeWon = WonTrade(entry, exit, pos.Type);
			Trades++;
			TimeSpan TradeTime = Symbol[period].CloseTime - pos.Date;
			TimeInTrades += TradeTime;

			decimal quantity = pos.DesiredQuantity;
			decimal feepct = (decimal)0.00075; // Correct
			decimal entryfee = quantity * feepct; // $10 / 2 = 5 The quantity I bought * 0.00075
			decimal assetamount = quantity - entryfee;
			decimal exitamount = (assetamount * exit) - ((assetamount * exit) * feepct); //Entry = 2, Profit = 1, Risk = 3, AssetAmount = 5. Enter at $10, Profit at $5, Risk at $15
			
			Balance.Available += exitamount;
			//Console.WriteLine($"[{Symbol.Symbol}] Available(Exit) += {exitamount:.00}");

			if (TradeWon)
			{
				Wins++;
				TotalProfit += Math.Abs(exitamount - (quantity * entry));
				TimeInWinning += TradeTime;
			}
			else
			{
				Losses++;
				TotalRisk += Math.Abs(exitamount - (quantity * entry));
				TimeInLosing += TradeTime;
			}

			switch (pos.Type)
			{
				case SignalType.Long:
					Balance.Total += exitamount - (quantity * entry);
					//Console.WriteLine($"{Symbol.Symbol} Total += {exitamount - (quantity * entry):.0000000}, Entry = {entry:.0000000}, Exit = {exit:.0000000}");
					break;
				case SignalType.Short:
					Balance.Total += (quantity * entry) - exitamount;
					break;
			}


			if (Balance.Total < 0)
				Balance.Total = 0;

			BetSizer.UpdateBetAmount();

			pos.Close();
		}

		private void CheckForExit(int period, Position pos)
		{
			foreach(var strat in pos.ExitStrategies)
			{
				if (strat.ShouldExit(period, pos.Type, out decimal exit_price))
				{
					ExitCallback(period, pos, exit_price);
				}
			}
		}
	}
}
