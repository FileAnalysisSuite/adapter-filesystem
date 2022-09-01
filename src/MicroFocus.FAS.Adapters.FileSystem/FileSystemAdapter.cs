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

using Microfocus.Common;
using MicroFocus.FAS.AdapterSdk.Api;

namespace MicroFocus.FAS.Adapters.FileSystem
{
    internal class FileSystemAdapter : IRepositoryAdapter
    {
        private readonly ILogger<FileSystemAdapter> _logger;

        public FileSystemAdapter(ILogger<FileSystemAdapter> logger)
        {
            _logger = logger;
        }

        public IAdapterDescriptor CreateDescriptor()
        {
            return new AdapterDescriptor("FileSystemAdapter",
                                         new List<RepositorySettingDefinition>
                                         {
                                             new("Location", TypeCode.String, true, false),
                                             new("UserName", TypeCode.String, true, false),
                                             new("Password", TypeCode.String, true, true)
                                         });
        }

        public async Task RetrieveFileListAsync(RetrieveFileListRequest request, IFileListResultsHandler handler, CancellationToken cancellationToken)
        {
            // Get the repository option provided in UI, the location to scan
            var location = request.RepositoryProperties.RepositoryOptions.GetOption("Location");
            if (location.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Location repository property was not supplied");
            }
            var userName = request.RepositoryProperties.RepositoryOptions.GetOption("UserName");
            var password = request.RepositoryProperties.RepositoryOptions.GetOption("Password");
            _logger.LogInformation("Starting processing of RetrieveFileList request using {Location}, {UserName}", location, userName);
            _logger.LogDebug("RetrieveFileList request details: {@RetrieveFileListRequest}", request);
            try
            {
                // Perform some operation, like a login
                LogIn(userName, password);
            }
            catch (Exception ex)
            {
                await handler.RegisterFailureAsync(new FailureDetails("Failed to log in to the repository", ex));
                return;
            }

            var directoryInfo = new DirectoryInfo(location);


            await ProcessDirectoryAsync(directoryInfo, handler, cancellationToken);
        }

        public async Task RetrieveFilesDataAsync(RetrieveFilesDataRequest request, IFileDataResultsHandler handler, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting processing of RetrieveFilesData request, number of items: {NumItems}", request.Items.Count());

            foreach (var repositoryItem in request.Items)
            {
                if (repositoryItem.Metadata.ItemLocation == null)
                {
                    throw new InvalidOperationException("Repository item metadata ItemLocation property was null!");
                }

                await handler.QueueItemAsync(repositoryItem.ItemId,
                                             () => {
                                                                  var file = new FileInfo(repositoryItem.Metadata.ItemLocation);
                                                                  return file.OpenRead();
                                                             },
                                             repositoryItem.Metadata);
            }
        }

        private async Task ProcessDirectoryAsync(DirectoryInfo directory, IFileListResultsHandler handler, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var file in directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await handler.QueueItemAsync(new ItemMetadata(file.Name, file.FullName)
                                                 {
                                                     Size = file.Length,
                                                     ModifiedTime = file.LastWriteTimeUtc,
                                                     AccessedTime = file.LastAccessTimeUtc,
                                                     CreatedTime = file.CreationTimeUtc
                                                 },
                                                 file.DirectoryName ?? string.Empty,
                                                 cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process directory {Directory}", directory);
                await handler.RegisterFailureAsync(directory.FullName, new FailureDetails($"Failed to process directory {directory}", ex));
            }

            try
            {
                var directories = directory.EnumerateDirectories();
                foreach (var subDirectory in directories)
                {
                    await ProcessDirectoryAsync(subDirectory, handler, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process subdirectories for {Directory}", directory);
                await handler.RegisterFailureAsync(directory.FullName, new FailureDetails($"Failed to process directory {directory}", ex));
            }
        }

        private void LogIn(string? userName, string? password)
        {
            if (userName.IsNullOrEmpty() || password.IsNullOrEmpty())
            {
                return;
            }
            // perform login...
            _logger.LogInformation("Logging in the user: {UserName} / {Password}", userName, password);
        }
    }
}
