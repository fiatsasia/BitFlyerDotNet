//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace PrivateApiTest
{
    class Program
    {
        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
        static BitFlyerClient _client;

        static void Main(string[] args)
        {
            Console.Write("Key:"); var key = Console.ReadLine();
            Console.Write("Secret:"); var secret = Console.ReadLine();
            _client = new BitFlyerClient(key, secret);

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Hit Q key to exit.");

                switch (GetCh())
                {
                    case 'Q':
                        return;

                    case '1':
                        GetCollateralHistory(0, 0, 0); // Occures "InternalServerError"
                        break;
                }
            }

            //GetPermissions();
            //GetBalance();
            //GetCollateral();
            //GetCoinAddresses();
            //GetCoinIns(0, 0, 0);
            //GetCoinOuts(0, 0, 0);
            //GetBankAccounts();
            //GetDeposits(0, 0, 0);
            //GetExecutions(BfProductCode.FXBTCJPY);

            //GetChildOrders(BfProductCode.FXBTCJPY);
            //GetChildOrders(BfProductCode.FXBTCJPY, BfOrderState.Active);
            //GetChildOrdersByAcceptanceId(BfProductCode.FXBTCJPY, "");
            //GetChildOrdersByOrderId(BfProductCode.FXBTCJPY, "");
            //GetChildOrdersByParentOrderId(BfProductCode.FXBTCJPY, "");

            //GetTradingCommission(BfProductCode.BTCJPY);
            //GetPositions(BfProductCode.FXBTCJPY);
        }

        static void GetPermissions()
        {
            var resp = _client.GetPermissions();
            foreach (var permission in resp.GetResult())
            {
                Console.WriteLine(permission);
            }
        }

        static void GetBalance()
        {
            var resp = _client.GetBalance();
            foreach (var balance in resp.GetResult())
            {
                Console.WriteLine("{0} {1} {2}",
                    balance.CurrencyCode,
                    balance.Amount,
                    balance.Available);
            }
        }

        static void GetCollateral()
        {
            var resp = _client.GetCollateral();
            var c = resp.GetResult();
            Console.WriteLine("{0} {1} {2} {3}",
                c.Collateral,
                c.OpenPositionProfitAndLoss,
                c.RequireCollateral,
                c.KeepRate);            
        }

        static void GetCoinAddresses()
        {
            var resp = _client.GetCoinAddresses();
            foreach (var add in resp.GetResult())
            {
                Console.WriteLine("{0} {1} {2}", add.AddressType, add.CurrencyCode, add.Address);
            }
        }

        static void GetCoinIns(int count = 0, int before = 0, int after = 0)
        {
            var resp = _client.GetCoinIns(count, before, after);
            foreach (var c in resp.GetResult())
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7}",
                    c.PagingId,
                    c.OrderId,
                    c.CurrencyCode,
                    c.Amount,
                    c.CoinAddress,
                    c.TransactionHash,
                    c.TransactionStatus,
                    c.EventDate.ToLocalTime());
            }
        }

        static void GetCoinOuts(int count = 0, int before = 0, int after = 0)
        {
            var resp = _client.GetCoinOuts(count, before, after);
            foreach (var c in resp.GetResult())
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                    c.PagingId,
                    c.OrderId,
                    c.CurrencyCode,
                    c.Amount,
                    c.CoinAddress,
                    c.TransactionHash,
                    c.Fee,
                    c.AdditionalFee,
                    c.TransactionStatus,
                    c.EventDate.ToLocalTime());
            }
        }

        static void GetBankAccounts()
        {
            var resp = _client.GetBankAccounts();
            foreach (var ba in resp.GetResult())
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6}",
                    ba.AccountId,
                    ba.IsVerified,
                    ba.BankName,
                    ba.BranchName,
                    ba.AccountType,
                    ba.AccountNumber,
                    ba.AccountName);
            }
        }

        static void GetDeposits(int count = 0, int before = 0, int after = 0)
        {
            var resp = _client.GetDeposits(count, before, after);
            foreach (var d in resp.GetResult())
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5}",
                    d.PagingId,
                    d.OrderId,
                    d.CurrencyCode,
                    d.Amount,
                    d.Status,
                    d.EventDate.ToLocalTime());
            }
        }

        static void GetChildOrders(BfProductCode productCode, int count = 0, int before = 0, int after = 0)
        {
            var resp = _client.GetChildOrders(productCode, count, before, after);
            PrintChildOrders(resp.GetResult());
        }

        static void GetChildOrders(BfProductCode productCode, BfOrderState orderState, int count = 0, int before = 0, int after = 0)
        {
            var resp = _client.GetChildOrders(productCode, orderState, count, before, after);
            PrintChildOrders(resp.GetResult());
        }

        static void GetChildOrdersByAcceptanceId(BfProductCode productCode, string childOrderAcceptanceId)
        {
            var resp = _client.GetChildOrdersByAcceptanceId(productCode, childOrderAcceptanceId);
            PrintChildOrders(resp.GetResult());
        }

        static void GetChildOrdersByOrderId(BfProductCode productCode, string childOrderId)
        {
            var resp = _client.GetChildOrdersByOrderId(productCode, childOrderId);
            PrintChildOrders(resp.GetResult());
        }

        static void GetChildOrdersByParentOrderId(BfProductCode productCode, string parentOrderId)
        {
            var resp = _client.GetChildOrdersByParentOrderId(productCode, parentOrderId);
            PrintChildOrders(resp.GetResult());
        }

        static void PrintChildOrders(BfChildOrder[] orders)
        {
            foreach (var order in orders)
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15}",
                    order.PagingId,
                    order.ChildOrderId,
                    order.ProductCode,
                    order.Side,
                    order.ChildOrderType,
                    order.Price,
                    order.AveragePrice,
                    order.Size,
                    order.ChildOrderState,
                    order.ExpireDate.ToLocalTime(),
                    order.ChildOrderDate.ToLocalTime(),
                    order.ChildOrderAcceptanceId,
                    order.OutstandingSize,
                    order.CancelSize,
                    order.ExecutedSize,
                    order.TotalCommission);
            }
        }

        static void GetExecutions(BfProductCode productCode)
        {
            var resp = _client.GetPrivateExecutions(productCode);
            foreach (var e in resp.GetResult())
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7}",
                    e.ExecutionId,
                    e.ChildOrderId,
                    e.Side,
                    e.Price,
                    e.Size,
                    e.Commission,
                    e.ExecutedTime.ToLocalTime(),
                    e.ChildOrderAcceptanceId);
            }
        }

        static void GetPositions(BfProductCode productCode)
        {
            var resp = _client.GetPositions(productCode);
            foreach (var pos in resp.GetResult())
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}",
                    pos.ProductCode,
                    pos.Side,
                    pos.Price,
                    pos.Size,
                    pos.Commission,
                    pos.SwapPointAccumulate,
                    pos.RequireCollateral,
                    pos.OpenDate.ToLocalTime(),
                    pos.Leverage,
                    pos.ProfitAndLoss,
                    pos.SwapForDifference);
            }
        }

        static void GetCollateralHistory(int count, int before, int after)
        {
            var resp = _client.GetCollateralHistory(count, before, after);
            foreach (var c in resp.GetResult())
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5}",
                    c.PagingId,
                    c.CurrencyCode,
                    c.Change,
                    c.Amount,
                    c.ReasonCode,
                    c.Date);
            }
        }

        static void GetTradingCommission(BfProductCode productCode)
        {
            var resp = _client.GetTradingCommission(productCode);
            Console.WriteLine("{0}", resp.GetResult().CommissionRate);
        }
    }
}
