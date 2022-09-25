using System;
using System.Collections.Generic;
using System.Text;

namespace BitFlyerDotNet.DataSource;

internal class DsPrivateDataSource
{
    // Use when ordered internal.
    public TOrder CreateOrder<TOrder>(string productCode) where TOrder : BfOrderContext
    {
        throw new NotImplementedException();
    }

    // Use when the order event is the origin. If ordered in internal, it is obtained from the store.
    // For ordered in external applications, must generate itself.
    public TOrder GetOrCreateOrderContext<TOrder>(string productCode, string acceptanceId) where TOrder : BfOrderContext
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TOrder> GetActiveOrders<TOrder>(string productCode) where TOrder : BfOrderContext
    {
        throw new NotImplementedException();
    }
}
