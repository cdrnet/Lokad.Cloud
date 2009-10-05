#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud;
using Lokad.Cloud.Mock;
using NUnit.Framework;
using System.Collections.Generic;

namespace TestingSample
{
	[TestFixture]
	public class BlobBackupToolTests
	{
		[Test]
		public void BackupBlob()
		{
			const string SourceContainer = "my-container-1";
			const string DestinationContainer = "backup-1";

			// This should resolve to a memory-based provider, but you can change the behavior in app.config
			var blobStorage = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

			var backupTool = new BlobBackupTool(blobStorage);

			Assert.IsFalse(backupTool.BackupBlob(SourceContainer, "my-blob", DestinationContainer),
				"Container does not exist: backup should fail");

			blobStorage.CreateContainer(SourceContainer);
			Assert.IsFalse(backupTool.BackupBlob(SourceContainer, "my-blob", DestinationContainer),
				"Blob does not exist: backup should fail");

			blobStorage.PutBlob(SourceContainer, "my-blob", 42);
			Assert.IsFalse(backupTool.BackupBlob(SourceContainer, "my-blob", DestinationContainer),
				"Destination container does not exist: backup should fail");

			blobStorage.CreateContainer(DestinationContainer);
			Assert.IsTrue(backupTool.BackupBlob(SourceContainer, "my-blob", DestinationContainer),
				"Backup should succeed");

			var backupBlobs = blobStorage.List(DestinationContainer, "");
			Assert.AreEqual(1, backupBlobs.Count());
			var blobName = backupBlobs.First();
			Assert.IsTrue(blobName.EndsWith("-my-blob"), "Wrong backup blob name format");
			Assert.IsTrue(blobName.Length == "YYMMDDHHMMSS-my-blob".Length, "Wrong backup blob name format");

			Assert.AreEqual(42, blobStorage.GetBlob<int>(DestinationContainer, blobName), "Wrong backup blob content");
		}

		[Test]
		public void BackupAllBlobs()
		{
			const string SourceContainer = "my-container-2";
			const string DestinationContainer = "backup-2";

			// This should resolve to a memory-based provider, but you can change the behavior in app.config
			var blobStorage = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

			var backupTool = new BlobBackupTool(blobStorage);

			Assert.IsFalse(backupTool.BackupAllBlobs(SourceContainer, DestinationContainer),
				"Container does not exist: backup should fail");

			blobStorage.PutBlob(SourceContainer, "my-blob1", 42);
			blobStorage.PutBlob(SourceContainer, "my-blob2", -75);
			Assert.IsFalse(backupTool.BackupAllBlobs(SourceContainer, DestinationContainer),
				"Destination container does not exist: backup should fail");

			blobStorage.CreateContainer(DestinationContainer);
			Assert.IsTrue(backupTool.BackupAllBlobs(SourceContainer, DestinationContainer),
				"Backup should succeed");

			var backupBlobs = blobStorage.List(DestinationContainer, "");
			Assert.AreEqual(2, backupBlobs.Count());

			// Use a list for easier testing
			var allNames = new List<string>(backupBlobs);

			Assert.IsTrue(allNames[0].EndsWith("-my-blob1"), "Wrong backup blob name format");
			Assert.IsTrue(allNames[0].Length == "YYMMDDHHMMSS-my-blob1".Length, "Wrong backup blob name format");
			Assert.AreEqual(42, blobStorage.GetBlob<int>(DestinationContainer, allNames[0]), "Wrong backup blob content");

			Assert.IsTrue(allNames[1].EndsWith("-my-blob2"), "Wrong backup blob name format");
			Assert.IsTrue(allNames[1].Length == "YYMMDDHHMMSS-my-blob2".Length, "Wrong backup blob name format");
			Assert.AreEqual(-75, blobStorage.GetBlob<int>(DestinationContainer, allNames[1]), "Wrong backup blob content");
		}
	}
}
