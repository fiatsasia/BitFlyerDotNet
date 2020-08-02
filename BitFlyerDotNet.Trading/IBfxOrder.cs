﻿//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Text;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public interface IBfxOrder
    {
        BfProductCode ProductCode { get; }      // 1-3- 56-8
        BfOrderType OrderType { get; }          // 1-3- 56-8   1,5,8:ChildOrderType 3,6:ConditionType
        BfTradeSide? Side { get; }               // 1-3- 56-8
        decimal? OrderPrice { get; }            // 1-3- 56-8   Price
        decimal? OrderSize { get; }              // 1-3- 56-8   Size

        string? AcceptanceId { get; }
        string? Id { get; }
        DateTime? OrderDate { get; }
        DateTime? ExpireDate { get; }
    }

    public interface IBfxExecution
    {
        int Id { get; }
        DateTime Time { get; }
        decimal Price { get; }
        decimal Size { get; }
        decimal? Commission { get; }
        decimal? SfdCollectedAmount { get; }
    }

    public interface IBfxChildOrder : IBfxOrder
    {
        // 1:ChildOrderRequest
        // 2:ChildOrderRequestResponse
        // 3:ParentOrderRequestParameter
        // 4:ParentOrderRequest
        // 5:ChildOrder
        // 6:ParentOrderParameter
        // 7:ParentOrderDetail
        // 8:ChildOrderEvent
        // 9:ParentOrderEvent

        // Request fields
        decimal? TriggerPrice { get; }          // --3- -6--   OrderType is Stop or Stop Limit
        decimal? TrailOffset { get; }           // --3- -6--   Offset; OrderType is Trailing stop
        int MinuteToExpire { get; }             // 1--4 --7-
        BfTimeInForce TimeInForce { get; }      // 1--4 ----

        // Order accepted
        string? ChildOrderAcceptanceId { get; } // -2-- 5--8

        // Order confirmed
        string? ChildOrderId { get; }           // ---- 5--8
        DateTime? OrderDate { get; }            // ---- 5--8  5:ChildOrderDate, 8:EventDate
        DateTime? ExpireDate { get; }           // ---- 5--8  Requested/Completed

        // Order Execution
        decimal? ExecutedSize { get; }          // ---- 5--
        decimal? ExecutedPrice { get; }         // ---- 5--   5:AveragePrice
        decimal? Commission { get; }            // ---- 5--8   5:TotalCommission
        decimal? SfdCollectedAmount { get; }    // ---- ---8
        IBfxExecution[] Executions { get; }     // ---- ---8    ExecutionId, Price, Size, EventDate

        BfxOrderState State { get; }            // ---- 5--   5:ChildOrderState
        string? OrderFailedReason { get; }      // ---- ---8


        // Not used
        // ChildOrder
        // - OutstandingSize ... OrderSize - ExecutedSize
        // - CancelSize ........ When status is canceled, OrderSize - ExecutedSize
        // - 
    }

    public interface IBfxParentOrder : IBfxOrder
    {
        IBfxOrder[] Children { get; }
    }
}