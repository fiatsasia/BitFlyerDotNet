//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace TradingApiTests;

public static class NLogServices
{
    public class NLogAdapter : BitFlyerDotNet.LightningApi.LogAdapter
    {
        NLog.Logger _logger;

        public NLogAdapter(NLog.Logger logger)
        {
            _logger = logger;
        }

        public override void Trace(string message) => _logger.Trace(message);
        public override void Debug(string message) => _logger.Debug(message);
        public override void Info(string message) => _logger.Info(message);
        public override void Warn(string message) => _logger.Warn(message);
        public override void Error(string message) => _logger.Error(message);
        public override void Error(Exception ex) => _logger.Error(ex);
        public override void Error(string message, Exception ex) => _logger.Error(ex, message);
        public override void Fatal(string message) => _logger.Fatal(message);
    }

    public static BfxApplication AddTraceLoggingService(this BfxApplication app, NLog.Logger logger)
    {
        BitFlyerDotNet.LightningApi.Log.Instance = new NLogAdapter(logger);
        return app;
    }
}
