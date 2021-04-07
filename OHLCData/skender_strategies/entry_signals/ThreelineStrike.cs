using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.entry_signals
{
	class ThreelineStrike : BaseStrategy
	{
		public ThreelineStrike(HList<OHLCVPeriod> history) : base(history)
		{

		}

		public override SignalType Run(int period)
		{
			if (period < 3)
				return SignalType.None;
			// 3 red candles followed by a green candle larger candle that closes above the oldest red candle

			decimal candle1 = History[period - 3].Close - History[period - 3].Open; // Negative means red
			decimal candle2 = History[period - 2].Close - History[period - 2].Open;
			decimal candle3 = History[period - 1].Close - History[period - 1].Open;

			if (candle1 < 0 && candle2 < 0 && candle3 < 0 &&
				History[period].Close > History[period - 3].Open)
				return SignalType.Long;

			if (candle1 > 0 && candle2 > 0 && candle3 > 0 &&
				History[period].Close < History[period - 3].Open)
				return SignalType.Short;

			return SignalType.None;
		}

		public override void UpdateData()
		{
			
		}
	}
}
