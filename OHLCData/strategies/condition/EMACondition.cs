using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.strategies.condition
{
	class EMACondition : Condition
	{
		public EMACondition(SymbolData data, int length) : base(data, $"{{0:{{name:\"EMA\",params:\"{length}\"}}}}")
		{
			
		}

		public override SignalType[] GetAllowedSignals()
		{
			return new SignalType[] { SignalType.Long };
		}
	}
}
