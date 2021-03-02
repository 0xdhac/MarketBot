using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;

namespace MarketBot
{
	interface IStrategy
	{
		bool Enter(List<OHLCVPeriod> data, int current_period);
		bool Exit(List<OHLCVPeriod> data, int current_period);
	}
}
