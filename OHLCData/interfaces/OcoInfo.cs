using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot
{
	public enum OcoStatus
	{
		Started,
		Done,
		Rejected
	}

	public partial class OcoInfo
	{
		public OcoStatus Status { get; set; }
		public IEnumerable<long> Orders;
	}
}
