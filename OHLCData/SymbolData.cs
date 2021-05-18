using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;
using MarketBot.indicators;
using System.Reflection;
using MarketBot.tools;
using System.IO;

namespace MarketBot
{
	public class SymbolData
	{
		public EventHandler PeriodClosed;
		public string Symbol;
		public Exchanges Exchange;
		public IExchangeOHLCVCollection Data;
		public HList<Indicator> Indicators = new HList<Indicator>();
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
			
			StartTime = start.HasValue ? start.Value : DateTime.UtcNow;
			

			if(exchange == Exchanges.Localhost)
			{
				var pattern = symbol + "-" + BinanceAnalyzer.GetKlineInterval(interval) + "*";

				
				var files = Directory.GetFiles(Program.GetConfigSetting("KLINE_DATA_FOLDER"), pattern);

				files.OrderBy((v1) => v1);

				Data = new GenericOHLCVCollection();

				foreach (var file in files)
				{
					CSVToOHLCData.Convert(file, Data.Periods, CSVConversionMethod.BinanceVision);
				}

				Periods = Data.Periods.Count;
				SymbolDataIsLoaded = true;
				callback(this);
			}
			else
			{
				Screening = screener_update;
				Periods = periods;
				ExchangeTasks.CollectOHLCV(exchange, symbol, interval, periods, CollectionCallback, screener_update, StartTime);
			}
		}

		public SymbolData(string symbol, string[] files, OHLCVInterval interval, CSVConversionMethod method, Action<SymbolData> symbol_loaded_callback = null)
		{
			Exchange = Exchanges.Localhost;
			Symbol = symbol;
			Data = new GenericOHLCVCollection();
			Interval = interval;

			foreach (var file in files)
			{
				CSVToOHLCData.Convert(file, Data.Periods, method);
			}
			
			Periods = Data.Periods.Count;
			SymbolDataIsLoaded = true;

			if(symbol_loaded_callback != null)
				symbol_loaded_callback(this);
		}

		public OHLCVPeriod this[int index] { get => Data.Periods[index]; }

		public void CollectionCallback(IExchangeOHLCVCollection data)
		{
			Data = data;
			Data.Periods.OnAdd_Post += PeriodClosedCallback;
			SymbolDataIsLoaded = true;

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

		public void ApplyIndicator(Indicator indicator)
		{
			Indicators.Add(indicator);
		}

		public void DeleteIndicators()
		{
			Indicators.RemoveRange(0, Indicators.Count);
		}

		public void DeleteIndicators(HList<Indicator> list)
		{
			Indicators.RemoveAll((i) => list.Contains(i));
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

		public Indicator RequireIndicator(string indicator_name, params object[] inputs)
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

			List<object> input_list = new List<object>();
			input_list.Add(this);
			foreach(var input in inputs)
			{
				input_list.Add(input);
			}
			Indicator new_indicator_instance = (Indicator)Activator.CreateInstance(Type.GetType("MarketBot.indicators." + indicator_name), input_list.ToArray());
			ApplyIndicator(new_indicator_instance);
			return new_indicator_instance;
		}

		public void Save()
		{
			OHLCDataToCSV.Convert(Data.Periods, $"./{Symbol}_{Interval}.csv");
		}
	}
}
