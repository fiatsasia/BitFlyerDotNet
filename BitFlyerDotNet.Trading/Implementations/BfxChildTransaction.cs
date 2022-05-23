//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Threading;
using System.Threading.Tasks;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxChildTransaction : BfxTransaction
    {
        // Public properties
        public override string MarketId => _order.AcceptanceId;
        public override IBfxOrder Order => _order;
        public override BfxOrderState OrderState => Order.State;
        public BfxParentTransaction? Parent { get; }
        public override bool HasParent => Parent != null;

        protected override void CancelTransaction() => _cts.Cancel();

        // Private properties
        CancellationTokenSource _cts = new CancellationTokenSource();
        BfxChildOrder _order;

        public BfxChildTransaction(BfxMarket market, BfxChildOrder order, BfxParentTransaction parent)
            : base(market)
        {
            _order = order;
            Parent = parent;
        }

        public BfxChildTransaction(BfxMarket2 market, BfxChildOrder order, BfxParentTransaction parent)
            : base(market)
        {
            _order = order;
            Parent = parent;
        }

        public BfxChildTransaction(BfxMarket market, BfxChildOrder order)
            : base(market)
        {
            _order = order;
        }

        public BfxChildTransaction(BfxMarket2 market, BfxChildOrder order)
            : base(market)
        {
            _order = order;
        }

        // - 経過時間でリトライ終了のオプション
        public async Task SendOrderRequestAsync()
        {
            if (_order.Request == null)
            {
                throw new BitFlyerDotNetException();
            }

            try
            {
                ChangeState(BfxTransactionState.SendingOrder);
                NotifyEvent(BfxOrderEventType.OrderSending);
                for (var retry = 0; retry <= Market.Config.OrderRetryMax; retry++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var resp = await Market.Client.SendChildOrderAsync(_order.Request, _cts.Token);
                    if (!resp.IsError)
                    {
                        Market.OrderCache?.OpenChildOrder(_order.Request, resp.GetContent());
                        _order.Update(resp.GetContent());
                        ChangeState(BfxTransactionState.WaitingOrderAccepted);
                        NotifyEvent(BfxOrderEventType.OrderSent, Market.ServerTime, resp);
                        Market.RegisterTransaction(this);
                        return;
                    }

                    Log.Warn($"SendChildOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
                    _cts.Token.ThrowIfCancellationRequested();
                    Log.Info("Trying retry...");
                    await Task.Delay(Market.Config.OrderRetryInterval);
                }

                Log.Error("SendOrderRequest - Retried out");
                ChangeState(BfxTransactionState.Idle);
                NotifyEvent(BfxOrderEventType.OrderSendFailed);
                throw new BitFlyerDotNetException();
            }
            catch (OperationCanceledException ex)
            {
                Log.Trace("SendChildOrderRequestAsync is canceled");
                ChangeState(BfxTransactionState.Idle);
                NotifyEvent(BfxOrderEventType.OrderSendCanceled, Market.ServerTime, ex);
            }
        }

        // - エラーリトライ(無限リトライ)
        // - 注文執行によるキャンセルの中止 => CancelFailed受信
        // - 注文送信リトライ中のキャンセル
        protected override async Task SendCancelOrderRequestAsync()
        {
            ChangeState(BfxTransactionState.SendingCancel);
            NotifyEvent(BfxOrderEventType.CancelSending);
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                var resp = await Market.Client.CancelChildOrderAsync(Market.ProductCode, string.Empty, _order.AcceptanceId, _cts.Token);
                if (!resp.IsError)
                {
                    ChangeState(BfxTransactionState.CancelAccepted);
                    NotifyEvent(BfxOrderEventType.CancelSent, Market.ServerTime, resp);
                }
                else
                {
                    ChangeState(BfxTransactionState.Idle);
                    NotifyEvent(BfxOrderEventType.CancelSendFailed, Market.ServerTime, resp);
                }
            }
            catch (OperationCanceledException ex)
            {
                ChangeState(BfxTransactionState.Idle);
                NotifyEvent(BfxOrderEventType.CancelSendCanceled, Market.ServerTime, ex);
            }
        }

        // Call from BfxMarket
        public override void OnChildOrderEvent(BfChildOrderEvent coe)
        {
            if (coe.ChildOrderAcceptanceId != _order.AcceptanceId)
            {
                throw new ArgumentException();
            }

            _order.Update(coe);

            switch (coe.EventType)
            {
                case BfOrderEventType.Order: // Order registered
                    ChangeState(BfxTransactionState.Idle);
                    NotifyEvent(BfxOrderEventType.Ordered, coe);
                    break;

                case BfOrderEventType.OrderFailed:
                    ChangeState(BfxTransactionState.Idle);
                    NotifyEvent(BfxOrderEventType.OrderFailed, coe);
                    break;

                case BfOrderEventType.Cancel:
                    ChangeState(BfxTransactionState.Idle);
                    NotifyEvent(BfxOrderEventType.Canceled, coe);
                    break;

                case BfOrderEventType.CancelFailed:
                    ChangeState(BfxTransactionState.Idle);
                    NotifyEvent(BfxOrderEventType.CancelFailed, coe);
                    break;

                case BfOrderEventType.Execution:
                    NotifyEvent(Order.State == BfxOrderState.PartiallyExecuted ? BfxOrderEventType.PartiallyExecuted : BfxOrderEventType.Executed, coe);
                    break;

                case BfOrderEventType.Expire:
                    NotifyEvent(BfxOrderEventType.Expired, coe);
                    break;

                case BfOrderEventType.Unknown:
                case BfOrderEventType.Complete:
                case BfOrderEventType.Trigger: // Not happened when Simple Order ?
                    throw new NotSupportedException();
            }
        }
    }
}
