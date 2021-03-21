using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarketBot
{
	public class IndicatorList : IEnumerable<KeyValuePair<string, List<object>>>
	{
		public List<KeyValuePair<string, List<object>>> Indicators = new List<KeyValuePair<string, List<object>>>();
		private int position = 0;

		public IndicatorList(List<KeyValuePair<string, List<object>>> indicators)
		{
			Indicators = indicators;
		}

		public IEnumerator<KeyValuePair<string, List<object>>> GetEnumerator()
		{
			return Indicators.GetEnumerator();
		}

		public void Reset()
		{
			position = 0;
		}

		//IEnumerator
		public bool MoveNext()
		{
			position++;
			return (position < Indicators.Count);
		}

		//IEnumerable
		public object Current
		{
			get { return Indicators[position]; }
		}

		public static implicit operator IndicatorList(string input)
		{
			List<KeyValuePair<string, List<object>>> indicators = new List<KeyValuePair<string, List<object>>>();

			// Convert json to Indicators field
			dynamic result = JsonConvert.DeserializeObject(input);

			int indicator = 0;
			while (true)
			{
				var ind = indicator.ToString();
				if(result.ContainsKey(ind))
				{
					List<object> fields = new List<object>();

					if (result[ind].ContainsKey("name"))
					{
						string name = result[ind]["name"];

						if (result[ind].ContainsKey("params"))
						{
							var parameters = (string)result[ind]["params"];
							var split = parameters.Split(',');

							foreach(var value in split)
							{
								if (int.TryParse(value, out int number))
								{
									fields.Add(number);
								}
								else if(decimal.TryParse(value, out decimal dnumber))
								{
									fields.Add(dnumber);
								}
								else
								{
									throw new Exception();
								}
							}
						}

						indicators.Add(new KeyValuePair<string, List<object>>(name, fields));
					}
					else
					{
						throw new Exception("JSON Must contain a 'name' property.");
					}
				}
				else
				{
					break;
				}

				indicator++;
			}

			return new IndicatorList(indicators);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}
}
