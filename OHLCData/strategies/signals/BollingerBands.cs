﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.strategies.signals
{
	class BollingerBands : EntrySignaler
	{

		public BollingerBands(SymbolData data) : base(data)
		{
			
		}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			//throw new NotImplementedException();

			return SignalType.None;
		}

		public override string GetName()
		{
			return "Bollinger Bands";
		}
	}
}
