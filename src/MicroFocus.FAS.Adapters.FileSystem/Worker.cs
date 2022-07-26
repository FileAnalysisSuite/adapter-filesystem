/**
 * Copyright 2022 Micro Focus or one of its affiliates.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using MicroFocus.FAS.AdapterSdk.Api;

namespace MicroFocus.FAS.Adapters.FileSystem
{
    public class Worker : BackgroundService
    {
        private readonly IProcessingEngineFactory _engineFactory;
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, IProcessingEngineFactory engineFactory)
        {
            _logger = logger;
            _engineFactory = engineFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() => _logger.LogInformation("Worker - cancellation requested, service will stop"));
            _logger.LogInformation("Starting Worker Service...");
            try
            {
                var processingEngine = _engineFactory.CreateEngine();

                await processingEngine.ExecuteAsync(stoppingToken).ConfigureAwait(false);

                _logger.LogInformation("Stopping worker service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught during execution");
                throw;
            }
        }
    }
}
