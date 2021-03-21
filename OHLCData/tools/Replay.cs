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
		public Strategy Current_Strategy;
		public RiskStrategy Exit_Strategy;
		public SymbolData Symbol;

		private int Wins = 0;
		private int Losses = 0;
		private decimal RiskProfitRatio = (decimal)4;
		private int Trades = 0;
		private int TradePeriodsTotal = 0;
		private decimal AccountTotal = 10000;
		private decimal AccountTotal_Start = 0;
		private decimal BetAmount = 0;
		private decimal BetPct = (decimal)1;
		private int ResizeEvery = 1;
		private BetResizeMode ResizeMode = BetResizeMode.Counter;
		private int SlicesUsed = 0;
		private decimal TotalRisk = 0;

		public Replay(Exchanges exchange, string symbol, OHLCVInterval interval, int periods, DateTime? start)
		{
			Console.WriteLine($"Starting replay/collecting symbol data for {symbol} on exchange {exchange}.");
			if (exchange == Exchanges.Localhost)
			{
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
			Symbol = data;
			Exit_Strategy = new ATR(data);
			//Exit_Strategy = new Swing(data, 1);
			Current_Strategy = new MACDCrossover(Symbol, 200, 12, 26, 9);
			//Current_Strategy = new CMFCrossover(Symbol, 23, 55, 21);
			//Current_Strategy = new Candlesticks(Symbol, 20, 55);

			OnStrategyReady(Current_Strategy);
		}

		public void OnStrategyReady(Strategy strategy)
		{
			Current_Strategy = strategy;
			Run();
		}

		public void Run()
		{
			AccountTotal_Start = AccountTotal;
			BetAmount = AccountTotal * BetPct;
			Console.WriteLine($"Running strategy on {Symbol.Symbol}");
			bool buy_in_pos = bool.Parse(Program.GetConfigSetting("BUY_WHEN_IN_POSITION"));
			for (int period = 0; period < Symbol.Data.Data.Count; period++)
			{
				List <Position> position_list = Position.FindPositions(Symbol); //<-- this function might need to have multiple definitions. One that takes in SymbolData object, and one that takes in symbol info, like Exchange and Symbol name

				if (position_list.Count > 0)
				{
					foreach (var position in position_list)
					{
						CheckForExit(Symbol, period, position);
					}

					if (buy_in_pos == true)
					{
						Current_Strategy.Run(period, OnReplaySignal);
					}
				}
				else
				{
					Current_Strategy.Run(period, OnReplaySignal);
				}
			}

			float profitability = (Losses == 0) ? -1 : ((float)Wins / (float)Losses) / ((float)1 / (float)RiskProfitRatio);
			int trades = (Wins + Losses == 0) ? 1 : Wins + Losses;
			Console.WriteLine($"Period Interval: {Symbol.Interval}");
			Console.WriteLine($"Number Of Periods: {Symbol.Data.Data.Count}");
			Console.WriteLine($"Entry Strategy: {Current_Strategy.GetName()}");
			Console.WriteLine($"Exit Strategy: {Exit_Strategy.GetName()}");
			Console.WriteLine($"Risk/Reward Ratio: 1:{(float)RiskProfitRatio}");
			Console.WriteLine($"Average Number of Periods: {(float)TradePeriodsTotal / (float)Trades}");
			Console.WriteLine($"Average risk size: {TotalRisk / (trades)}");
			Console.WriteLine($"Wins: {Wins}");
			Console.WriteLine($"Losses: {Losses}");
			Console.WriteLine($"Profitability: {profitability}");
			Console.WriteLine($"AccountTotal@Start: {AccountTotal_Start}");
			Console.WriteLine($"AccountTotal@End: {AccountTotal}");
			Console.WriteLine("--------------------------------");
		}

		void OnReplaySignal(SymbolData data, int period, SignalType signal)
		{
			if (signal == SignalType.Short)
				return;

			Console.WriteLine(data.Data[period].OpenTime);

			decimal entry_price = data.Data[period].Close;
			decimal risk_price = Exit_Strategy.GetRiskPrice(period, signal);
			decimal profit = RiskProfitRatio;
			
			if (Math.Abs(((double)risk_price - (double)entry_price) / (double)entry_price) < 0.001)
			{
				//return;
			}

			TotalRisk += (decimal)Math.Abs(((double)risk_price - (double)entry_price) / (double)entry_price);


			decimal profit_price = ((entry_price - risk_price) * profit) + entry_price;

			if (entry_price - risk_price == 0)
				return;
			
			decimal bet_amount = BetAmount > AccountTotal ? AccountTotal : BetAmount;

			if (bet_amount <= 0)
			{
				return;
			}

			new Position(data, period, signal, entry_price, risk_price, profit_price, bet_amount /entry_price, false);
		}

		public void ExitCallback(SymbolData data, Position pos, int period, bool TradeWon)
		{
			if (TradeWon)
				Wins++;
			else
				Losses++;

			Trades++;
			TradePeriodsTotal += (period - pos.Period);

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
