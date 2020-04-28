//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxChildOrder
    {
        public BfChildOrderRequest Request { get; private set; }
        BfChildOrderResponse _response;
        BfChildOrder _order;

        // When use child of parent order
        public BfParentOrderRequestParameter RequestElement { get; private set; }
        public BfChildOrderElement OrderElement { get; private set; }

        List<IBfExecution> _wsExecs = new List<IBfExecution>();
        List<IBfExecution> _restExecs = new List<IBfExecution>();

        // From  child order request
        public BfProductCode ProductCode => Request?.ProductCode ?? RequestElement.ProductCode;
        public new BfOrderType OrderType => Request?.OrderType ?? RequestElement.ConditionType;
        public BfTradeSide Side => Request?.Side ?? RequestElement.Side;
        public decimal Size => Request?.Size ?? RequestElement.Size;
        public decimal Price => Request?.Price ?? RequestElement.Price;

        // Valid if child order of parent order
        public decimal TriggerPrice => RequestElement?.TriggerPrice ?? decimal.MinValue;
        public decimal Offset => RequestElement?.Offset ?? decimal.MinValue;
        public BfOrderType UnderlyingOrder => _order.ChildOrderType;

        // From response
        public string AcceptanceId { get; private set; } = string.Empty;

        // From confirmed chila order
        public string OrderId => _order?.ChildOrderId ?? string.Empty;
        public decimal AveragePrice => _order.AveragePrice;
        public DateTime OrderDate => _order.ChildOrderDate;
        public decimal OutstandingSize => _order.OutstandingSize;
        public decimal TotalCommission => _order.TotalCommission;

        // Manage by this class
        public BfOrderState OrderState { get; private set; } = BfOrderState.Unknown;
        public decimal ExecutedPrice { get; private set; }
        public decimal ExecutedSize { get; private set; }
        public DateTime ExecutedTime { get { return _wsExecs.Select(e => e.ExecutedTime).DefaultIfEmpty().Max(); } }
        public decimal CancelSize { get; private set; }
        public DateTime ExpireDate { get; private set; }

        public IEnumerable<IBfExecution> Executions => (_wsExecs.Count >= _restExecs.Count) ? _wsExecs : _restExecs;
        public bool IsOrderCompleted => (OrderState != BfOrderState.Unknown && OrderState != BfOrderState.Active);

        public event EventHandler<BfxChildOrderEventArgs> OrderChanged;

        public BfxChildOrder(BfChildOrderRequest request)
        {
            Request = request;
        }

        public BfxChildOrder(BfParentOrderRequestParameter request)
        {
            RequestElement = request;
        }

        void NotifyOrderChanged()
        {
            try
            {
                OrderChanged?.Invoke(this, new BfxChildOrderEventArgs(this));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occuted in user handler. {ex.Message}");
            }
        }

        public virtual void OnOrderAccepted(BfChildOrderResponse response)
        {
            _response = response;
            AcceptanceId = _response.ChildOrderAcceptanceId;
            OrderState = BfOrderState.Active;
            NotifyOrderChanged();
        }

        public virtual bool OnOrderConfirmed(BfChildOrder order)
        {
            _order = order;

            AcceptanceId = order.ChildOrderAcceptanceId; // Set first if child of parent
            CancelSize = order.CancelSize;

            var updated = false;
            if (OrderState != _order.ChildOrderState)
            {
                if (OrderState != BfOrderState.Completed)
                {
                    OrderState = _order.ChildOrderState;
                    updated = true;
                }
            }

            if (_order.ExecutedSize > ExecutedSize)
            {
                ExecutedSize = _order.ExecutedSize;
                ExecutedPrice = _order.AveragePrice;
                updated = true;
            }

            if (updated)
            {
                NotifyOrderChanged();
            }

            return updated;
        }

        public virtual void OnOrderConfirmed(BfChildOrderElement order)
        {
            OrderElement = order;
        }

        // bF API erase canceled order from the list.
        public void OnOrderCanceled()
        {
            OrderState = BfOrderState.Canceled;
            CancelSize = Size;
            NotifyOrderChanged();
        }

        public void OnOrderExpired(DateTime time)
        {
            OrderState = BfOrderState.Expired;
            CancelSize = Size;
            ExpireDate = time;
            NotifyOrderChanged();
        }

        public virtual bool OnExecutionReceived(BfExecution exec)
        {
            _wsExecs.Add(exec);
            var executedSize = _wsExecs.Sum(e => e.Size);
            if (executedSize > ExecutedSize)
            {
                ExecutedSize = executedSize;
                // ToDo:
                // marketから価格の精度を取得して丸める。
                //
                ExecutedPrice = _wsExecs.Sum(e => e.Price * e.Size) / executedSize;
                if (ExecutedSize == Size)
                {
                    OrderState = BfOrderState.Completed;
                }

                NotifyOrderChanged();
                return true;
            }
            return false;
        }

        public virtual bool OnExecutionConfirmed(BfPrivateExecution[] execs)
        {
            if (execs.Length == 0)
            {
                return false;
            }

            _restExecs.AddRange(execs);
            var executedSize = execs.Sum(e => e.Size);
            if (executedSize > ExecutedSize)
            {
                ExecutedSize = executedSize;
                // ToDo:
                // marketから価格の精度を取得して丸める。
                //
                ExecutedPrice = execs.Sum(e => e.Price * e.Size) / executedSize;
                if (ExecutedSize == Size)
                {
                    OrderState = BfOrderState.Completed;
                }
                NotifyOrderChanged();
                return true;
            }
            return false;
        }
    }
}
