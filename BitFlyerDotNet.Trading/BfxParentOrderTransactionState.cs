//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Diagnostics;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxParentOrderTransactionState : BfxParentOrder
    {
        BfTradingMarket _market;

        public DateTime RequestedTime { get; private set; }
        public DateTime AcceptedTime { get; private set; }

        public RequestingState OrderingStatus { get; private set; }
        public RequestingState CancelingStatus { get; private set; }

        public Exception OrderFailedException { get; private set; }
        public event EventHandler<BfxParentOrderTransactionEventArgs> StateChanged;

        public BfxParentOrderTransactionState(BfTradingMarket market, BfParentOrderRequest request)
            : base(request)
        {
            _market = market;
        }

        public bool IsOrderable()
        {
            if (OrderingStatus != RequestingState.Idle || CancelingStatus != RequestingState.Idle)
            {
                DebugEx.Trace();
                return false;
            }
            if (ParentOrderState != BfOrderState.Unknown)
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
                StateChanged?.Invoke(this, new BfxParentOrderTransactionEventArgs(kind, this, time));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occuted in user handler. {ex.Message}");
            }
        }

        public bool OnParentOrderRequested()
        {
            RequestedTime = _market.ServerTime;
            OrderingStatus = RequestingState.Requesting;
            NotifyStateChanged(BfxOrderTransactionEventKind.OrderRequested, RequestedTime);
            return true;
        }

        public override void OnParentOrderAccepted(BfParentOrderResponse response)
        {
            AcceptedTime = _market.ServerTime;
            OrderingStatus = RequestingState.Accepted;
            base.OnParentOrderAccepted(response);
            NotifyStateChanged(BfxOrderTransactionEventKind.OrderAccepted, AcceptedTime);
        }

        public void OnParentOrderFailed(Exception ex)
        {
            OrderFailedException = ex;
            OrderingStatus = RequestingState.Idle;
            NotifyStateChanged(BfxOrderTransactionEventKind.OrderFailed, _market.ServerTime);
        }

        // Order detail confirmation is only called single time
        public override void OnParentOrderConfirmed(BfParentOrderDetail order)
        {
            OrderingStatus = RequestingState.Confirmed;
            base.OnParentOrderConfirmed(order);
            NotifyStateChanged(BfxOrderTransactionEventKind.OrderConfirmed, _market.ServerTime);
        }

        public void OnParentOrderConfirmed(BfParentOrder[] orders)
        {
            Debug.Assert(orders.Length == 1);
            base.OnParentOrderConfirmed(orders[0]);
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

            switch (ParentOrderState)
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

        public bool OnParentOrderCancelRequested()
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

        public void OnParentOrderCancelAccepted(string result)
        {
            CancelingStatus = RequestingState.Accepted;
            NotifyStateChanged(BfxOrderTransactionEventKind.CancelAccepted, _market.ServerTime);
        }

        public void OnParentOrderCancelFailed()
        {
            CancelingStatus = RequestingState.Idle;
            NotifyStateChanged(BfxOrderTransactionEventKind.CancelFailed, _market.ServerTime);
        }

        public override void OnChildOrderConfirmed(BfChildOrder[] orders)
        {
            base.OnChildOrderConfirmed(orders);
        }
    }
}
