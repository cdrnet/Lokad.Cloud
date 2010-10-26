#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using NUnit.Framework;

namespace Lokad.Cloud.Test
{
	[TestFixture]
	public class StandaloneTests
	{
		[Test]
		public void Can_create_populated_providers_from_settings()
		{
			var settings = RoleConfigurationSettings.LoadFromRoleEnvironment();
			CloudInfrastructureProviders providers;
			if (settings.HasValue)
			{
				providers = Standalone.CreateProviders(settings.Value);
				VerifyBlobProviderWorks(providers, true);
			}
			else
			{
				providers = Standalone.CreateProviders(
					new RoleConfigurationSettings {DataConnectionString = "UseDevelopmentStorage=true"});
				VerifyBlobProviderWorks(providers, false);
			}
			Assert.IsInstanceOf(typeof(Storage.Azure.BlobStorageProvider), providers.BlobStorage);
		}

		[Test]
		public void Can_create_populated_providers_from_connection_string()
		{
			var providers = Standalone.CreateProviders("UseDevelopmentStorage=true");
			VerifyBlobProviderWorks(providers, false);
			Assert.IsInstanceOf(typeof(Storage.Azure.BlobStorageProvider), providers.BlobStorage);
		}

		[Test]
		public void Can_create_populated_providers_from_appconfig()
		{
			var providers = Standalone.CreateProvidersFromConfiguration("autofac");
			VerifyBlobProviderWorks(providers, true);
			Assert.IsInstanceOf(typeof(Storage.Azure.BlobStorageProvider), providers.BlobStorage);
		}

		[Test]
		public void Can_create_populated_development_storage_providers()
		{
			var providers = Standalone.CreateDevelopmentStorageProviders();
			VerifyBlobProviderWorks(providers, false);
			Assert.IsInstanceOf(typeof(Storage.Azure.BlobStorageProvider), providers.BlobStorage);
		}

		[Test]
		public void Can_create_populated_mock_storage_providers()
		{
			var providers = Standalone.CreateMockProviders();
			VerifyBlobProviderWorks(providers, true);
			Assert.IsInstanceOf(typeof(Mock.MemoryBlobStorageProvider), providers.BlobStorage);
		}

		void VerifyBlobProviderWorks(CloudInfrastructureProviders providers, bool verifyBlobRoundtrip)
		{
			Assert.NotNull(providers);
			Assert.NotNull(providers.BlobStorage);
			Assert.NotNull(providers.QueueStorage);
			Assert.NotNull(providers.TableStorage);

			if (verifyBlobRoundtrip)
			{
				Assert.IsTrue(providers.BlobStorage.PutBlob("test-container-standalone", "blob", "message", false));

				var blob = providers.BlobStorage.GetBlob<string>("test-container-standalone", "blob");
				Assert.IsTrue(blob.HasValue);
				Assert.AreEqual("message", blob.Value);

				Assert.IsTrue(providers.BlobStorage.DeleteContainer("test-container-standalone"));
			}
		}
	}
}
