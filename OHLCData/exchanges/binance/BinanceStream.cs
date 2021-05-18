using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot.UserStream;
using Binance.Net.Objects.Spot;
using MarketBot.tools;
using System.Net.Sockets;
using CryptoExchange.Net.Sockets;

namespace MarketBot.exchanges.binance
{
	static class BinanceStream
	{
		static BinanceSocketClient SocketClient = null;
		static UpdateSubscription Subscription = null;
		public static void CreateUserStream()
		{
			new Task(() =>
			{
				while (true)
				{
					CreateStream();
					System.Threading.Thread.Sleep(3600000);
				}
			}).Start();
		}

		private static async void CreateStream()
		{
			if(SocketClient != null)
			{
				await SocketClient.Unsubscribe(Subscription);
			}

			using (var client = new BinanceClient())
			{
				var key = client.Spot.UserStream.StartUserStream().Data;

				SocketClient = new BinanceSocketClient(new BinanceSocketClientOptions()
				{
					AutoReconnect = true,
					ReconnectInterval = TimeSpan.FromSeconds(5),
				});

				var result = await SocketClient.Spot.SubscribeToUserDataUpdatesAsync(key, OrderUpdate, OcoOrderUpdate, PositionUpdate, BalanceUpdate);

				Subscription = result.Data;

				Subscription.ConnectionLost += Data_ConnectionLost;
				Subscription.ConnectionRestored += Data_ConnectionRestored;
				Subscription.ActivityPaused += Data_ActivityPaused;
				Subscription.ActivityUnpaused += Data_ActivityUnpaused;
			}
		}

		private static void Data_ActivityUnpaused()
		{
			Program.Print("Data_ActivityUnpaused");
			RealtimeBot.Finish = false;
		}

		private static void Data_ActivityPaused()
		{
			Program.Print("Data_ActivityPaused");
			RealtimeBot.Finish = true;
		}

		private static void Data_ConnectionRestored(TimeSpan obj)
		{
			Program.Print("Data_ConnectionRestored");
			RealtimeBot.Finish = false;
		}

		private static void Data_ConnectionLost()
		{
			Program.Print("Data_ConnectionLost");
			RealtimeBot.Finish = true;
		}

		private static void OrderUpdate(BinanceStreamOrderUpdate update)
		{
			foreach(var pos in Position.Positions.ToArray())
			{
				if(pos.Symbol == update.Symbol)
				{
					pos.OrderStreamUpdate(update);
				}
			}
			// Not in positions list
			/*
			foreach (var pos in Position.Positions)
			{
				if(pos.Symbol == update.Symbol) // If it matches the symbol
				{
					if (pos.Real == true) // If it's a real money position
					{
						OrderSide signal_direction = (pos.Type == SignalType.Long) ? OrderSide.Buy : OrderSide.Sell;

						if(update.Side == signal_direction) // If the order type matches the signal direction
						{
							if(update.Status == OrderStatus.PartiallyFilled) // If it only fills partially
							{
								pos.Filled += update.LastQuantityFilled;
								if (new Regex($"^{update.CommissionAsset}").IsMatch(update.Symbol))
								{
									pos.Commission += update.Commission;
								}
							}
							if (update.Status == OrderStatus.Filled)
							{
								pos.Status			= PositionStatus.Filled;
								if (new Regex($"^{update.CommissionAsset}").IsMatch(update.Symbol))
								{
									pos.Commission += update.Commission;
								}
								pos.Filled			+= update.LastQuantityFilled;
								pos.Quantity		= ExchangeTasks.GetStepSizeAdjustedQuantity(Exchanges.Binance, update.Symbol, pos.Filled - pos.Commission);

								BinanceOrders.PlaceOcoOrder(pos.Symbol, 0, 0, 0, pos.Quantity);
							}
							else if (update.Status == OrderStatus.Rejected || update.Status == OrderStatus.Expired)
							{
								RealtimeBot.Wallets[Exchanges.Binance].Available += (pos.Quantity - update.QuantityFilled) * pos.Entry;
								Console.WriteLine($"[{pos.Symbol}] Order {update.Status}");
								ToRemove.Add(pos); // Remove expired/rejected buys
							}
							else if(update.Status == OrderStatus.New)
							{
								// Update wallet balance
								RealtimeBot.Wallets[Exchanges.Binance].Available -= (update.Quantity * update.Price);

								pos.OrderId = update.OriginalClientOrderId;
								//update.Order
								Task.Run(() => 
								{
									string setting = Program.GetConfigSetting("CANCEL_BUY_ORDER_AFTER_X_SECONDS");
									bool success = int.TryParse(setting, out int seconds);

									if(!success)
									{
										Console.WriteLine($"Error: Config setting 'CANCEL_BUY_ORDER_AFTER_X_SECONDS' not found. Defaulted to 10 seconds.");
										Program.LogError($"Config setting 'CANCEL_BUY_ORDER_AFTER_X_SECONDS' not found.");
										seconds = 10;
									}

									System.Threading.Thread.Sleep(seconds * 1000);

									if(pos != null)
									{
										if(pos.Status == PositionStatus.Ordered)
										{
											BinanceOrders.CancelAnyOrders(pos.Symbol, OrderType.Limit, signal_direction);
										}
									}
								});
							}
							else if(update.Status == OrderStatus.Canceled)
							{
								// Update wallet balance, increase based on quantity that was unfilled during the order
								RealtimeBot.Wallets[Exchanges.Binance].Available += (pos.Quantity - update.QuantityFilled) * pos.Entry;

								if(pos.Filled > 0)
								{
									pos.Status = PositionStatus.Filled;
									pos.Quantity = ExchangeTasks.GetStepSizeAdjustedQuantity(Exchanges.Binance, update.Symbol, pos.Filled - pos.Commission);

									BinanceOrders.PlaceOcoOrder(pos.Symbol, 0, 0, 0, pos.Quantity);
								}
							}
						}
						else // if an asset tries to sell when the entry was to buy
						{
							if (update.Status == OrderStatus.Expired && update.Price > pos.Entry)
							{
								//Program.Print($"market selling after x seconds {update.Symbol} {update.Status}");
								Task.Run(() =>
								{
									string setting = Program.GetConfigSetting("CANCEL_AND_MARKET_SELL_ORDER_AFTER_X_SECONDS");
									bool success = int.TryParse(setting, out int seconds);

									if (!success)
									{
										Console.WriteLine($"Error: Config setting 'CANCEL_AND_MARKET_SELL_ORDER_AFTER_X_SECONDS' not found. Defaulted to 5 seconds.");
										Program.LogError($"Config setting 'CANCEL_AND_MARKET_SELL_ORDER_AFTER_X_SECONDS' not found.");
										seconds = 5;
									}

									System.Threading.Thread.Sleep(seconds * 1000);

									if (pos != null)
									{
										if (pos.Status == PositionStatus.Oco)
										{
											BinanceOrders.CancelAnyOrders(pos.Symbol, OrderType.StopLossLimit, signal_direction + 1 % 2);
											BinanceOrders.PlaceOrder(pos, signal_direction + 1 % 2, OrderType.Market, 0, pos.Filled, -2010); // Ignore rejected order in the case that the stop loss limit was a success
										}
									}
								});
							}
							else if(update.Status == OrderStatus.PartiallyFilled)
							{
								pos.Filled -= update.LastQuantityFilled;
							}
							else if(update.Status == OrderStatus.Filled)
							{
								//Console.WriteLine($"{pos.Symbol} Sold: {update.LastQuantityFilled}");
								pos.Filled -= update.LastQuantityFilled;
								if(++RealtimeBot.Trades[pos.Exchange] % RealtimeBot.ResetBetAmountEvery == 0)
								{
									ExchangeTasks.GetWallet(pos.Exchange, RealtimeBot.OnWalletLoaded); // Reset bet size
								}

								if((update.Price > pos.Entry && signal_direction == OrderSide.Buy) || (update.Price < pos.Entry && signal_direction == OrderSide.Sell))
								{
									RealtimeBot.Wins[pos.Exchange]++;
								}
								else
								{
									RealtimeBot.Losses[pos.Exchange]++;
								}

								int losses = RealtimeBot.Losses[pos.Exchange];
								int wins = RealtimeBot.Wins[pos.Exchange];
								Console.WriteLine($"[{pos.Symbol}] Winrate: {(wins / (decimal)(wins + losses)):.00}");
								ToRemove.Add(pos); // Remove filled sells
							}
						}
					}
				}
			
			}
			*/
		}

		private static void OcoOrderUpdate(BinanceStreamOrderList update)
		{
			
		}

		public static void PositionUpdate(BinanceStreamPositionsUpdate update)
		{

		}

		public static void BalanceUpdate(BinanceStreamBalanceUpdate update)
		{
			Console.WriteLine($"{update.Asset} balance update. {update.BalanceDelta}");
		}
	}
}
