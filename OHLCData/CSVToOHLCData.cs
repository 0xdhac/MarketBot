using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CsvHelper;
using System.Globalization;
using System.Dynamic;
using CsvHelper.Configuration;

namespace MarketBot
{
	public enum CSVConversionMethod
	{
		Standard,
		Histdata,
		BinanceVision
	}
	public delegate void CSVStepThroughCallback(KeyValuePair<string, object> item, HList<OHLCVPeriod> list);
	public class CSVToOHLCData
	{
		public static void Convert(string file_path, HList<OHLCVPeriod> output, CSVConversionMethod method)
		{
			using (var reader = new StreamReader(file_path))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				switch (method)
				{
				case CSVConversionMethod.Standard:
					StandardConversion(file_path, output);
					break;
				case CSVConversionMethod.Histdata:
					HistDataConversion(file_path, output);
					break;
				case CSVConversionMethod.BinanceVision:
					BinanceVisionConversion(file_path, output);
					break;
				}

			}
		}

		public static void OutputCSVData(string file_path)
		{
			using (var reader = new StreamReader(file_path))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				var data = csv.GetRecords<dynamic>();

				int i = 0;
				foreach (var record in data)
				{
					if(i++ < 10)
					{
						IDictionary<string, object> props = (IDictionary<string, object>)record;

						foreach (var kvp in props)
						{
							Console.WriteLine(kvp.Value);
						}
					}
					
				}
			}
		}

		// Conversion for CSV files from histdata.com
		public static void HistDataConversion(string file_path, HList<OHLCVPeriod> list)
		{
			
			//new CsvReader()
			using (var reader = new StreamReader(file_path))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				var data = csv.GetRecords<dynamic>();

				foreach (var record in data)
				{
					IDictionary<string, object> props = (IDictionary<string, object>)record;

					foreach (var kvp in props)
					{
						string[] exploded_item = kvp.Value.ToString().Split(';');

						OHLCVPeriod period = new OHLCVPeriod();
						period.Open = decimal.Parse(exploded_item[1]);
						period.High = decimal.Parse(exploded_item[2]);
						period.Low = decimal.Parse(exploded_item[3]);
						period.Close = decimal.Parse(exploded_item[4]);
						list.Add(period);
					}
				}
			}

		}

		// Conversion for CSV files from histdata.com
		public static void StandardConversion(string file_path, HList<OHLCVPeriod> list)
		{
			using (var reader = new StreamReader(file_path))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				var records = csv.GetRecords<OHLCVPeriod>();
				foreach (var record in records)
				{
					list.Add(record);
				}
			}
		}

		public static void BinanceVisionConversion(string file_path, HList<OHLCVPeriod> list)
		{
			var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ",", HasHeaderRecord = false };
			using (var reader = new StreamReader(file_path))
			using (var csv = new CsvReader(reader, config))
			{
				var data = csv.GetRecords<dynamic>();

				foreach (var record in data)
				{
					OHLCVPeriod period = new OHLCVPeriod();

					period.Date = new DateTime(1970, 1, 1).AddMilliseconds(double.Parse(record.Field1));
					period.Open = decimal.Parse(record.Field2);
					period.High = decimal.Parse(record.Field3);
					period.Low = decimal.Parse(record.Field4);
					period.Close = decimal.Parse(record.Field5);
					period.Volume = decimal.Parse(record.Field6);
					period.CloseTime = new DateTime(1970, 1, 1).AddMilliseconds(double.Parse(record.Field7));

					list.Add(period);
				}
			}	
		}
	}

	public class OHLCDataToCSV
	{
		public static void Convert(HList<OHLCVPeriod> data, string file_path)
		{
			using (var writer = new StreamWriter(file_path))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecords(data);
			}
		}

		public static void OutputCSVData(string file_path)
		{
			using (var reader = new StreamReader(file_path))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				var data = csv.GetRecords<dynamic>();

				int i = 0;
				foreach (var record in data)
				{
					if (i++ < 10)
					{
						IDictionary<string, object> props = (IDictionary<string, object>)record;

						foreach (var kvp in props)
						{
							Console.WriteLine(kvp.Value);
						}
					}

				}
			}
		}
	}
}
