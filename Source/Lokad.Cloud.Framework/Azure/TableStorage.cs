#region (c)2009 Lokad - New BSD license

// Copyright (c) Lokad 2009 
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Azure {
	
	/// <summary>Shorthand class for accessing table storage service classes.</summary>
	public class TableStorage
	{
		internal TableStorage(CloudTableClient client, TableServiceContext context)
		{
			Client = client;
			Context = context;
		}

		/// <summary>The cloud table client, for managing table storage.</summary>
		public CloudTableClient Client { get; private set; }

		/// <summary>The table service context for interacting with table data.</summary>
		public TableServiceContext Context { get; private set; }
	}
}
