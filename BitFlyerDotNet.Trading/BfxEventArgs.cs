//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxPositionChangedEventArgs : EventArgs
    {
        public BfxPositionChange Position { get; internal set; }
        public bool IsOpened => Position.Close == null;

        public BfxPositionChangedEventArgs(BfxPositionChange pos)
        {
            Position = pos;
        }
    }

    public class BfxOrderTransactionEventArgs : EventArgs
    {
        public BfxOrderTransactionEventType EventType { get; internal set; }
        public BfxOrderTransactionState State { get; internal set; }
        public BfxOrderState OrderState { get; internal set; }
        public DateTime Time { get; internal set; }

        public IBfxOrder Order { get; internal set; }
    }

    public class BfxChildOrderTransactionEventArgs : BfxOrderTransactionEventArgs
    {
        public BfChildOrderEvent OrderEvent { get; internal set; }
        public IBitFlyerResponse Response { get; internal set; }

        public string ChildOrderAcceptanceId
        {
            get
            {
                if (Response is BitFlyerResponse<BfChildOrderResponse> resp)
                {
                    return resp.GetContent().ChildOrderAcceptanceId;
                }
                else if (OrderEvent != null)
                {
                    return OrderEvent.ChildOrderAcceptanceId;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }

    public class BfxParentOrderTransactionEventArgs : BfxOrderTransactionEventArgs
    {
        public BfParentOrderEvent OrderEvent { get; internal set; }
        public IBitFlyerResponse Response { get; internal set; }
    }

    public class BfxOrderEventArgs : EventArgs
    {
        public BfxOrderTransactionEventType EventType { get; internal set; }
    }
}
