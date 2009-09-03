#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Threading;
using Lokad.Cloud;
using NUnit.Framework;
using NUnit.Framework.Extensions;

// TODO: [vermorel] 2009-07-23, excluded from the built
// Focusing on a minimal amount of feature for the v0.1
// will be reincluded later on.

namespace Lokad.Cloud.Test
{
	[TestFixture]
	public class LockTests
	{
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_NullProvider()
		{
			new Lock(null, "mylock", new TimeSpan(0, 1, 0), new TimeSpan(0, 1, 0));
		}

		[RowTest]
		[Row(null, ExpectedException = typeof(ArgumentNullException))]
		[Row("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_InvalidLockId(string lockId)
		{
			new Lock(new InMemoryBlobStorageProvider(), lockId,
				new TimeSpan(0, 1, 0), new TimeSpan(0, 1, 0));
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void Constructor_ShortTimeout()
		{
			new Lock(new InMemoryBlobStorageProvider(), "mylock",
				new TimeSpan((Lock.MinimumTimeoutInMS - 1) * TimeSpan.TicksPerMillisecond),
				new TimeSpan(0, 1, 0));
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void Constructor_ShortLockDuration()
		{
			new Lock(new InMemoryBlobStorageProvider(), "mylock",
				new TimeSpan(0, 1, 0),
				new TimeSpan((Lock.MinimumTimeoutInMS - 1) * TimeSpan.TicksPerMillisecond));
		}

		[Test]
		public void Lock_Simple()
		{
			IBlobStorageProvider provider = new InMemoryBlobStorageProvider();

			Console.Write("Trying to acquire lock...");
			using(new Lock(provider, "mylock"))
			{
				Console.WriteLine(" acquired!");
			}
			Console.WriteLine("Lock disposed");
		}

		[Test]
		public void Lock_Simple_DifferentLocks()
		{
			IBlobStorageProvider provider = new InMemoryBlobStorageProvider();

			Console.Write("Trying to acquire lock1...");
			using (new Lock(provider, "mylock1"))
			{
				Console.WriteLine(" acquired!");

				Console.Write("Trying to acquire lock2...");
				using (new Lock(provider, "mylock2"))
				{
					Console.WriteLine(" acquired!");
				}
				Console.WriteLine("Lock2 disposed");
			}
			Console.WriteLine("Lock1 disposed");
		}

		[Test, Explicit]
		public void Lock_WaitWithoutTimeout()
		{
			// TODO: find another way to test concurrency without having race conditions
			// The result of this test mostly depends on a race condition

			bool lock1Done = false;
			IBlobStorageProvider provider = new InMemoryBlobStorageProvider();

			// Launch an async operation to acquire a second lock just while the first lock is being used
			new Thread(() =>
			{
				Thread.Sleep(500);
				Console.Write("Trying to acquire lock2...");
				using (new Lock(provider, "mylock"))
				{
					Assert.IsTrue(lock1Done, "Lock1 was not used");
					Console.WriteLine(" acquired!");
				}
				Console.Write("Lock2 released");
			}).Start();

			Console.Write("Trying to acquire lock1...");
			using (new Lock(provider, "mylock"))
			{
				Console.WriteLine(" acquired!");
				Console.Write("Working on lock1...");
				Thread.Sleep(2000);
				lock1Done = true;
				Console.WriteLine(" done!");
			}
			Console.WriteLine("Lock1 released");
		}

		[Test, Explicit]
		public void Lock_WaitWithTimeout()
		{
			// TODO: find another way to test concurrency without having race conditions
			// The result of this test mostly depends on a race condition

			bool lock1Done = false;
			bool timeoutHit = false;
			IBlobStorageProvider provider = new InMemoryBlobStorageProvider();

			// Launch an async operation to acquire a second lock just while the first lock is being used
			new Thread(() =>
			{
				Thread.Sleep(500);
				Console.Write("Trying to acquire lock2...");
				try
				{
					using (new Lock(provider, "mylock", new TimeSpan(0, 0, 1), new TimeSpan(2, 0, 0)))
					{
						Assert.Fail("Should not get here");
					}
				}
				catch (TimeoutException)
				{
					timeoutHit = true;
				}
				Assert.IsTrue(timeoutHit, "Timeout was not hit");
			}).Start();

			Console.Write("Trying to acquire lock1...");
			using (new Lock(provider, "mylock"))
			{
				Console.WriteLine(" acquired!");
				Console.Write("Working on lock1...");
				Thread.Sleep(2000);
				lock1Done = true;
				Console.WriteLine(" done!");
			}
			Console.WriteLine("Lock1 released");

			Assert.IsTrue(lock1Done, "Lock1 was not used");
		}

		[Test, Explicit]
		public void Lock_Refresh()
		{
			// TODO: find another way to test concurrency without having race conditions
			// The result of this test mostly depends on a race condition

			bool lock1Done = false;
			bool timeoutHit = false;
			IBlobStorageProvider provider = new InMemoryBlobStorageProvider();

			// Launch an async operation to acquire a second lock just while the first lock is being used
			new Thread(() =>
			{
				Thread.Sleep(1000);
				try
				{
					Console.Write("Trying to acquire lock2...");
					using (new Lock(provider, "mylock", new TimeSpan(0, 0, 1), new TimeSpan(0, 1, 0)))
					{
						Assert.Fail("Should not get here");
					}
				}
				catch (TimeoutException)
				{
					timeoutHit = true;
				}
				Assert.IsTrue(timeoutHit, "Timeout was not hit");
			}).Start();

			Console.Write("Trying to acquire lock1...");
			using (Lock lk = new Lock(provider, "mylock",
				TimeSpan.MaxValue, new TimeSpan(2, 0, 0)))
			{
				Console.WriteLine(" acquired!");
				Console.Write("Working on lock1...");
				Thread.Sleep(600);
				lk.Refresh();
				Thread.Sleep(800);
				lock1Done = true;
				Console.WriteLine(" done!");
			}
			Console.WriteLine("Lock1 released");

			Assert.IsTrue(lock1Done, "Lock1 was not used");
		}

		[Test]
		public void Lock_StealLock_WithRetries()
		{
			bool lock2Acquired = false;
			IBlobStorageProvider provider = new InMemoryBlobStorageProvider();

			Console.Write("Trying to acquire lock1...");
			Lock lk = new Lock(provider, "mylock", TimeSpan.MaxValue, new TimeSpan(0, 0, 1));
			Console.WriteLine("Lock1 acquired for 1 second");

			Console.Write("Trying to acquire lock2...");
			using (new Lock(provider, "mylock", new TimeSpan(0, 0, 10), new TimeSpan(0, 0, 1)))
			{
				Console.WriteLine(" acquired!");
				lock2Acquired = true;
			}
			Console.WriteLine("Lock2 released");

			Assert.IsTrue(lock2Acquired, "Lock2 was not acquired");
		}

		[Test]
		public void Lock_StealLock_WithoutRetries()
		{
			InMemoryBlobStorageProvider provider = new InMemoryBlobStorageProvider();

			Lock lock1 = new Lock(provider, "mylock", TimeSpan.MaxValue, new TimeSpan(0, 0, 1));

			Thread.Sleep(1500);

			// Lock1 is now expired

			Lock lock2 = new Lock(provider, "mylock");

			bool hasThrownOnRefresh = false;
			try
			{
				lock1.Refresh();
			}
			catch (InvalidOperationException)
			{
				hasThrownOnRefresh = true;
			}

			Assert.IsTrue(hasThrownOnRefresh, "Refresh should have thrown");

			// Should do nothing
			lock1.Dispose();

			// Should work
			lock2.Refresh();
			lock2.Dispose();
		}

	}

	/// <summary>
	/// Implements an in-memory blob storage provider for testing purposes.
	/// </summary>
	/// <remarks><para>Could not use a mocking framework because the provider must
	/// really store data that is accessed in potentially random order and
	/// this behavior is difficult if not impossible to achieve using a mocking FW.</para>
	/// <para>Not all methods are implemented.</para>
	/// <para>Methods throw an exception once every two invocations of any method.</para></remarks>
	public class InMemoryBlobStorageProvider : IBlobStorageProvider
	{
		private Dictionary<string, Dictionary<string, object>> _containers =
			new Dictionary<string, Dictionary<string, object>>();

		#region IBlobStorageProvider Members

		public bool CreateContainer(string containerName)
		{
			if (!_containers.ContainsKey(containerName))
			{
				_containers.Add(containerName, new Dictionary<string, object>());
				return true;
			}
			else return false;
		}

		public bool DeleteContainer(string containerName)
		{
			return _containers.Remove(containerName);
		}

		public void PutBlob<T>(string containerName, string blobName, T item)
		{
			PutBlob<T>(containerName, blobName, item, true);
		}

		public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite)
		{
			if (overwrite)
			{
				_containers[containerName][blobName] = item;
				return true;
			}
			else
			{
				if (_containers[containerName].ContainsKey(blobName)) return false;
				else
				{
					_containers[containerName][blobName] = item;
					return true;
				}
			}
		}

		public T GetBlob<T>(string containerName, string blobName)
		{
			object value = null;
			if (_containers[containerName].TryGetValue(blobName, out value))
			{
				return (T)value;
			}
			else return default(T);
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, T> updater, out T result)
		{
			throw new NotImplementedException();
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, T> updater)
		{
			throw new NotImplementedException();
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, Lokad.Result<T>> updater, out Lokad.Result<T> result)
		{
			throw new NotImplementedException();
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, Lokad.Result<T>> updater)
		{
			throw new NotImplementedException();
		}

		public bool DeleteBlob(string containerName, string blobName)
		{
			return _containers[containerName].Remove(blobName);
		}

		public IEnumerable<string> List(string containerName, string prefix)
		{
			throw new NotImplementedException();
		}

		#endregion

	}

}
