using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CsvHelper;
using System.Globalization;
using System.Dynamic;

namespace MarketBot
{
	public enum CSVConversionMethod
	{
		Standard,
		Histdata
	}
	public delegate void CSVStepThroughCallback(KeyValuePair<string, object> item, CustomList<OHLCVPeriod> list);
	public class CSVToOHLCData
	{
		
		public static void Convert(string file_path, CustomList<OHLCVPeriod> output, CSVConversionMethod method)
		{
			using (var reader = new StreamReader(file_path))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				switch (method)
				{
				case CSVConversionMethod.Standard:
						var records = csv.GetRecords<OHLCVPeriod>();
						foreach(var record in records)
						{
							output.Add(record);
						}

						break;
				case CSVConversionMethod.Histdata:
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

							output.Add(period);
						}
					}
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
		public static void HistDataConversion(KeyValuePair<string, object> item, CustomList<OHLCVPeriod> list)
		{
			
		}

		// Conversion for CSV files from histdata.com
		public static void StandardConversion(KeyValuePair<string, object> item, CustomList<OHLCVPeriod> list)
		{
			string[] exploded_item = item.Value.ToString().Split(';');

			OHLCVPeriod period = new OHLCVPeriod();
			period.Open = decimal.Parse(exploded_item[1]);
			period.High = decimal.Parse(exploded_item[2]);
			period.Low = decimal.Parse(exploded_item[3]);
			period.Close = decimal.Parse(exploded_item[4]);

			list.Add(period);
		}
	}

	public class OHLCDataToCSV
	{
		public static void Convert(CustomList<OHLCVPeriod> data, string file_path)
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
