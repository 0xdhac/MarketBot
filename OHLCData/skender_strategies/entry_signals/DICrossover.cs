using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.entry_signals
{
	class DICrossover : BaseStrategy
	{
		public List<AdxResult> Data;
		public DICrossover(HList<OHLCVPeriod> history) : base(history)
		{
			Data = Indicator.GetAdx(history, 14).ToList();
		}

		public override SignalType Run(int period)
		{
			ShouldUpdate();

			if (period == 0)
				return SignalType.None;

			if (!(Data[period].Mdi.HasValue && Data[period].Pdi.HasValue))
				return SignalType.None;

			if (!(Data[period - 1].Mdi.HasValue && Data[period - 1].Pdi.HasValue))
				return SignalType.None;

			if (Data[period - 1].Mdi > Data[period - 1].Pdi &&
				Data[period].Mdi < Data[period].Pdi)
				return SignalType.Long;

			if (Data[period - 1].Mdi < Data[period - 1].Pdi &&
				Data[period].Mdi > Data[period].Pdi)
				return SignalType.Short;

			return SignalType.None;

		}

		public override void UpdateData()
		{
			Data = Indicator.GetAdx(History).ToList();
		}
	}
}
