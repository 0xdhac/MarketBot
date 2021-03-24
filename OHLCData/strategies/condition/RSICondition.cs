using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.condition
{
	class RSICondition : ConditionalAddon
	{
		private RSI RSI;
		private bool Overbought;
		public RSICondition(SymbolData data, bool overbought_only, int length) :
			base(data, $"{{\"indicators\":[{{\"name\":\"RSI\", \"inputs\":[{length}]}}]}}")
		{
			Overbought = overbought_only;
			RSI = (RSI)FindIndicator("RSI", length);
		}

		public override SignalType[] GetAllowedSignals(int period)
		{
			if (period < (int)RSI.Inputs[0] + 10)
				return new SignalType[] { };

			List<SignalType> Allowed = new List<SignalType>();
			if (Overbought == false)
			{
				if (RSI[period].Item4 <= 30)
				{
					Allowed.Add(SignalType.Long);
				}
				else if (RSI[period].Item4 >= 70)
				{
					Allowed.Add(SignalType.Short);
				}
				else
				{
					Allowed.Add(SignalType.Long);
					Allowed.Add(SignalType.Short);
				}
			}
			else
			{
				if (RSI[period].Item4 >= 70)
				{
					Allowed.Add(SignalType.Short);
				}
				else if (RSI[period].Item4 <= 30)
				{
					Allowed.Add(SignalType.Long);
				}
			}


			return Allowed.ToArray();
		}

		public override string GetName()
		{
			return "RSI Condition";
		}
	}
}
