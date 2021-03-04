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
		public static ISignalStrategy Strategy = new MACDCrossover();

		public static void Start()
		{
			//AddSymbol(Exchanges.Binance, "BTCUSDT")
		}

		public static void AddSymbol(Exchanges exchange, OHLCVInterval interval, string symbol)
		{
			SymbolData sym = new SymbolData(exchange, interval, symbol, 10000, OnPeriodClose);
			Strategy.ApplyIndicators(sym);
		}

		public static void OnPeriodClose(SymbolData symbol)
		{
			Strategy.Run(symbol, OnEntrySignal);
		}

		public static void OnEntrySignal(SymbolData symbol, SignalType signal)
		{
			// enter trade and create exit positions
		}
	}
}
