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
    public class BfxParentOrder
    {
        public BfParentOrderRequest Request { get; private set; }
        BfParentOrderResponse _response;
        public BfParentOrderDetail OrderDetail { get; private set; }
        BfParentOrder _order;

        public bool IsSimpleOrder => Request.OrderMethod == BfOrderType.Simple;

        public int PagingId => OrderDetail.PagingId;
        public string OrderId => OrderDetail.ParentOrderId;

        public BfProductCode ProductCode { get; }
        public BfOrderType OrderType => Request.OrderMethod;
        public BfTradeSide Side => _order.Side; // include BuySell

        public string AcceptanceId { get; private set; } = string.Empty;
        public BfOrderState ParentOrderState { get; private set; } = BfOrderState.Unknown;
        public DateTime ExpireDate { get; private set; }

        public List<BfxChildOrder> ChildOrders { get; private set; } = new List<BfxChildOrder>();

        public event EventHandler<BfxChildOrderEventArgs> ChildOrderChanged
        {
            add { ChildOrders.ForEach(e => { e.OrderChanged += value; }); }
            remove { ChildOrders.ForEach(e => { e.OrderChanged -= value; }); }
        }

        public event EventHandler<BfxParentOrderEventArgs> ParentOrderChanged;

        public BfxParentOrder(BfParentOrderRequest request)
        {
            Request = request;
            ProductCode = request.Paremters[0].ProductCode;

            foreach (var childRequest in request.Paremters)
            {
                ChildOrders.Add(new BfxChildOrder(childRequest));
            }
        }

        void NotifyOrderChanged()
        {
            try
            {
                ParentOrderChanged?.Invoke(this, new BfxParentOrderEventArgs(this));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occuted in user handler. {ex.Message}");
            }
        }

        public virtual void OnParentOrderAccepted(BfParentOrderResponse response)
        {
            _response = response;
            AcceptanceId = response.ParentOrderAcceptanceId;
            ParentOrderState = BfOrderState.Active;
            NotifyOrderChanged();
        }

        public virtual void OnParentOrderConfirmed(BfParentOrderDetail order)
        {
            OrderDetail = order;
            order.ChildOrders.Zip(ChildOrders, (o2, o1) => (o1, o2))
                .ForEach(e => e.o1.OnOrderConfirmed(e.o2));
        }

        public virtual void OnParentOrderConfirmed(BfParentOrder order)
        {
            _order = order;
            var updated = false;
            if (ParentOrderState != _order.ParentOrderState)
            {
                updated = true;
            }
            ParentOrderState = _order.ParentOrderState;
            ExpireDate = _order.ExpireDate;

            if (updated)
            {
                NotifyOrderChanged();
            }
        }

        public void OnOrderCanceled()
        {
            ParentOrderState = BfOrderState.Canceled;
            NotifyOrderChanged();
        }

        public void OnOrderExpired(DateTime time)
        {
            ParentOrderState = BfOrderState.Expired;
            ExpireDate = time;
            NotifyOrderChanged();
        }

        public virtual void OnChildOrderConfirmed(BfChildOrder[] orders)
        {
            if (orders.Length == 0)
            {
                return;
            }

            // 1. Simple order (stop/stop limit/trailing stop)
            // - If condition is done, order will be disptached as limit price or market price.
            // 2. IFD
            // - At first, condition order will be dispatched.
            // - At be done first order, second order will be dispatched.
            // 3. OCO
            // - At first, both of orders will be dispatched.
            // - At be done first or second order, other order will be canceled.
            // 4. IFDOCO
            // - At first, first order will be dispached.
            // - At be done first order, second and thirnd order will be dispached at same time.
            // - At be done second or third order, other order will be canceld.
            for (int orderPos = 0; orderPos < orders.Length; orderPos++)
            {
                ChildOrders[orderPos].OnOrderConfirmed(orders[orderPos]);
            }
        }
    }
}
