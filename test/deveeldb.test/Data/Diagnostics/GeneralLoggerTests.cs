﻿// 
//  Copyright 2010-2018 Deveel
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Deveel.Data.Events;

using Moq;

using Xunit;

namespace Deveel.Data.Diagnostics {
	public static class GeneralLoggerTests {
		[Theory]
		[InlineData(LogLevel.Error)]
		[InlineData(LogLevel.Debug)]
		public static async void LogToEmpty(LogLevel level) {
			var emptyLogger = Logger.Empty;

			Assert.NotNull(emptyLogger);
			Assert.True(emptyLogger.IsInterestedIn(level));

			await emptyLogger.LogAsync(new LogEntry(null, "test", level));
		}

		[Fact]
		public static void InterceptEvent() {
			var transformer = new Mock<IEventTransformer>();
			transformer.Setup(x => x.Transform(It.IsAny<IEvent>()))
				.Returns<IEvent>(e => new LogEntry(null, e.EventData["message"].ToString()) {
					Data = new Dictionary<string, object> {
						{"os", e.EventSource.Metadata["env.os"]}
					}
				});

			var entries = new List<LogEntry>();

			var logger = new Mock<ILogger>();
			logger.Setup(x => x.IsInterestedIn(It.IsAny<LogLevel>()))
				.Returns(true);
			logger.Setup(x => x.LogAsync(It.IsAny<LogEntry>()))
				.Returns<LogEntry>(entry => {
					entries.Add(entry);

					return Task.CompletedTask;
				});

			var registry = new InMemoryEventRegistry();
			var system = new Mock<IDatabaseSystem>();
			system.As<IEventHandler>()
				.SetupGet(x => x.Registry)
				.Returns(registry);

			SystemEventLogger.Attach(system.Object, logger.Object, transformer.Object);

			var @event = new Event(EventSource.Environment) {
				Data = new Dictionary<string, object> {
					{"message", "test message"}
				}
			};

			system.Object.RaiseEvent(@event);
			registry.Dispose();

			Assert.Single(entries);
			Assert.Equal(LogLevel.Information, entries[0].Level);
			Assert.Equal(@event.Data["message"], entries[0].Message);
			Assert.True(entries[0].Data.ContainsKey("os"));
		}
	}
}