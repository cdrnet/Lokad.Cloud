#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

using Lokad.Cloud.Mock;
using NUnit.Framework;

namespace Lokad.Cloud.Test.Mock.Services
{
	[TestFixture]
	public class QueueServiceTests
	{
		[Test]
		public void SquareServiceTest()
		{
			var providersForCloudStorage = new ProvidersForCloudStorage(new MemoryBlobStorageProvider(),
				new MemoryQueueStorageProvider(new BinaryFormatter()), new MemoryLogger());
			
			var service = new SquareQueueService { Providers = providersForCloudStorage };
			var blobStorage = providersForCloudStorage.BlobStorage;

			const string containerName = "mockContainer";

			//filling blobs to be processed.
			for (int i = 0 ; i < 10 ; i++)
			{
				blobStorage.PutBlob(containerName, "blob" + i, (double)i);
			}

			var squareMessage = new SquareMessage
				{
					ContainerName = containerName,
					Expiration = DateTime.Now + new TimeSpan(10, 0, 0, 0),
					IsStart = true
				};

			var queueName = TypeMapper.GetStorageName(typeof(SquareMessage));
			providersForCloudStorage.QueueStorage.Put(queueName, squareMessage);

			for (int i = 0 ; i < 2 ; i++)
			{
				service.StartService();
			}

			var blobNames = providersForCloudStorage.BlobStorage.List(containerName, "");
			var sum = blobNames.Select(e => providersForCloudStorage.BlobStorage.GetBlob<double>(containerName, e)).Sum();
 
			//0*0+1*1+2*2+3*3+...+9*9 = 285
			Assert.AreEqual(285, sum, "result is different from expected.");	
		}

		[Serializable]
		class SquareMessage
		{
			public bool IsStart { get; set; }

			public DateTime Expiration { get; set; }

			public string ContainerName { get; set;}

			public string BlobName { get; set;}

			public TemporaryBlobName BlobCounter { get; set; }
		}

		[QueueServiceSettings(AutoStart = true, BatchSize = 100, //QueueName = "SquareQueue",
		Description = "multiply numbers by themselves.")]
		class SquareQueueService : QueueService<SquareMessage>
		{
			public void StartService()
			{
				StartImpl();
			}

			protected override void Start(SquareMessage message)
			{
				var blobStorage = Providers.BlobStorage;

				if (message.IsStart)
				{
					var counterName = TemporaryBlobName.GetNew(message.Expiration);
					var counter = new BlobCounter(blobStorage, counterName);
					counter.Reset(BlobCounter.Aleph);

					var blobNames = blobStorage.List(message.ContainerName, "");
					
					foreach (var blobName in blobNames)
					{
						Put(new SquareMessage
							{
								BlobName = blobName,
								ContainerName = message.ContainerName,
								IsStart = false,
								BlobCounter = counterName
							});
					}

					// dealing with rare race condition
					if (0m >= counter.Increment(-BlobCounter.Aleph + blobNames.Count()))
					{
						Finish(counter);
					}
					
				}
				else
				{
					var value = blobStorage.GetBlob<double>(message.ContainerName, message.BlobName);
					blobStorage.PutBlob<double>(message.ContainerName, message.BlobName, value * value);

					var counter = new BlobCounter(Providers.BlobStorage, message.BlobCounter);
					if (0m >= counter.Increment(-1))
					{
						Finish(counter);
					}
				}
			}

			void Finish(BlobCounter counter)
			{
				counter.Delete();
			}
		}
	}
}
