using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;
using MarketBot.indicators;
using System.Reflection;

namespace MarketBot
{
	public delegate void PeriodCloseCallback(SymbolData data);

	public class SymbolData
	{
		public string Symbol;
		public Exchanges Exchange;
		public IExchangeOHLCVCollection Data;
		public CustomList<Indicator> Indicators = new CustomList<Indicator>();
		public bool SymbolDataIsLoaded = false;

		public SymbolData(Exchanges exchange, OHLCVInterval interval, string symbol, int periods)
		{
			Exchange = exchange;
			Symbol = symbol;

			ExchangeTasks.CollectOHLCV(exchange, symbol, interval, periods, CollectionCallback);
		}

		public void CollectionCallback(IExchangeOHLCVCollection data)
		{
			Data = data;
			SymbolDataIsLoaded = true;

			// Attach to indicators
			foreach(var indicator in Indicators)
			{
				indicator.AttachSource(data);
			}
		}

		public void ApplyIndicator(Indicator indicator)
		{
			Indicators.Add(indicator);

			if(SymbolDataIsLoaded == true)
			{
				indicator.AttachSource(Data);
			}
		}

		public Indicator GetIndicatorByName(string indicator_name)
		{
			foreach(var indicator in Indicators)
			{
				if(indicator.GetType().Name == indicator_name)
				{
					return indicator;
				}
			}

			return null;
		}

		public Indicator RequireIndicator(string indicator_name, params KeyValuePair<string, object>[] field_list)
		{
			foreach (var indicator in Indicators)
			{
				if (indicator.GetType().Name == indicator_name)
				{
					bool fields_found = true;
					foreach (var pair in field_list)
					{
						FieldInfo property = indicator.GetType().GetField(pair.Key);
						
						if (property != null)
						{
							if (!property.GetValue(indicator).Equals(pair.Value))
							{
								fields_found = false;
								break;
							}
						}
					}

					if (fields_found == true)
					{
						return indicator;
					}
				}
			}

			object[] fields = new object[field_list.Length];
			for(int i = 0; i < fields.Length; i++)
			{
				fields[i] = field_list[i].Value;
			}

			try
			{
				Indicator new_indicator_instance = (Indicator)Activator.CreateInstance(Type.GetType("MarketBot.indicators." + indicator_name), fields);
				Indicators.Add(new_indicator_instance);
				new_indicator_instance.AttachSource(Data);
				return new_indicator_instance;
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return null;
		}

		public void TestStrategy(ISignalStrategy strategy, SignalCallback callback)
		{

		}
	}
}
