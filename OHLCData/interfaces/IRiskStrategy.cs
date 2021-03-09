using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.interfaces
{
	public delegate void RiskCallback(SymbolData data);
	public interface IRiskStrategy
	{
		//decimal Run(Position pos, )
		void ApplyIndicators();
		string GetName();
	}

	public abstract class RiskStrategy : IRiskStrategy
	{
		public SymbolData Pair;

		public RiskStrategy(SymbolData data)
		{
			Pair = data;

			ApplyIndicators();
		}

		public abstract void ApplyIndicators();
		//public abstract void Start(int period);
		public abstract decimal GetRiskPrice(int period, SignalType signal);

		public abstract string GetName();
	}
}
