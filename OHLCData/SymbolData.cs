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
	public delegate void SymbolDataLoadedCallback(SymbolData data);

	public class SymbolData
	{
		public string Symbol;
		public Exchanges Exchange;
		public IExchangeOHLCVCollection Data;
		public CustomList<IIndicator> Indicators = new CustomList<IIndicator>();
		public bool SymbolDataIsLoaded = false;
		private SymbolDataLoadedCallback ExternalCollectionCallback;
		public OHLCVInterval Interval;
		public DateTime StartTime;
		public int Periods;

		public SymbolData(Exchanges exchange, OHLCVInterval interval, string symbol, int periods, SymbolDataLoadedCallback callback, DateTime? start = null)
		{
			Exchange = exchange;
			Symbol = symbol;
			Interval = interval;
			ExternalCollectionCallback = callback;
			Periods = periods;
			StartTime = start.HasValue ? start.Value : DateTime.UtcNow;

			Data = ExchangeTasks.CollectOHLCV(exchange, symbol, interval, periods, CollectionCallback, StartTime);
		}

		public SymbolData(string file_path, CSVConversionMethod method, SymbolDataLoadedCallback symbol_loaded_callback)
		{
			Exchange = Exchanges.Localhost;
			Symbol = file_path;
			Data = new GenericOHLCVCollection();

			CSVToOHLCData.Convert(file_path, Data.Data, method);
			SymbolDataIsLoaded = true;

			Periods = Data.Data.Count;

			symbol_loaded_callback(this);
		}

		public void CollectionCallback(IExchangeOHLCVCollection data)
		{
			SymbolDataIsLoaded = true;

			foreach(var indicator in Indicators)
			{
				indicator.AttachSource(data);
			}

			ExternalCollectionCallback(this);
		}

		public void ApplyIndicator(IIndicator indicator)
		{
			Indicators.Add(indicator);

			if(SymbolDataIsLoaded == true)
			{
				indicator.AttachSource(Data);
			}
		}

		public IIndicator GetIndicatorByName(string indicator_name)
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

		public IIndicator RequireIndicator(string indicator_name, params KeyValuePair<string, object>[] field_list)
		{
			//Console.WriteLine()
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

			IIndicator new_indicator_instance = (IIndicator)Activator.CreateInstance(Type.GetType("MarketBot.indicators." + indicator_name), fields);
			ApplyIndicator(new_indicator_instance);
			return new_indicator_instance;
		}
	}
}
