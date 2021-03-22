using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.signals
{
	public class Star : Strategy
	{
		private int ShortEmaLength;
		private int LongEmaLength;
		private EMA ShortEMA;
		private EMA LongEMA;

		public Star(SymbolData data, int ema_length, int long_ema_length) : 
			base(data, $"{{\"indicators\":[{{\"name\":\"EMA\", \"inputs\":[{ema_length}]}},{{\"name\":\"EMA\", \"inputs\":[{long_ema_length}]}}]}}")
		{
			ShortEmaLength = ema_length;
			LongEmaLength = long_ema_length;

			ShortEMA = (EMA)FindIndicator("EMA", ShortEmaLength);
			LongEMA = (EMA)FindIndicator("EMA", LongEmaLength);
		}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			if (new_period < 300)
				return SignalType.None;
			// Morning star
			// period - 2 Close < Open
			// period - 1 abs(Close - Open)/period[-2].abs(Close-Open) < 0.2)
			// period Close > Open && abs(Close - Open)/period[-1].abs(Close - Open) >= 5

			
			HList<OHLCVPeriod> kline = Source.Data.Data;
			
			decimal oldest_diff = kline[new_period - 2].Close - kline[new_period - 2].Open;
			decimal middle_diff = kline[new_period - 1].Close - kline[new_period - 1].Open;
			decimal new_diff = kline[new_period].Close - kline[new_period].Open;

			//Console.WriteLine($"{kline[new_period].Low > ShortEMA[new_period].Item2}\n{ShortEMA[new_period].Item2 > LongEMA[new_period].Item2}\n{oldest_diff < 0}\n{((oldest_diff != 0)?Math.Abs(middle_diff / oldest_diff) < (decimal)0.2:false)}\n{new_diff > 0}\n{(new_diff != 0?Math.Abs(middle_diff / new_diff) < (decimal)0.2:false)}\n");

			if (kline[new_period].Low > ShortEMA[new_period].Item2 &&
				oldest_diff < 0 &&
				Math.Abs(middle_diff / oldest_diff) < (decimal)0.2 &&
				new_diff > 0 &&
				Math.Abs(middle_diff / new_diff) < (decimal)0.2)
			{
				return SignalType.Long;
			}

			if (kline[new_period].High < ShortEMA[new_period].Item2 &&
				oldest_diff > 0 &&
				Math.Abs(middle_diff / oldest_diff) < (decimal)0.2 &&
				new_diff < 0 &&
				Math.Abs(middle_diff / new_diff) < (decimal)0.2)
			{
				return SignalType.Short;
			}
			return SignalType.None;

		}

		public override string GetName()
		{
			return "Candlesticks";
		}
	}
}
