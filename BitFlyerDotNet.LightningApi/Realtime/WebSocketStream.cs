//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace BitFlyerDotNet.LightningApi
{
    class WebSocketStream : MemoryStream
    {
        readonly ClientWebSocket _ws;

        public WebSocketStream(ClientWebSocket ws)
        {
            _ws = ws;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            if (_ws.State != WebSocketState.Open)
            {
                return 0;
            }

            WebSocketReceiveResult wsrr;
            do
            {
                wsrr = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), ct);
            } while (!wsrr.EndOfMessage);

            if (wsrr.MessageType == WebSocketMessageType.Close)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
                return 0;
            }

            return wsrr.Count;
        }

        public override async Task FlushAsync(CancellationToken ct)
        {
            await _ws.SendAsync(new ArraySegment<byte>(ToArray()), WebSocketMessageType.Binary, true, ct);
            SetLength(0);
        }
    }
}
