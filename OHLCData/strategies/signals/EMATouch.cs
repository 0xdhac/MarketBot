using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.condition
{
	class EMATouch : EntrySignaler
	{
		private EMA EMA;
		public EMATouch(SymbolData data, int length) :
			base(data, $"{{\"indicators\":[{{\"name\":\"EMA\", \"inputs\":[{length}]}}]}}")
		{
			EMA = (EMA)FindIndicator("EMA", length);
			
		}

		



		public override string GetName()
		{
			return "EMA Touch";
		}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			if (Source[new_period].Open < EMA[new_period] &&
				Source[new_period].Close > EMA[new_period])
				return SignalType.Long;

			if (Source[new_period].Open > EMA[new_period] &&
				Source[new_period].Close < EMA[new_period])
				return SignalType.Short;

			return SignalType.None;
		}
	}
}
