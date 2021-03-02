using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;

namespace MarketBot
{
	class Replay
	{
		// Give strategy the OHLCVCollection data.
		
		// The replay goes through all candles and applies the strategy until it finds an entry point.
		// The exit point is calculated in the replay (not the strategy) because the exit point has nothing to do with the strategy.
		// The strategy calculates all the indicators, and because of that, don't keep creating new strategy instances. Only create them once per replay.
		// The replay feeds the collection data to the strategy

		public IExchangeOHLCVCollection Data;
		public IStrategy Strategy;

		public bool DataIsCollected = false;

		public Replay(Exchange exchange, string symbol, OHLCVInterval interval, int periods)
		{
			Exchanges.CollectOHLCV(exchange, symbol, interval, periods, DataCollected);
		}

		public void SetStrategy(IStrategy strategy)
		{
			Strategy = strategy;
		}

		void DataCollected(IExchangeOHLCVCollection data)
		{
			Data = data;
			DataIsCollected = true;
		}

		void Start()
		{

		}
	}
}
