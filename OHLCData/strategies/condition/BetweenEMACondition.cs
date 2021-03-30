using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.condition
{
	class BetweenEMACondition : ConditionalAddon
	{
		private EMA LongEMA;
		private EMA ShortEMA;
		public BetweenEMACondition(SymbolData data, int long_length, int short_length) :
			base(data, $"{{\"indicators\":[{{\"name\":\"EMA\", \"inputs\":[{long_length}]}}, {{\"name\":\"EMA\", \"inputs\":[{short_length}]}}]}}")
		{
			if (short_length >= long_length)
				throw new ArgumentException();

			LongEMA = (EMA)FindIndicator("EMA", long_length);
			ShortEMA = (EMA)FindIndicator("EMA", short_length);
		}

		public override SignalType[] GetAllowedSignals(int period)
		{
			List<SignalType> signals = new List<SignalType>();
			if (period >= (int)LongEMA.Inputs[0] + 50)
			{
				if (Source[period].Low < ShortEMA[period] &&
					Source[period].Low > LongEMA[period])
					signals.Add(SignalType.Long);

				if (Source[period].High > ShortEMA[period] &&
					Source[period].High < LongEMA[period])
					signals.Add(SignalType.Short);
			}

			return signals.ToArray();
		}

		public override string GetName()
		{
			return "Between EMA Condition";
		}
	}
}
