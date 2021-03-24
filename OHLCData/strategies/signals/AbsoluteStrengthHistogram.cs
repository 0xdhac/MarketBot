using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.strategies.signals
{
	class AbsoluteStrengthHistogram : EntrySignaler
	{
		public AbsoluteStrengthHistogram(SymbolData data) : base(data)
		{
			
		}

		public override void Run(int period, SignalCallback callback)
		{

		}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			//throw new NotImplementedException();

			return SignalType.None;
		}

		public override string GetName()
		{
			return "Absolute Strength Histogram";
		}
	}
}
