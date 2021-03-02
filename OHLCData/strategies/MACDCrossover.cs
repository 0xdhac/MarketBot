using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;

namespace MarketBot
{
	class MACDCrossover : IStrategy
	{
		public bool Enter(List<OHLCVPeriod> data, int current_period)
		{
			return false;
		}

		public bool Exit(List<OHLCVPeriod> data, int current_period)
		{
			return false;
		}

		public static void Strategy()
		{

		}
	}
}
