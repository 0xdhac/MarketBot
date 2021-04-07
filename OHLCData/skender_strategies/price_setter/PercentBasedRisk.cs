using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.price_setter
{
	class PercentBasedRisk : PriceSetter
	{
		decimal Percent;

		public PercentBasedRisk(HList<OHLCVPeriod> history, decimal percent) : base(history)
		{
			Percent = percent;
		}

		public override decimal GetPrice(int period, SignalType signal)
		{
			Debug.Assert(signal != SignalType.None);

			switch (signal)
			{
				case SignalType.Long:
					return History[period].Close - (History[period].Close * Percent);
				case SignalType.Short:
					return History[period].Close + (History[period].Close * Percent);
				default:
					return 0;
			}
		}

		public override void UpdateData()
		{
			
		}
	}
}
