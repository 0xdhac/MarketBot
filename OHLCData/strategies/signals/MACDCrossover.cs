using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.signals
{
	public class MACDCrossover : Strategy
	{
		private EMA TrendLine;
		private MACD MACD;
		private ATR ATR;

		public MACDCrossover(SymbolData data, StrategyReadyCallback callback) : base(data, callback) 
		{
			callback(this);
		}

		public override void ApplyIndicators()
		{
			TrendLine = (EMA)DataSource.RequireIndicator("EMA",
				new KeyValuePair<string, object>("Length", 200));

			MACD = (MACD)DataSource.RequireIndicator("MACD",
				new KeyValuePair<string, object>("Short_EMA_Length", 12),
				new KeyValuePair<string, object>("Long_EMA_Length", 26),
				new KeyValuePair<string, object>("Signal_EMA_Length", 9));

			ATR = (ATR)DataSource.RequireIndicator("ATR", new KeyValuePair<string, object>("Length", 14));
		}

		public override SignalType StrategyConditions(int new_period, int old_period)
		{
			if (new_period < 300)
				return SignalType.None;
			/*
			 * LONG CONDITION
			 * - CURRENT MACD MUST BE ABOVE SIGNAL LINE
			 * - OLD MACD MUST BE BELOW SIGNAL LINE
			 */
			if (DataSource.Data[new_period].Low > TrendLine[new_period].Item2 &&
				MACD[new_period].Item2 - MACD[new_period].Item4 < 0 &&
				MACD[old_period].Item2 - MACD[old_period].Item4 > 0 &&
				MACD[new_period].Item2 < -(ATR[new_period].Item2 / 2)) // CHANGE THIS TOLERANCE VALUE TO BE RELATIVE TO THE 
			{
				return SignalType.Long;
			}

			/*
			 * SHORT CONDITION
			 * - CANDLE MUST NOT BE HIGHER THAN TRENDLINE AT ANY POINT
			 * - CURRENT MACD MUST BE BELOW SIGNAL LINE
			 * - OLD MACD MUST BE ABOVE SIGNAL LINE
			 * - MACD MUST BE ABOVE ZERO LINE
			 */
			if (DataSource.Data[new_period].High < TrendLine[new_period].Item2 &&
				MACD[new_period].Item2 - MACD[new_period].Item4 > 0 &&
				MACD[old_period].Item2 - MACD[old_period].Item4 < 0 &&
				MACD[new_period].Item2 > ATR[new_period].Item2 / 2)
			{
				//Console.WriteLine($"High: {DataSource.Data[new_period].High}, Trend: {TrendLine[new_period].Item2}, MACD: {MACD[new_period].Item2}, Signal: {MACD[new_period].Item3}");
				return SignalType.Short;
			}

			return SignalType.None;
		}

		public override string GetName()
		{
			return "MACD Crossover";
		}
	}
}
