using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.signals
{
	public class Star : EntrySignaler
	{
		public Star(SymbolData data) : base(data){}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			if (new_period < 2)
				return SignalType.None;

			HList<OHLCVPeriod> kline = Source.Data.Periods;
			
			decimal oldest_diff = kline[new_period - 2].Close - kline[new_period - 2].Open;
			decimal middle_diff = kline[new_period - 1].Close - kline[new_period - 1].Open;
			decimal new_diff = kline[new_period].Close - kline[new_period].Open;

			if (oldest_diff < 0 &&
				Math.Abs(middle_diff / oldest_diff) < (decimal)0.2 &&
				new_diff > 0 &&
				Math.Abs(middle_diff / new_diff) < (decimal)0.2)
			{
				return SignalType.Long;
			}

			if (oldest_diff > 0 &&
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
			return "Candles (Morning/Evening Star)";
		}
	}
}
