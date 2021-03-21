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
	public class SymbolData
	{
		public EventHandler PeriodClosed;
		public string Symbol;
		public Exchanges Exchange;
		public IExchangeOHLCVCollection Data;
		public HList<IIndicator> Indicators = new HList<IIndicator>();
		public bool SymbolDataIsLoaded = false;
		private Action<SymbolData> ExternalCollectionCallback;
		public OHLCVInterval Interval;
		public DateTime StartTime;
		public int Periods;
		private bool Screening = false;

		public SymbolData(Exchanges exchange, OHLCVInterval interval, string symbol, int periods, Action<SymbolData> callback, bool screener_update, DateTime? start = null)
		{
			Exchange = exchange;
			Symbol = symbol;
			Interval = interval;
			ExternalCollectionCallback = callback;
			Periods = periods;
			StartTime = start.HasValue ? start.Value : DateTime.UtcNow;
			Screening = screener_update;
			Data = ExchangeTasks.CollectOHLCV(exchange, symbol, interval, periods, CollectionCallback, screener_update, StartTime);
		}

		public SymbolData(string symbol, string[] files, OHLCVInterval interval, CSVConversionMethod method, Action<SymbolData> symbol_loaded_callback)
		{
			Exchange = Exchanges.Localhost;
			Symbol = symbol;
			Data = new GenericOHLCVCollection();
			Interval = interval;

			foreach (var file in files)
			{
				CSVToOHLCData.Convert(file, Data.Data, method);
			}
			
			Periods = Data.Data.Count;
			SymbolDataIsLoaded = true;
			symbol_loaded_callback(this);
		}

		public void CollectionCallback(IExchangeOHLCVCollection data)
		{
			Data.Data.OnAdd_Post += PeriodClosedCallback;
			SymbolDataIsLoaded = true;

			foreach(var indicator in Indicators)
			{
				indicator.AttachSource(data);
			}

			if(!Screening)
				Save();

			ExternalCollectionCallback(this);
		}

		private void PeriodClosedCallback(object sender, EventArgs e)
		{
			if(null != PeriodClosed)
			{
				PeriodClosed(this, null);
			}
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

		public IIndicator RequireIndicator(string indicator_name, params object[] inputs)
		{
			foreach (var indicator in Indicators)
			{
				if (indicator.GetType().Name == indicator_name)
				{
					bool fields_found = true;
					for (int i = 0; i < indicator.Inputs.Count; i++)
					{
						if(i >= inputs.Length)
						{
							throw new Exception("Invalid number of inputs for indicator");
						}

						if (!inputs[i].Equals(indicator.Inputs[i]))
						{
							fields_found = false;
							break;
						}
					}

					if (fields_found == true)
					{
						return indicator;
					}
				}
			}

			IIndicator new_indicator_instance = (IIndicator)Activator.CreateInstance(Type.GetType("MarketBot.indicators." + indicator_name), inputs);
			ApplyIndicator(new_indicator_instance);
			return new_indicator_instance;
		}

		public void Save()
		{
			OHLCDataToCSV.Convert(Data.Data, $"./{Symbol}_{Interval}.csv");
		}
	}
}
