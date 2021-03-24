using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	class BollingerBands : Indicator<Tuple<bool, decimal, decimal>>
	{
		public BollingerBands(SymbolData data) : base(data) { }

		public override void Calculate(int period)
		{

		}

		public override string GetName()
		{
			return "Bollinger Bands";
		}
	}
}
