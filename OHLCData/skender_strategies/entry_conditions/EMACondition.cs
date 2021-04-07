using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.entry_conditions
{
	class EMACondition : BaseCondition
	{
		public List<EmaResult> Data;
		private int Lookback;
		public EMACondition(HList<OHLCVPeriod> history, int lookback) : base(history)
		{
			Lookback = lookback;
		}

		public override SignalType[] GetAllowed(int period)
		{
			ShouldUpdate();

			if (!Data[period].Ema.HasValue || period < (Lookback + 50))
				return new SignalType[] { };

			if (Data[period].Ema.Value < History[period].Low)
				return new SignalType[] { SignalType.Long };
			else if (Data[period].Ema.Value > History[period].High)
				return new SignalType[] { SignalType.Short };

			return new SignalType[] { };
		}

		public override void UpdateData()
		{
			Data = Indicator.GetEma(History, Lookback).ToList();
		}
	}
}
