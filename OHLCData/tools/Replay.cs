using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.strategies.signals;
using MarketBot.interfaces;
using MarketBot.strategies.position;

namespace MarketBot
{
	class Replay
	{
		public Strategy Current_Strategy;
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
		private int Slices = 5;
		private int SlicesUsed = 0;

		public Replay(Exchanges exchange, string symbol, OHLCVInterval interval, int periods, DateTime? start)
		{
			Console.WriteLine($"Starting replay/collecting symbol data for {symbol} on exchange {exchange}.");
			if (exchange == Exchanges.Localhost)
			{
				new SymbolData(symbol, CSVConversionMethod.Standard, OnSymbolLoaded);
			}
			else
			{
				new SymbolData(exchange, interval, symbol, periods, OnSymbolLoaded, start);
			}
			
			//Current_Strategy = new Pair(Symbol, OnStrategyReady, "BTCUSDT", "MACDCrossover");
			
			//testind = (indicators.CMF)Symbol.RequireIndicator("CMF", new KeyValuePair<string, object>("Length", 20));
			
			
		}

		void OnSymbolLoaded(SymbolData data)
		{
			Symbol = data;
			Exit_Strategy = new Swing(data, 1);
			Current_Strategy = new CMFCrossover(Symbol, 200, 20, 20);
			//Current_Strategy = new CMFPassZero(Symbol, 200, 20, 20);
			//new Pair(data, OnStrategyReady, "BTCUSDT", "CMFPassZero", 1440, 20, 50);

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
			Console.WriteLine($"Running strategy on {Symbol.Symbol}");
			bool buy_in_pos = bool.Parse(Environment.GetEnvironmentVariable("BUY_WHEN_IN_POSITION"));
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
			Console.WriteLine($"Period Interval: {Symbol.Interval}");
			Console.WriteLine($"Number Of Periods: {Symbol.Data.Data.Count}");
			Console.WriteLine($"Entry Strategy: {Current_Strategy.GetName()}");
			Console.WriteLine($"Exit Strategy: {Exit_Strategy.GetName()}");
			Console.WriteLine($"Risk/Reward Ratio: 1:{(float)RiskProfitRatio}");
			Console.WriteLine($"Average Number of Periods: {(float)TradePeriodsTotal / (float)Trades}");
			Console.WriteLine($"Wins: {Wins}");
			Console.WriteLine($"Losses: {Losses}");
			Console.WriteLine($"Profitability: {profitability}");
			Console.WriteLine($"AccountTotal@Start: {AccountTotal_Start}");
			Console.WriteLine($"AccountTotal@End: {AccountTotal}");
			Console.WriteLine("--------------------------------");
		}

		void OnReplaySignal(SymbolData data, int period, SignalType signal)
		{
			decimal entry_price = data.Data[period].Close;
			decimal risk_price = Exit_Strategy.GetRiskPrice(period, signal);
			decimal profit_price = ((entry_price - risk_price) * RiskProfitRatio) + entry_price;

			if (SlicesUsed == 0)
			{
				BetAmount = AccountTotal / (decimal)Slices;
				SlicesUsed = (SlicesUsed + 1) % Slices;
			}

			//Console.WriteLine($"{Symbol.Data[period].OpenTime}");

			new Position(data, period, signal, entry_price, risk_price, profit_price);
		}

		public void ExitCallback(SymbolData data, Position pos, int period, bool TradeWon)
		{
			if (TradeWon)
				Wins++;
			else
				Losses++;

			Trades++;
			TradePeriodsTotal += (period - pos.Period);


			switch (pos.Type)
			{
				case SignalType.Long:
					switch (TradeWon)
					{
						case true:
							AccountTotal -= BetAmount;
							AccountTotal += ((pos.Profit * BetAmount) / pos.Entry);
							break;
						case false:
							AccountTotal -= BetAmount;
							AccountTotal += ((pos.Risk * BetAmount) / pos.Entry);
							break;
					}
					break;
				case SignalType.Short:
					switch (TradeWon)
					{
						case true:
							AccountTotal += BetAmount;
							AccountTotal -= ((pos.Profit * BetAmount) / pos.Entry);
							break;
						case false:
							AccountTotal += BetAmount;
							AccountTotal -= ((pos.Risk * BetAmount) / pos.Entry);
							break;
					}
					break;
			}

			pos.Close();
		}

		private void CheckForExit(SymbolData data, int period, Position pos)
		{
			//Console.WriteLine($"{data.Data[pos.Period].CloseTime} {data.Data[pos.Period].Close} {pos.Profit} {pos.Risk}");
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
