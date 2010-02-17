#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using Lokad.Cloud.Azure.Test;
using NUnit.Framework;

namespace Lokad.Cloud.ServiceFabric.Tests
{
	class MockDisposable : IDisposable
	{
		public bool IsDisposed { get; set; }

		public void Dispose()
		{
			IsDisposed = true;
		}
	}

	[TestFixture]
	public class RuntimeFinalizerTests
	{
		[Test]
		public void IocIsWorking()
		{
			var finalizer = GlobalSetup.Container.Resolve<IRuntimeFinalizer>();
			Assert.IsNotNull(finalizer, "#A00");
		}

		[Test]
		public void Finalize()
		{
			// HACK: using a distinct finalizer, because finalizer can be finalized only once.
			var finalizer = new RuntimeFinalizer();

			var obj = new MockDisposable();

			finalizer.Register(obj);
			finalizer.Unregister(obj);

			Assert.IsFalse(obj.IsDisposed, "#A00");

			finalizer.Register(obj);
			finalizer.FinalizeRuntime();

			Assert.IsTrue(obj.IsDisposed, "#A01");
		}
	}
}
