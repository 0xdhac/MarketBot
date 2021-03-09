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
	public delegate void CSVStepThroughCallback(KeyValuePair<string, object> item, CustomList<OHLCVPeriod> list);
	public class CSVToOHLCData
	{
		public static void Convert(string file_path, CustomList<OHLCVPeriod> output, CSVStepThroughCallback callback)
		{
			using (var reader = new StreamReader(file_path))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				var data = csv.GetRecords<dynamic>();

				foreach (var record in data)
				{
					IDictionary<string, object> props = (IDictionary<string, object>)record;

					foreach (var kvp in props)
					{
						callback(kvp, output);
					}
				}
			}
		}

		public static void HistDataConversion(KeyValuePair<string, object> item, CustomList<OHLCVPeriod> list)
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
}
