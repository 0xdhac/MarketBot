using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace MarketBot.skender_strategies.entry_signals
{
	class BollingerBands : BaseStrategy
	{
		List<BollingerBandsResult> Data;
		public BollingerBands(HList<OHLCVPeriod> history) : base(history)
		{
			
		}

		public override SignalType Run(int period)
		{
			ShouldUpdate();

			if (period == 0)
				return SignalType.Long;

			return SignalType.None;
		}

		public override void UpdateData()
		{
			Data = Indicator.GetBollingerBands(History).ToList();
		}
	}
}
