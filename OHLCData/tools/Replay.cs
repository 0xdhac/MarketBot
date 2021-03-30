using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.strategies.signals;
using MarketBot.interfaces;
using MarketBot.strategies.position;
using System.IO;
using MarketBot.tools;
using MarketBot.strategies.condition;
using MarketBot.indicators;
using Skender.Stock.Indicators;

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
		public List<EntrySignaler> Current_Strategy = new List<EntrySignaler>();
		public RiskStrategy Exit_Strategy;
		public SymbolData Symbol;

		private int Wins = 0;
		private int Losses = 0;
		private decimal RiskProfitRatio = (decimal)3;
		private int Trades = 0;
		private int TradePeriodsTotal = 0;
		private decimal AccountTotal = 10000;
		private decimal AccountTotal_Start = 0;
		private decimal BetAmount = 0;
		private decimal BetPct = (decimal)0.05;
		private int ResizeEvery = 1;
		private BetResizeMode ResizeMode = BetResizeMode.Counter;
		private int SlicesUsed = 0;
		private decimal TotalRisk = 0;
		private TimeSpan TimeInTrades = new TimeSpan();
		private OHLCVInterval Interval;
		private Action<ReplayResults> Callback = null;
		IEnumerable<PivotPointsResult> Pivots;

		public Replay(Exchanges exchange, string symbol, OHLCVInterval interval, int periods, DateTime? start, Action<ReplayResults> callback = null)
		{
			Interval = interval;
			//Console.WriteLine($"Starting replay/collecting symbol data for {symbol} on exchange {exchange}.");
			if (exchange == Exchanges.Localhost)
			{
				Callback = callback;
				var pattern = symbol + "-" + BinanceAnalyzer.GetKlineInterval(interval) + "*";

				var files = Directory.GetFiles("./klines/", pattern);

				if(files.Length == 0)
				{
					Console.WriteLine($"No files for symbol {symbol} found on interval {interval}.");
					return;
				}
				files.OrderBy((v1) => v1);

				new SymbolData(symbol, files, interval, CSVConversionMethod.BinanceVision, OnSymbolLoaded);
			}
			else
			{
				new SymbolData(exchange, interval, symbol, periods, OnSymbolLoaded, false, start);
			}
		}

		void OnSymbolLoaded(SymbolData data)
		{
			ExchangeTasks.UpdateExchangeInfo(Exchanges.Binance);
			Symbol = data;
			Exit_Strategy = new ATRRisk(data);

			//Current_Strategy.Add(new CMFCrossover(Symbol, 55, 21));
			//Current_Strategy.Add(new MACDCrossover(Symbol, 12, 26, 9));
			Current_Strategy.Add(new Star(Symbol));
			//Current_Strategy.Add(new ThreelineStrike(Symbol));
			//Current_Strategy.Add(new DICrossover(Symbol));
			//Current_Strategy.Add(new EMATouch(Symbol, 300));

			EMACondition cond1 = new EMACondition(data, 800);
			//BetweenEMACondition cond1 = new BetweenEMACondition(data, 200, 50);
			//EMAOverEMACondition cond1 = new EMAOverEMACondition(Symbol, 800, 300);
			ADXCondition cond2 = new ADXCondition(Symbol, 14);
			CMFCondition cond3 = new CMFCondition(Symbol, 21);
			//RSICondition cond3 = new RSICondition(Symbol, false, 14);
			//PivotPointType.
			foreach(var strat in Current_Strategy)
			{
				strat.Add(cond1);
				strat.Add(cond2);
				//strat.Add(cond3);
			}

			Pivots = Skender.Stock.Indicators.Indicator.GetPivotPoints(data.Data.Periods, PeriodSize.Day);

			Run();
		}

		public void Run()
		{
			AccountTotal_Start = AccountTotal;
			BetAmount = AccountTotal * BetPct;
			bool buy_in_pos = bool.Parse(Program.GetConfigSetting("BUY_WHEN_IN_POSITION"));
			for (int period = 0; period < Symbol.Data.Periods.Count; period++)
			{
				List <Position> position_list = Position.FindPositions(Symbol); //<-- this function might need to have multiple definitions. One that takes in SymbolData object, and one that takes in symbol info, like Exchange and Symbol name

				if (position_list.Count > 0)
				{
					foreach (var position in position_list)
					{
						CheckForExit(Symbol, period, position);
					}

					if (true)
					{
						foreach (var strat in Current_Strategy)
						{
							if (position_list.Count == 0 || true)
							{
								strat.Run(period, OnReplaySignal);
							}
						}
					}
				}
				else
				{
					foreach (var strat in Current_Strategy)
					{
						if (position_list.Count == 0 || true)
						{
							strat.Run(period, OnReplaySignal);
						}
					}
				}
			}

			decimal profitability = (Losses == 0) ? -1 : ((decimal)Wins / (decimal)Losses) / ((decimal)1 / (decimal)RiskProfitRatio);
			int trades = (Wins + Losses == 0) ? 1 : Wins + Losses;
			TimeSpan days = new TimeSpan((ExchangeTasks.GetOHLCVIntervalTimeSpan(Interval).Ticks * Symbol.Data.Periods.Count));
			ReplayResults r = new ReplayResults()
			{
				Profitability = profitability,
				Symbol = Symbol.Symbol,
				Trades = Wins + Losses,
				EndTotal = AccountTotal,
			};

			if(Callback != null)
			{
				Callback(r);
			}
			else
			{
				Console.WriteLine("--------------------------------");
				PaddedPrint("Symbol", Symbol.Symbol);
				PaddedPrint("Interval", $"{Symbol.Interval}");
				PaddedPrint("Periods", $"{Symbol.Data.Periods.Count} ({days.TotalDays:.00}d)");
				PaddedPrint("Entry Strategy", Current_Strategy.ToString());
				PaddedPrint("Exit Strategy", Exit_Strategy.ToString());
				PaddedPrint("Ratio", $"1:{(float)RiskProfitRatio}");
				PaddedPrint("Average Trade Periods", $"{((float)TradePeriodsTotal / (float)Trades):.00}");
				PaddedPrint("Time In Trades", $"{TimeInTrades.TotalDays:.00}d");
				PaddedPrint("Average Risk", $"{((TotalRisk / (trades)) * 100):0.0000}%");
				PaddedPrint("Wins", $"{Wins}");
				PaddedPrint("Losses", $"{Losses}");
				PaddedPrint("Profitability", $"{profitability:.00}");
				PaddedPrint("AccountTotal@Start", $"${AccountTotal_Start}");
				PaddedPrint("AccountTotal@End", $"${AccountTotal:.00}");
				Console.WriteLine("--------------------------------");
			}
		}

		void PaddedPrint(string description, string value)
		{
			string padded_description = (description + ":").PadRight(23);

			Console.WriteLine($"{padded_description}{value}");
		}

		void OnReplaySignal(SymbolData data, int period, SignalType signal)
		{
			if (signal == SignalType.Short)
				return;

			//signal = SignalType.Short;

			//Console.WriteLine(data.Data[period].OpenTime);

			decimal entry_price = data.Data[period].Close;
			decimal risk_price = Exit_Strategy.GetRiskPrice(period, signal);
			decimal profit = RiskProfitRatio;

			var pp = Pivots.ToList();
			decimal support = pp[period].S1.Value;
			decimal resistance = pp[period].R2.Value;
			ExchangeTasks.GetTickSize(Exchanges.Binance, data.Symbol, out decimal tick_size);
			decimal tick_scale = 10;
			if (risk_price > support)
			{
				risk_price = support - (tick_size * tick_scale);
			}

			TotalRisk += (decimal)Math.Abs(((double)risk_price - (double)entry_price) / (double)entry_price);

			decimal profit_price = ((entry_price - risk_price) * profit) + entry_price;

			if(profit_price <= resistance)
			{
				profit_price = resistance - (tick_size * tick_scale);
			}

			if(Math.Abs(((double)profit_price - (double)entry_price) / (double)entry_price) > 0.2)
			{
				return;
			}

			if (entry_price - risk_price == 0)
				return;
			
			decimal bet_amount = BetAmount > AccountTotal ? AccountTotal : BetAmount;

			if (bet_amount <= 0)
			{
				return;
			}

			new Position(data, period, signal, entry_price, risk_price, profit_price, bet_amount / entry_price, false);
		}

		public void ExitCallback(SymbolData data, Position pos, int period, bool TradeWon)
		{
			if (TradeWon)
				Wins++;
			else
				Losses++;

			Trades++;

			int periods = period - pos.Period;
			TradePeriodsTotal += periods;

			TimeInTrades += new TimeSpan(ExchangeTasks.GetOHLCVIntervalTimeSpan(Interval).Ticks * periods);


			//$10 / 2LTC 5 * 0.0075
			decimal Quantity = pos.Quantity;
			decimal FeePct = (decimal)0.00075; // Correct
			decimal EntryFee = Quantity * FeePct; // $10 / 2 = 5 The quantity I bought * 0.00075
			decimal AssetAmount = Quantity - EntryFee;
			decimal ProfitAmount = (AssetAmount * pos.Profit) - ((AssetAmount * pos.Profit) * FeePct); //Entry = 2, Profit = 1, Risk = 3, AssetAmount = 5. Enter at $10, Profit at $5, Risk at $15
			decimal RiskAmount = (AssetAmount * pos.Risk) - ((AssetAmount * pos.Risk) * FeePct); // Entry = 2, Profit = 3, Risk = 1, AssetAmount = 5. Enter at $10, Profit at $15

			switch (pos.Type)
			{
				case SignalType.Long:
					switch (TradeWon)
					{
						case true:
							AccountTotal += ProfitAmount - (pos.Quantity * pos.Entry);
							break;
						case false:
							AccountTotal += RiskAmount - (pos.Quantity * pos.Entry);
							break;
					}
					break;
				case SignalType.Short:
					switch (TradeWon)
					{
						case true:
							AccountTotal += (pos.Quantity * pos.Entry) - ProfitAmount;
							break;
						case false:
							AccountTotal += (pos.Quantity * pos.Entry) - RiskAmount;
							break;
					}
					break;
			}


			if (AccountTotal < 0)
				AccountTotal = 0;

			switch (ResizeMode)
			{
				case BetResizeMode.Counter:
					if (SlicesUsed++ % ResizeEvery == 0)
						BetAmount = AccountTotal * BetPct;
					break;
				case BetResizeMode.Win:
					if(TradeWon)
						BetAmount = AccountTotal * BetPct;
					break;
				case BetResizeMode.Loss:
					if (!TradeWon)
						BetAmount = AccountTotal * BetPct;
					break;
				default:
					break;
			}

			pos.Close();
		}

		private void CheckForExit(SymbolData data, int period, Position pos)
		{
			switch (pos.Type)
			{
				case SignalType.Long:
					if(data.Data[period].High >= pos.Profit) // Profit hit
					{
						if(data.Data[period].Low <= pos.Risk) // Make sure candle didn't touch risk as well
						{
							pos.Close();
						}
						else // Trade won
						{
							ExitCallback(data, pos, period, true);
						}
					}
					else if(data.Data[period].Low <= pos.Risk) // Risk hit
					{
						if (data.Data[period].High >= pos.Profit) // Make sure candle didn't touch profit as well
						{
							pos.Close();
						}
						else // Trade lost
						{
							ExitCallback(data, pos, period, false);
						}
					}
					break;
				case SignalType.Short:
					if (data.Data[period].Low <= pos.Profit) // Profit hit
					{
						if (data.Data[period].High >= pos.Risk) // Make sure candle didn't touch risk as well
						{
							pos.Close();
						}
						else // Trade won
						{
							ExitCallback(data, pos, period, true);
						}
					}
					else if (data.Data[period].High >= pos.Risk) // Risk hit
					{
						if (data.Data[period].Low <= pos.Profit) // Make sure candle didn't touch profit as well
						{
							pos.Close();
						}
						else // Trade lost
						{
							ExitCallback(data, pos, period, false);
						}
					}
					break;
			}
		}
	}
}
