#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

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
			foreach(var blobName in _provider.List(CloudServiceStateName.GetPrefix()))
			{
				var state = _provider.GetBlobOrDelete<CloudServiceState?>(blobName);

				if (!state.HasValue) continue;

				yield return new
					{
						Name = blobName.ServiceName,
						State = state.ToString()
					};
			}
		}

		protected void ServicesView_OnRowCommand(object sender, GridViewCommandEventArgs e)
		{
			if(e.CommandName == "Toggle")
			{
				var row = -1;
				int.TryParse(e.CommandArgument as string, out row);

				var blobName = new CloudServiceStateName(ServicesView.Rows[row].Cells[1].Text);

				// inverting the service status
				_provider.UpdateIfNotModified<CloudServiceState?>(blobName, 
					s => s.HasValue ? 
						(s.Value == CloudServiceState.Started ? CloudServiceState.Stopped : CloudServiceState.Started) :
						CloudServiceState.Started);

				ServicesView.DataSource = GetServices();
				ServicesView.DataBind();
			}
		}
	}
}
