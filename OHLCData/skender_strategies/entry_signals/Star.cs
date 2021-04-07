using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace MarketBot.skender_strategies.entry_signals
{
	class Star : BaseStrategy
	{
		public Star(HList<OHLCVPeriod> history) : base(history)
		{

		}

		public override SignalType Run(int period)
		{
			if (period < 2)
				return SignalType.None; ;

			decimal oldest_diff = History[period].Close - History[period - 2].Open;
			decimal middle_diff = History[period - 1].Close - History[period - 1].Open;
			decimal new_diff = History[period].Close - History[period].Open;

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

		public override void UpdateData()
		{
			
		}
	}
}
