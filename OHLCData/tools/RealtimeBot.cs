using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.strategies.signals;

namespace MarketBot.tools
{
	class RealtimeBot
	{
		public static List<SymbolData> Symbols;
		public static IEntryStrategy Strategy;

		public static void Start()
		{
			/*
			 * Exchanges.Screen(Exchanges.Binance);
			 */
		}

		public static void AddSymbol(Exchanges exchange, OHLCVInterval interval, string symbol)
		{
			SymbolData sym = new SymbolData(exchange, interval, symbol, 10000, SymbolLoadedCallback);
			//Strategy = new MACDCrossover(sym);
			//Strategy.ApplyIndicators();
		}

		public static void SymbolLoadedCallback(SymbolData symbol)
		{
			
		}

		public void OnClose(SymbolData data)
		{

		}

		public static void OnEntrySignal(SymbolData symbol, int period, SignalType signal)
		{
			// enter trade and create exit positions
		}
	}
}
