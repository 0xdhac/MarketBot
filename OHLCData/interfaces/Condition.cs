using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.interfaces
{
	public abstract class Condition
	{
		public SymbolData Source = null;
		public List<KeyValuePair<string, object>> Inputs = new List<KeyValuePair<string, object>>();
		public List<IIndicator> Indicators = new List<IIndicator>();

		public Condition(SymbolData data, IndicatorList list)
		{
			Source = data;

			foreach(var indicator in list)
			{
				Indicators.Add(Source.RequireIndicator(indicator.Key, indicator.Value.ToArray()));
			}
		}

		public IIndicator FindIndicator(string name, params object[] inputs)
		{
			foreach (var indicator in Indicators)
			{
				if (indicator.GetType().Name == name)
				{
					bool found = true;
					for (int i = 0; i < inputs.Length; i++)
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

					if (found == true)
					{
						return indicator;
					}
				}
			}

			return null;
		}

		public abstract SignalType[] GetAllowedSignals();
	}
}
