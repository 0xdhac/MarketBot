using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.signals
{
	public class MACDCrossover : ISignalStrategy
	{
		/*
		 * Required indicators:
		 * - EMA
		 *	- Length: 200
		 * - EMA
		 *	- Length: 12
		 * - EMA
		 *  - Length: 26
		 *  
		 *  Check if indicator is of type 'EMA' and specified member 'Length' has an 'integer' value of '200'
		 */

		private Settings Requirements;

		private EMA TrendLine;
		private EMA MACDShort;
		private EMA MACDLong;

		public MACDCrossover()
		{
			
		}

		public void ApplyIndicators(SymbolData data)
		{
			TrendLine = (EMA)data.RequireIndicator("EMA",
				new KeyValuePair<string, object>("Length", 200));

			MACDShort = (EMA)data.RequireIndicator("EMA",
				new KeyValuePair<string, object>("Length", 12));

			MACDLong = (EMA)data.RequireIndicator("EMA",
				new KeyValuePair<string, object>("Length", 26));
		}

		// Make function that applies this strategie's required indicators to the SymbolData object that requests it
		public void Run(SymbolData data, SignalCallback callback)
		{
			ApplyIndicators(data);
		}
	}
}
