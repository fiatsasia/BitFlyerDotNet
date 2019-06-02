//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Diagnostics;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public enum RequestingState
    {
        Idle,
        Requesting,
        Accepted,
        Confirmed,
    }

    public class BfxChildOrderTransactionState : BfxChildOrder
    {
        BfTradingMarket _market;

        public DateTime RequestedTime { get; private set; }
        public DateTime AcceptedTime { get; private set; }
        public decimal ReferencePrice { get; internal set; }
        public RequestingState OrderingStatus { get; private set; }
        public RequestingState CancelingStatus { get; private set; }

        public Exception OrderFailedException { get; private set; }
        public event EventHandler<BfxChildOrderTransactionEventArgs> StateChanged;

        public BfxChildOrderTransactionState(BfTradingMarket market, BfChildOrderRequest request)
            : base(request)
        {
            _market = market;
        }

        public bool IsTransactionCompleted => (OrderState != BfOrderState.Unknown && OrderState != BfOrderState.Active);

        public bool IsOrderable()
        {
            if (OrderingStatus != RequestingState.Idle || CancelingStatus != RequestingState.Idle)
            {
                DebugEx.Trace();
                return false;
            }
            if (OrderState != BfOrderState.Unknown)
            {
                DebugEx.Trace();
                return false;
            }
            return true;
        }

        void NotifyStateChanged(BfxOrderTransactionEventKind kind, DateTime time)
        {
            try
            {
                StateChanged?.Invoke(this, new BfxChildOrderTransactionEventArgs(kind, this, time));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occuted in user handler. {ex.Message}");
            }
        }

        public bool OnOrderRequested()
        {
            if (!IsOrderable())
            {
                return false;
            }

            RequestedTime = _market.ServerTime;
            OrderingStatus = RequestingState.Requesting;
            if (OrderType == BfOrderType.Market)
            {
                ReferencePrice = Side == BfTradeSide.Buy ? _market.BestBidPrice : _market.BestAskPrice;
            }

            NotifyStateChanged(BfxOrderTransactionEventKind.OrderRequested, RequestedTime);
            return true;
        }

        public override void OnOrderAccepted(BfChildOrderResponse response)
        {
            AcceptedTime = _market.ServerTime;
            OrderingStatus = RequestingState.Accepted;
            base.OnOrderAccepted(response);
            NotifyStateChanged(BfxOrderTransactionEventKind.OrderAccepted, AcceptedTime);
        }

        public void OnOrderFailed(Exception ex)
        {
            OrderFailedException = ex;
            OrderingStatus = RequestingState.Idle;
            NotifyStateChanged(BfxOrderTransactionEventKind.OrderFailed, _market.ServerTime);
        }

        public bool OnOrderConfirmed(BfChildOrder[] orders) // orders is always less or equal 1
        {
            var time = _market.ServerTime;
            bool orderUpdated = false;

            if (orders.Length == 0)
            {
                switch (OrderingStatus)
                {
                    case RequestingState.Accepted:
                        switch (Request.TimeInForce)
                        {
                            case BfTimeInForce.FOK:
                            case BfTimeInForce.IOC:
                                orderUpdated = true;
                                base.OnOrderExpired(time);
                                NotifyStateChanged(BfxOrderTransactionEventKind.OrderKilled, time);
                                break;
                        }
                        break;

                    case RequestingState.Confirmed:
                        if (_market.ServerTime - AcceptedTime > Request.MinuteToExpireSpan)
                        {
                            orderUpdated = true;
                            base.OnOrderExpired(time);
                            NotifyStateChanged(BfxOrderTransactionEventKind.OrderExpired, time);
                        }
                        else
                        {
                            switch (CancelingStatus)
                            {
                                case RequestingState.Idle:
                                case RequestingState.Requesting:
                                    // Probbly canceled from outside environment
                                    orderUpdated = true;
                                    CancelingStatus = RequestingState.Confirmed;
                                    base.OnOrderCanceled();
                                    NotifyStateChanged(BfxOrderTransactionEventKind.CanceledFromOther, time);
                                    break;

                                case RequestingState.Accepted:
                                    orderUpdated = true;
                                    CancelingStatus = RequestingState.Confirmed;
                                    base.OnOrderCanceled();
                                    NotifyStateChanged(BfxOrderTransactionEventKind.CancelConfirmed, time);
                                    break;
                            }
                        }
                        break;

                    default:
                        Debug.Assert(false, "Unexpected ordering state");
                        break;
                }

                return orderUpdated;
            }

            // Child order staus recored found
            var order = orders[0];
            switch (OrderingStatus)
            {
                case RequestingState.Accepted:
                    orderUpdated = true;
                    OrderingStatus = RequestingState.Confirmed;
                    base.OnOrderConfirmed(order);

                    // *** responsed order date resolution is secondly. Somtimes confirmed time is earlier than accepted time
                    NotifyStateChanged(BfxOrderTransactionEventKind.OrderConfirmed, time);
                    break;

                case RequestingState.Confirmed:
                    base.OnOrderConfirmed(order);
                    break;

                case RequestingState.Idle:
                case RequestingState.Requesting:
                default:
                    Debug.Assert(false, "Unexpected ordering state");
                    break;
            }

            switch (CancelingStatus)
            {
                case RequestingState.Requesting:
                    break; // Wait until cancel accepted

                case RequestingState.Accepted:
                    if (order.ChildOrderState == BfOrderState.Canceled)
                    {
                        CancelingStatus = RequestingState.Confirmed;
                        NotifyStateChanged(BfxOrderTransactionEventKind.Canceled, time);
                    }
                    break;
            }

            return orderUpdated;
        }

        public override bool OnExecutionReceived(BfExecution exec)
        {
            OrderingStatus = RequestingState.Confirmed;

            var orderUpdated = base.OnExecutionReceived(exec);
            if (!orderUpdated)
            {
                return false;
            }

            if (ExecutedSize < Size)
            {
                NotifyStateChanged(BfxOrderTransactionEventKind.PartiallyExecuted, exec.ExecutedTime);
            }
            else
            {
                NotifyStateChanged(BfxOrderTransactionEventKind.Executed, exec.ExecutedTime);
            }

            return true;
        }

        public override bool OnExecutionConfirmed(BfPrivateExecution[] execs)
        {
            var orderUpdated = base.OnExecutionConfirmed(execs);
            if (!orderUpdated)
            {
                return false;
            }

            if (ExecutedSize < Size)
            {
                NotifyStateChanged(BfxOrderTransactionEventKind.PartiallyExecuted, execs.Last().ExecutedTime);
            }
            else
            {
                NotifyStateChanged(BfxOrderTransactionEventKind.Executed, execs.Last().ExecutedTime);
            }

            return true;
        }

        public bool IsCancelable()
        {
            if (CancelingStatus != RequestingState.Idle)
            {
                return false; // キャンセル処理中もしくはキャンセル済み
            }

            switch (OrderingStatus)
            {
                case RequestingState.Idle:
                case RequestingState.Requesting:
                    return false; // order IDがないのでCancelできない。
            }

            switch (OrderState)
            {
                case BfOrderState.Unknown:  // 注文送信完了後、未確認状態
                case BfOrderState.Active:   // 注文状態確認済み
                    break;

                case BfOrderState.Completed:
                case BfOrderState.Canceled:
                case BfOrderState.Expired:
                case BfOrderState.Rejected:
                    return false; // キャンセルできない
            }

            return true;
        }

        public bool OnCancelRequested()
        {
            if (!IsCancelable())
            {
                return false;
            }

            // Start canceling
            CancelingStatus = RequestingState.Requesting;
            NotifyStateChanged(BfxOrderTransactionEventKind.CancelRequested, _market.ServerTime);
            return true;
        }

        public void OnCancelAccepted(string result)
        {
            CancelingStatus = RequestingState.Accepted;
            NotifyStateChanged(BfxOrderTransactionEventKind.CancelAccepted, _market.ServerTime);
        }

        public void OnCancelFailed()
        {
            CancelingStatus = RequestingState.Idle;
            NotifyStateChanged(BfxOrderTransactionEventKind.CancelFailed, _market.ServerTime);
        }
    }
}
