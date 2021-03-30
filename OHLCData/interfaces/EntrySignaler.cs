using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;
using MarketBot.interfaces;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Dynamic;

namespace MarketBot
{
	public enum SignalType
	{
		None = 0,
		Long = 1,
		Short = 2
	}

	public delegate void SignalCallback(SymbolData symbol, int period, SignalType signal);
	public delegate void StrategyReadyCallback(EntrySignaler strategy);

	public abstract class EntrySignaler : BaseStrategy
	{
		public List<ConditionalAddon> Conditions = new List<ConditionalAddon>();

		public EntrySignaler(SymbolData data, IndicatorList list = null) : base(data, list)
		{
			
		}

		public EntrySignaler(SymbolData data, Indicator[] list) : base(data, list)
		{

		}

		public EntrySignaler(SymbolData data, Indicator indicator) : base(data, indicator)
		{

		}

		public virtual void Run(int period, SignalCallback callback)
		{
			if (period == 0)
				return;

			SignalType signal = StrategyConditions(period - 1, period);
			if (signal != SignalType.None)
			{
				bool send = true;
				foreach (var condition in Conditions)
				{
					if (condition.Allows(period, signal) == false)
					{
						send = false;
						break;
					}
				}

				if(send == true)
				{
					callback(Source, period, signal);
				}
			}
		}

		public virtual void Add(ConditionalAddon condition)
		{
			Conditions.Add(condition);
		}

		public new dynamic ToExpando()
		{
			var expando = base.ToExpando();

			foreach (var condition in Conditions)
			{
				expando.Add(condition.GetType().Name, condition.ToExpando());
			}

			return expando;
		}

		public abstract SignalType StrategyConditions(int old_period, int new_period);

		public override string ToString()
		{
			return $"{GetName()} + [{string.Join(", ", Conditions)}]";
		}
	}
}
