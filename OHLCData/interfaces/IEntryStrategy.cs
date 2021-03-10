using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;

namespace MarketBot
{
	public enum SignalType
	{
		None = 0,
		Long = 1,
		Short = 2
	}

	public delegate void SignalCallback(SymbolData symbol, int period, SignalType signal);
	public delegate void StrategyReadyCallback(Strategy strategy);

	public interface IEntryStrategy
	{
		void Run(int period, SignalCallback callback);
		string GetName();
	}

	public abstract class Strategy : IEntryStrategy 
	{
		public SymbolData DataSource;
		

		public Strategy(SymbolData data)
		{
			DataSource = data;
		}
		public abstract void ApplyIndicators();

		public virtual void Run(int period, SignalCallback callback)
		{
			if (period == 0)
				return;

			SignalType signal = StrategyConditions(period - 1, period);
			if (signal != SignalType.None)
			{
				callback(DataSource, period, signal);
			}
		}

		public abstract SignalType StrategyConditions(int old_period, int new_period);

		public abstract string GetName();
	}
}
