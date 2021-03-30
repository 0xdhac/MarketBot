using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.signals
{
	public class DICrossover : EntrySignaler
	{
		private ADX ADX;
		public DICrossover(SymbolData data) : 
			base(data, $"{{\"indicators\":[{{\"name\":\"ADX\", \"inputs\":[14]}}]}}")
		{
			ADX = (ADX)FindIndicator("ADX", 14);
		}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			if (new_period < 28)
				return SignalType.None;

			decimal new_di_plus = ADX.Value<decimal>("diplus", new_period);
			decimal new_di_minus = ADX.Value<decimal>("diminus", new_period);
			decimal old_di_plus = ADX.Value<decimal>("diplus", old_period);
			decimal old_di_minus = ADX.Value<decimal>("diminus", old_period);
			if (new_di_plus > new_di_minus &&
				old_di_plus < old_di_minus)
				return SignalType.Long;

			if (new_di_plus < new_di_minus &&
				old_di_plus > old_di_minus)
				return SignalType.Short;

			return SignalType.None;
		}

		public override string GetName()
		{
			return "Directional Index Crossover";
		}
	}
}
