using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.entry_signals
{
	class GoldenCross : BaseStrategy
	{
		public List<EmaResult> ShortTermEmaData;
		public List<EmaResult> LongTermEmaData;

		int ShortTermEmaLength;
		int LongTermEmaLength;

		public GoldenCross(HList<OHLCVPeriod> history, int shorttermema, int longtermema) : base(history)
		{
			Debug.Assert(longtermema > shorttermema);
			ShortTermEmaLength = shorttermema;
			LongTermEmaLength = longtermema;
		}

		public override SignalType Run(int period)
		{
			ShouldUpdate();

			if (period == 0 ||
				!LongTermEmaData[period].Ema.HasValue || 
				!ShortTermEmaData[period].Ema.HasValue ||
				!LongTermEmaData[period - 1].Ema.HasValue ||
				!ShortTermEmaData[period - 1].Ema.HasValue)
				return SignalType.None;

			if (ShortTermEmaData[period].Ema > LongTermEmaData[period].Ema &&
				ShortTermEmaData[period - 1].Ema <= LongTermEmaData[period - 1].Ema)
				return SignalType.Long;

			if (ShortTermEmaData[period].Ema < LongTermEmaData[period].Ema &&
				ShortTermEmaData[period - 1].Ema >= LongTermEmaData[period - 1].Ema)
				return SignalType.Short;

			return SignalType.None;
		}

		public override void UpdateData()
		{
			ShortTermEmaData = Indicator.GetEma(History, ShortTermEmaLength).ToList();
			LongTermEmaData = Indicator.GetEma(History, LongTermEmaLength).ToList();
		}
	}
}
