using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.condition
{
	class CMFCondition : ConditionalAddon
	{
		private CMF CMF;
		public CMFCondition(SymbolData data, int length) :
			base(data, $"{{\"indicators\":[{{\"name\":\"CMF\", \"inputs\":[{length}]}}]}}")
		{
			CMF = (CMF)FindIndicator("CMF", length);
		}

		public override SignalType[] GetAllowedSignals(int period)
		{
			List<SignalType> signals = new List<SignalType>();
			if (period >= (int)CMF.Inputs[0] + 50)
			{
				if (CMF[period] > 0)
					signals.Add(SignalType.Long);

				if (CMF[period] < 0)
					signals.Add(SignalType.Short);
			}

			return signals.ToArray();
		}

		public override string GetName()
		{
			return "CMF Condition";
		}
	}
}
