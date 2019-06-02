//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitFlyerDotNet.LightningApi;

namespace PrivateApiTests
{
    [TestClass]
    public class UnitTest1
    {
        static BfProductCode _productCode;
        static string _key;
        static string _secret;
        BitFlyerClient _client;

        string _parentOrderAcceptanceId;
        string _parentOrderId;
        string _childOrderAcceptanceId;
        string _childOrderId;

        [TestInitialize]
        public void Initialize()
        {
            _client = new BitFlyerClient(_key, _secret);
        }

        [ClassInitialize]
        public static void Classinitialize(TestContext context)
        {
            _productCode = Enum.Parse<BfProductCode>(context.Properties["ProductCode"].ToString());

            // ApiKey and ApiSecret are defined in PrivateTest.runsettings
            // Should copy that file to any other directory such as desktop and fill them. 
            _key = context.Properties["ApiKey"].ToString();
            _secret = context.Properties["ApiSecret"].ToString();
        }

        bool CheckUnauthorized(IBitFlyerResponse resp)
        {
            if (resp.IsUnauthorized)
            {
                Console.WriteLine("API is Unauthorized by user settings.");
            }
            return resp.IsUnauthorized;
        }

        void DumpJson(IBitFlyerResponse resp)
        {
            Console.WriteLine("JSON:");
            Console.WriteLine(resp.Json);
            Console.WriteLine("--------------------------------");
        }

        [TestMethod]
        public void CancelAllChildOrders()
        {
            var resp = _client.CancelAllChildOrders(_productCode);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);
        }

        [TestMethod]
        [Ignore]
        public void CancelChildOrderByAcceptanceId()
        {
            var resp = _client.CancelChildOrder(_productCode, childOrderAcceptanceId: _childOrderAcceptanceId);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);
        }

        [TestMethod]
        [Ignore]
        public void CancelChildOrderById()
        {
            var resp = _client.CancelChildOrder(_productCode, childOrderId: _childOrderId);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);
        }

        //
        // Order testing needs test server environment
        //
        [TestMethod]
        [Ignore]
        public void CancelParentOrderByAcceptanceId()
        {
            var resp = _client.CancelParentOrder(_productCode, parentOrderAcceptanceId: _parentOrderAcceptanceId);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);
        }

        [TestMethod]
        [Ignore]
        public void CancelParentOrderById()
        {
            var resp = _client.CancelParentOrder(_productCode, parentOrderId: _parentOrderId);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);
        }

        [TestMethod]
        public void GetBalance()
        {
            var resp = _client.GetBalance();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsErrorOrEmpty);
            DumpJson(resp);

            var balances = resp.GetResult();
            balances.ForEach(balance => { Console.WriteLine("{0} {1} {2}", balance.CurrencyCode, balance.Available, balance.Amount); });
        }

        [TestMethod]
        public void GetBankAccounts()
        {
            var resp = _client.GetBankAccounts();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsErrorOrEmpty);
            DumpJson(resp);

            var bankAccounts = resp.GetResult();
            bankAccounts.ForEach(ba =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6}",
                    ba.AccountId,
                    ba.IsVerified,
                    ba.BankName,
                    ba.BranchName,
                    ba.AccountType,
                    ba.AccountNumber,
                    ba.AccountName
                );
            });
        }

        [TestMethod]
        public void GetChildOrders()
        {
            var resp = _client.GetChildOrders(_productCode);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var orders = resp.GetResult();
            orders.ForEach(order =>
            {
                Console.WriteLine($"{order.Side} {order.ChildOrderType} {order.ChildOrderState} {order.ChildOrderAcceptanceId} {order.ExpireDate}");
            });
        }

        [TestMethod]
        public void GetCoinAddresses()
        {
            var resp = _client.GetCoinAddresses();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var coinAddresses = resp.GetResult();
            coinAddresses.ForEach(add => { Console.WriteLine("{0} {1} {2}", add.AddressType, add.CurrencyCode, add.Address); });
        }

        [TestMethod]
        public void GetCoinIns()
        {
            var resp = _client.GetCoinIns();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var coinins = resp.GetResult();
            coinins.ForEach(coinin =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7}",
                    coinin.PagingId,
                    coinin.OrderId,
                    coinin.CurrencyCode,
                    coinin.Amount,
                    coinin.CoinAddress,
                    coinin.TransactionHash,
                    coinin.TransactionStatus,
                    coinin.EventDate.ToLocalTime()
                );
            });
        }

        [TestMethod]
        public void GetCoinOuts()
        {
            var resp = _client.GetCoinOuts();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var coinouts = resp.GetResult();
            coinouts.ForEach(coinout =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                    coinout.PagingId,
                    coinout.OrderId,
                    coinout.CurrencyCode,
                    coinout.Amount,
                    coinout.CoinAddress,
                    coinout.TransactionHash,
                    coinout.Fee,
                    coinout.AdditionalFee,
                    coinout.TransactionStatus,
                    coinout.EventDate.ToLocalTime()
                );
            });
        }

        [TestMethod]
        public void GetCollateral()
        {
            var resp = _client.GetCollateral();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var coll = resp.GetResult();
            Console.WriteLine("{0} {1} {2} {3}",
                coll.Collateral,
                coll.OpenPositionProfitAndLoss,
                coll.RequireCollateral,
                coll.KeepRate
            );
        }

        //
        // GetCollateralHistory always returns "InternalServerError" since end of 2017.
        //
        [TestMethod]
        [Ignore]
        public void GetCollateralHistory()
        {
            var resp = _client.GetCollateralHistory();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var colls = resp.GetResult();
            colls.ForEach(coll =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5}",
                    coll.PagingId,
                    coll.CurrencyCode,
                    coll.Change,
                    coll.Amount,
                    coll.ReasonCode,
                    coll.Date
                );
            });
        }

        [TestMethod]
        public void GetDeposits()
        {
            var resp = _client.GetDeposits();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var deposits = resp.GetResult();
            deposits.ForEach(deposit =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5}",
                    deposit.PagingId,
                    deposit.OrderId,
                    deposit.CurrencyCode,
                    deposit.Amount,
                    deposit.Status,
                    deposit.EventDate
                );
            });
        }

        //
        // Order testing needs test server environment
        //
        [TestMethod]
        [Ignore]
        public void GetParentOrder()
        {
            var resp = _client.GetParentOrder(_productCode, parentOrderAcceptanceId: _parentOrderAcceptanceId);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);
        }

        [TestMethod]
        public void GetParentOrders()
        {
            var resp = _client.GetParentOrders(_productCode);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var orders = resp.GetResult();
            orders.ForEach(order =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5}",
                    order.ParentOrderAcceptanceId,
                    order.ParentOrderId,
                    order.Side,
                    order.ParentOrderType,
                    order.ParentOrderState,
                    order.ParentOrderDate.ToLocalTime()
                );
            });
        }

        [TestMethod]
        public void GetActiveParentOrders()
        {
            var resp = _client.GetParentOrders(_productCode, BfOrderState.Active);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var orders = resp.GetResult();
            orders.ForEach(order =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5}",
                    order.ParentOrderAcceptanceId,
                    order.ParentOrderId,
                    order.Side,
                    order.ParentOrderType,
                    order.ParentOrderState,
                    order.ParentOrderDate.ToLocalTime()
                );
            });
        }

        [TestMethod]
        public void GetPermissions()
        {
            var resp = _client.GetPermissions();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var permissions = resp.GetResult();
            permissions.ForEach(permisson => { Console.WriteLine(permisson); });
        }

        [TestMethod]
        public void GetPositions()
        {
            var resp = _client.GetPositions(_productCode);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var positions = resp.GetResult();
            positions.ForEach(pos =>
            {
                Console.WriteLine("{0} [1} {2} {3}",
                    pos.Side,
                    pos.Price,
                    pos.Size,
                    pos.SwapPointAccumulate,
                    pos.OpenDate.ToLocalTime(),
                    pos.SwapPointAccumulate,
                    pos.SwapForDifference
                );
            });
        }

        [TestMethod]
        public void GetExecutions()
        {
            var resp = _client.GetPrivateExecutions(_productCode, count: 5);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var execs = resp.GetResult();
            execs.ForEach(exec =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4}",
                    exec.Side,
                    exec.Price,
                    exec.Size,
                    exec.Commission,
                    exec.ExecutedTime.ToLocalTime()
                );
            });
        }

        [TestMethod]
        public void GetTradingCommission()
        {
            var resp = _client.GetTradingCommission(_productCode);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsErrorOrEmpty);
            DumpJson(resp);

            var commission = resp.GetResult();
            Console.WriteLine("Commission rate = {0}", commission.CommissionRate);
        }

        [TestMethod]
        public void GetWithdrawals()
        {
            var resp = _client.GetWithdrawals();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);

            var withdrawls = resp.GetResult();
            withdrawls.ForEach(withdrawl =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5}",
                    withdrawl.PagingId,
                    withdrawl.OrderId,
                    withdrawl.CurrencyCode,
                    withdrawl.Amount,
                    withdrawl.Status,
                    withdrawl.EventDate.ToLocalTime()
                );
            });
        }

        [TestMethod]
        [Ignore]
        public void SendChildOrder()
        {
            var resp = _client.SendChildOrder(_productCode, BfOrderType.Limit, BfTradeSide.Buy, 0.0m, 0.0m);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);
        }

        [TestMethod]
        [Ignore]
        public void SendParentOrder()
        {
            var order = new BfParentOrderRequest();
            var resp = _client.SendParentOrder(order);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);
        }

        [TestMethod]
        [Ignore]
        public void Withdraw()
        {
            var request = new BfWithdrawRequest();
            var resp = _client.Withdraw(request);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsError);
            DumpJson(resp);
        }
    }
}
