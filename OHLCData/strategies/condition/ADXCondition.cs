using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.indicators;
using MarketBot.interfaces;

namespace MarketBot.strategies.condition
{
	class ADXCondition : ConditionalAddon
	{
		private ADX ADX;
		public ADXCondition(SymbolData data, int length) :
			base(data, new ADX(data, length))
		{
			ADX = (ADX)FindIndicator("ADX", length);
		}

		public override SignalType[] GetAllowedSignals(int period)
		{
			List<SignalType> signals = new List<SignalType>();
			if (period >= (int)ADX.Inputs[0] + 150)
			{
				if (ADX.Value<decimal>("value", period) > 25 && ADX.Value<decimal>("diplus", period) > ADX.Value<decimal>("diminus", period))
				{
					decimal diplus = ADX.Value<decimal>("diplus", period);
					decimal diminus = ADX.Value<decimal>("diminus", period);

					if(diplus > diminus)
					{
						signals.Add(SignalType.Long);
					}
					else if(diminus > diplus)
					{
						signals.Add(SignalType.Short);
					}
				}
			}

			return signals.ToArray();
		}

		public override string GetName()
		{
			return "ADX Condition";
		}
	}
}
