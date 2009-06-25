#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using Lokad.Cloud.Core;
using Lokad.Cloud.Framework;

// TODO: blobs are sequentially enumerated, performance issue
// if there are more than a few dozen services

namespace Lokad.Cloud.Web
{
	public partial class Services : System.Web.UI.Page
	{
		readonly IBlobStorageProvider _provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

		protected void Page_Load(object sender, EventArgs e)
		{
			ServicesView.DataSource = GetServices();
			ServicesView.DataBind();
		}

		IEnumerable<object> GetServices()
		{
			var cn = CloudService.ServiceAdministrationContainer;
			var prefix = CloudService.StatePrefix;

			foreach(var blobName in _provider.List(cn, prefix))
			{
				var state = _provider.GetBlob<CloudServiceState?>(cn, blobName);
				yield return new
					{
						Name = blobName.Substring(prefix.Length + 1), // discarding the prefix
						State = state.ToString()
					};
			}
		}
	}
}
