using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.interfaces
{
	public abstract class RiskStrategy : BaseStrategy
	{
		public RiskStrategy(SymbolData data, IndicatorList list = null) : base(data, list)
		{

		}

		public abstract decimal GetRiskPrice(int period, SignalType signal);
	}
}
