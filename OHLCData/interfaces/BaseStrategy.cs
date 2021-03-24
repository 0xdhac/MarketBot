using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.interfaces
{
	public abstract class BaseStrategy : Expandum
	{
		public List<IIndicator> Indicators = new List<IIndicator>();
		public SymbolData Source;

		public BaseStrategy(SymbolData data, IndicatorList list = null)
		{
			Source = data;

			if (list != null)
			{
				foreach (var indicator in list)
				{
					Indicators.Add(Source.RequireIndicator(indicator.Key, indicator.Value.ToArray()));
				}
			}
		}

		public BaseStrategy(SymbolData data, IIndicator[] list)
		{
			Source = data;

			foreach(var indicator in list)
			{
				Indicators.Add(indicator);
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

		public abstract string GetName();
		public virtual dynamic ToExpando()
		{
			Dictionary<string, object> expando = new Dictionary<string, object>();

			Dictionary<string, object[]> indicator_inputs = new Dictionary<string, object[]>();
			foreach (var indicator in Indicators)
			{
				indicator_inputs.Add(indicator.GetType().Name, indicator.Inputs.ToArray());
			}
			expando.Add("indicators", indicator_inputs);

			return expando;
		}

		public override string ToString()
		{
			return GetName();
		}
	}
}
