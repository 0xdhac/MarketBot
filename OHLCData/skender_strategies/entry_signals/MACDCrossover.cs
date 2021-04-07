using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.entry_signals
{
	class MACDCrossover : BaseStrategy
	{
		public List<MacdResult> Data = new List<MacdResult>();
		public MACDCrossover(HList<OHLCVPeriod> history) : base(history)
		{
			
		}
		public override SignalType Run(int period)
		{
			ShouldUpdate();

			if (period == 0)
				return SignalType.None;

			if (!(Data[period].Macd.HasValue && Data[period].Signal.HasValue))
				return SignalType.None;

			if (!(Data[period - 1].Macd.HasValue && Data[period - 1].Signal.HasValue))
				return SignalType.None;


			if (Data[period - 1].Macd.Value > Data[period - 1].Signal.Value &&
				Data[period - 1].Macd.Value < 0 &&
				Data[period].Macd.Value < Data[period].Signal.Value &&
				Data[period].Macd.Value < 0)
				return SignalType.Long;

			if (Data[period - 1].Macd.Value > Data[period - 1].Signal.Value &&
				Data[period - 1].Macd.Value > 0 &&
				Data[period].Macd.Value < Data[period].Signal.Value &&
				Data[period].Macd.Value > 0)
				return SignalType.Short;

			return SignalType.None;
		}

		public override void UpdateData()
		{
			Data = Indicator.GetMacd(History).ToList();
		}
	}
}
