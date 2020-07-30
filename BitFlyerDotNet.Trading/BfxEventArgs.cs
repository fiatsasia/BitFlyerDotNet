//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxChildOrderTransactionEventArgs : EventArgs
    {
        public BfxOrderEventType EventType { get; internal set; }
        public BfChildOrderEvent OrderEvent { get; internal set; }
        public IBitFlyerResponse Response { get; internal set; }

        public string ChildOrderAcceptanceId
        {
            get
            {
                if (Response is BitFlyerResponse<BfChildOrderResponse> resp)
                {
                    return resp.GetMessage().ChildOrderAcceptanceId;
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

    public class BfxParentOrderTransactionEventArgs : EventArgs
    {
        public BfxOrderEventType EventType { get; internal set; }
        public BfParentOrderEvent OrderEvent { get; internal set; }
        public IBitFlyerResponse Response { get; internal set; }
    }

    public class BfxOrderEventArgs : EventArgs
    {
        public BfxOrderEventType EventType { get; internal set; }
    }

    public class BfxSimpleOrderEventArgs : BfxOrderEventArgs
    {
        public BfChildOrderEvent OrderEvent { get; internal set; }
        public IBfxSimpleOrder Order { get; internal set; }
    }

    public class BfxConditionalOrderEventArgs : BfxOrderEventArgs
    {
        public BfParentOrderEvent OrderEvent { get; internal set; }
        public BfxParentOrder Order { get; internal set; }
    }
}
