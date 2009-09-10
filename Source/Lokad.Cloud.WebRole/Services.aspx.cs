#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using Lokad.Cloud;

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
			var cn = CloudService.ServiceStateContainer;
			var prefix = CloudService.ServiceStatePrefix;

			foreach(var blobName in _provider.List(cn, prefix))
			{
				var state = _provider.GetBlobOrDelete<CloudServiceState?>(cn, blobName);
				yield return new
					{
						Name = blobName.Substring(prefix.Length + 1), // discarding the prefix
						State = state.ToString()
					};
			}
		}

		protected void ServicesView_OnRowCommand(object sender, GridViewCommandEventArgs e)
		{
			var cn = CloudService.ServiceStateContainer;
			var prefix = CloudService.ServiceStatePrefix;

			if(e.CommandName == "Toggle")
			{
				var row = -1;
				int.TryParse(e.CommandArgument as string, out row);

				var suffix = ServicesView.Rows[row].Cells[1].Text;
				var bn = prefix + "/" + suffix;

				// inverting the service status
				_provider.UpdateIfNotModified<CloudServiceState?>(cn, bn, 
					s => s.HasValue ? 
						(s.Value == CloudServiceState.Started ? CloudServiceState.Stopped : CloudServiceState.Started) :
						CloudServiceState.Started);

				ServicesView.DataSource = GetServices();
				ServicesView.DataBind();
			}
		}
	}
}
