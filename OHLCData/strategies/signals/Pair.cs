using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.signals
{
	public class Pair : Strategy
	{
		string StrategyName;
		SymbolData PairedSymbol;
		Strategy Entry_Strategy;
		public StrategyReadyCallback Callback;
		object[] ParamList;

		public Pair(SymbolData main_symbol, StrategyReadyCallback callback, string paired, string strategy_classname, params object[] param_list) : base(main_symbol) 
		{
			PairedSymbol = new SymbolData(main_symbol.Exchange, main_symbol.Interval, paired, main_symbol.Periods, DataLoaded, false, main_symbol.StartTime);
			StrategyName = strategy_classname;
			ParamList = new object[param_list.Length + 1];
			ParamList[0] = PairedSymbol;
			Callback = callback;
			for (int i = 1; i < ParamList.Length; i++)
			{
				ParamList[i] = param_list[i - 1];
			}
			
		}

		public override void ApplyIndicators()
		{
			throw new NotImplementedException();
		}

		private void DataLoaded(SymbolData data)
		{
			object strat = Activator.CreateInstance(Type.GetType("MarketBot.strategies.signals." + StrategyName), ParamList);
			OnPairedStrategyReady((Strategy)strat);
		}

		private void OnPairedStrategyReady(Strategy strategy)
		{
			Entry_Strategy = strategy;
			Callback(this);
		}

		public override SignalType StrategyConditions(int new_period, int old_period)
		{
			return Entry_Strategy.StrategyConditions(new_period, old_period);
		}

		public override string GetName()
		{
			return $"Pair ({PairedSymbol.Symbol}/{StrategyName})";
		}
	}
}
