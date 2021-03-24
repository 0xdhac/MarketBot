using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.condition
{
	class EMACondition : ConditionalAddon
	{
		private EMA EMA;
		public EMACondition(SymbolData data, int length) : 
			base(data, $"{{\"indicators\":[{{\"name\":\"EMA\", \"inputs\":[{length}]}}]}}")
		{
			EMA = (EMA)FindIndicator("EMA", length);
		}

		public override SignalType[] GetAllowedSignals(int period)
		{
			List<SignalType> signals = new List<SignalType>();
			if (period >= (int)EMA.Inputs[0] + 50)
			{
				if (Source.Data[period].Close > EMA[period].Item2)
					signals.Add(SignalType.Long);

				if (Source.Data[period].High < EMA[period].Item2)
					signals.Add(SignalType.Short);
			}

			return signals.ToArray();
		}

		public override string GetName()
		{
			return "EMA Condition";
		}
	}
}
