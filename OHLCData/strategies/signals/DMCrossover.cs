using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.indicators;
using MarketBot.interfaces;

namespace MarketBot.strategies.signals
{
	public class DMCrossover : EntrySignaler
	{
		//Testing
		private ADX ADX;

		public DMCrossover(SymbolData data, int length) :
			base(data, new IIndicator[] { new ADX(data, length) })
		{
			ADX = (ADX)FindIndicator("ADX", length);
		}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			return SignalType.None;
		}

		public override string GetName()
		{
			return "Directional Movement Crossover";
		}
	}
}
