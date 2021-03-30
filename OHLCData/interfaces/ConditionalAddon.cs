using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.interfaces
{
	public abstract class ConditionalAddon : BaseStrategy
	{
		public ConditionalAddon(SymbolData data, IndicatorList list = null) : base(data, list)
		{
			
		}

		public ConditionalAddon(SymbolData data, Indicator[] list = null) : base(data, list)
		{

		}

		public ConditionalAddon(SymbolData data, Indicator indicator = null) : base(data, indicator)
		{

		}

		public abstract SignalType[] GetAllowedSignals(int period);

		public bool Allows(int period, SignalType signal)
		{
			return GetAllowedSignals(period).Contains(signal);
		}
	}
}
