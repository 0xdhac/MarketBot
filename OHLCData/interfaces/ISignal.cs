using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;

namespace MarketBot
{
	public class Settings
	{

	}

	public enum SignalType
	{
		Long = 0,
		Short = 1
	}
	public delegate void SignalCallback(SymbolData symbol, SignalType signal);

	public interface ISignalStrategy
	{
		void Run(SymbolData data, SignalCallback callback);
		void ApplyIndicators(SymbolData data);
	}
}
