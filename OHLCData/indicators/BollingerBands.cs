using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	class BollingerBands : Indicator
	{
		public BollingerBands(HList<OHLCVPeriod> data) : base(data) { }

		public override void BuildDataTable()
		{
			throw new NotImplementedException();
		}

		public override DataRow Calculate(int period)
		{
			throw new NotImplementedException();
		}

		public override string GetName()
		{
			return "Bollinger Bands";
		}
	}
}
