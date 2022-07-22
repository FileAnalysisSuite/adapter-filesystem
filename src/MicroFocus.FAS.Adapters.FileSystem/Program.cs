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

using MicroFocus.FAS.Adapters.FileSystem;
using MicroFocus.FAS.AdapterSdk.Engine.Runtime;
using MicroFocus.FAS.AdapterSdk.Runtime.NetCore;

AppInitializer.Initialize();

var host = Host.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration((context, builder) => builder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory).AddJsonFile("appsettings.json"))
               .ConfigureServices((context, services) =>
                                  {
                                      services.ConfigureAdapterSdk<FileSystemAdapter>(context.Configuration, "FileSystemAdapter").AddHostedService<Worker>();
                                      services.Configure<FileSystemAdapterConfiguration>(context.Configuration
                                                                                                .GetSection(nameof(FileSystemAdapterConfiguration)));
                                  })
               .UseWindowsService()
               .Build();

await host.RunAsync();