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
		object[] ParamList;

		public Pair(SymbolData main_symbol, StrategyReadyCallback callback, string paired, string strategy_classname, params object[] param_list) : base(main_symbol, callback) 
		{
			PairedSymbol = new SymbolData(main_symbol.Exchange, main_symbol.Interval, paired, main_symbol.Periods, DataLoaded, main_symbol.StartTime);
			StrategyName = strategy_classname;
			ParamList = new object[param_list.Length + 2];
			ParamList[0] = PairedSymbol;
			ParamList[1] = (StrategyReadyCallback)OnPairedStrategyReady;
			for(int i = 2; i < ParamList.Length; i++)
			{
				ParamList[i] = param_list[i - 2];
			}
		}

		private void DataLoaded(SymbolData data)
		{
			Activator.CreateInstance(Type.GetType("MarketBot.strategies.signals." + StrategyName), ParamList);
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
