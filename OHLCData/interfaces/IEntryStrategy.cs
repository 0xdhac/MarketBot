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
	public delegate void StrategyReadyCallback(Strategy strategy);

	public interface IEntryStrategy
	{
		void Run(int period, SignalCallback callback);
		string GetName();
	}

	public abstract class Strategy : IEntryStrategy
	{
		public List<Condition> Conditions = new List<Condition>();
		public List<IIndicator> Indicators = new List<IIndicator>();
		public SymbolData Source;

		public Strategy(SymbolData data, IndicatorList list = null)
		{
			Source = data;

			if(list != null)
			{
				foreach (var indicator in list)
				{
					Indicators.Add(Source.RequireIndicator(indicator.Key, indicator.Value.ToArray()));
				}
			}
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

		public IIndicator FindIndicator(string name, params object[] inputs)
		{
			foreach(var indicator in Indicators)
			{
				if(indicator.GetType().Name == name)
				{
					bool found = true;
					for(int i = 0; i < inputs.Length; i++)
					{
						if (i >= indicator.Inputs.Count)
						{
							break;
						}

						if (!indicator.Inputs[i].Equals(inputs[i]))
						{
							found = false;
							break;
						}
					}

					if(found == true)
					{
						return indicator;
					}
				}
			}

			return null;
		}

		public dynamic ToExpando()
		{
			Dictionary<string, object> expando = new Dictionary<string, object>();

			Dictionary<string, object[]> indicator_inputs = new Dictionary<string, object[]>();
			foreach(var indicator in Indicators)
			{
				indicator_inputs.Add(indicator.GetType().Name, indicator.Inputs.ToArray());
			}
			expando.Add("indicators", indicator_inputs);

			foreach (var condition in Conditions)
			{
				expando.Add(condition.GetType().Name, condition.ToExpando());
			}

			return expando;
		}

		public abstract SignalType StrategyConditions(int old_period, int new_period);

		public abstract string GetName();
	}
}
