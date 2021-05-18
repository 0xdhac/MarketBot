using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot
{
	public partial class OrderResult
	{
		public Exchanges Exchange { get; set; }
		public decimal QuantityFilled { get; set; }
		public GenericOrderSide OrderSide { get; set; }
		public GenericOrderType OrderType { get; set; }
		public string Asset { get; set; }
		public string Symbol { get; set; }
		public string CommissionAsset { get; set; }
		public decimal CommissionQuantity { get; set; }
		public long OrderListId { get; set; }
		public long OrderId { get; set; }
		public GenericOrderStatus Status { get; set; }
		public decimal? Price { get; set; }
	}
}
