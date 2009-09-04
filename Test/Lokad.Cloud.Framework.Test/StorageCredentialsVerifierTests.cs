#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Samples.ServiceHosting.StorageClient;
using NUnit.Framework;
using Lokad.Cloud.Azure.Test;

namespace Lokad.Cloud.Framework.Test
{
	[TestFixture]
	public class StorageCredentialsVerifierTests
	{

		[Test]
		public void VerifyCredentials()
		{
			var storage = GlobalSetup.Container.Resolve<BlobStorage>();

			StorageCredentialsVerifier verifier = new StorageCredentialsVerifier(storage);

			Assert.IsTrue(verifier.VerifyCredentials(), "Credentials should be verified");
		}

	}

}
