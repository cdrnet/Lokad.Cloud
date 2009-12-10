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

			ServiceList.DataBind();
		}

		IEnumerable<object> GetServices()
		{
			foreach(var blobName in _provider.List(CloudServiceStateName.GetPrefix()))
			{
				var state = _provider.GetBlobOrDelete(blobName);
				if (!state.HasValue)
				{
					continue;
				}

				yield return new
					{
						Name = blobName.ServiceName,
						State = state.Value.ToString()
					};
			}
		}

		protected void ServicesView_OnRowCommand(object sender, GridViewCommandEventArgs e)
		{
			if(e.CommandName == "Toggle")
			{
				int row;
				if(!int.TryParse(e.CommandArgument as string, out row)) return;

				var blobName = new CloudServiceStateName(ServicesView.Rows[row].Cells[1].Text);

				// inverting the service status
				_provider.UpdateIfNotModified(
					blobName,
					s => s.HasValue
						? (s.Value == CloudServiceState.Started ? CloudServiceState.Stopped : CloudServiceState.Started)
						: CloudServiceState.Started);

				ServicesView.DataSource = GetServices();
				ServicesView.DataBind();
			}
		}

		protected void ServiceList_DataBinding(object sender, EventArgs e)
		{
			// Filter out built-in services
			var services = new List<string>();

			foreach(var name in _provider.List(CloudServiceStateName.GetPrefix()))
			{
				// HACK: name of built-in services is hard-coded
				if(name.ServiceName != typeof(Cloud.Services.GarbageCollectorService).FullName &&
					name.ServiceName != typeof(Cloud.Services.DelayedQueueService).FullName &&
					name.ServiceName != typeof(Cloud.Services.MonitoringService).FullName)
				{
					services.Add(name.ServiceName);
				}
			}

			ServiceList.DataSource = services;
		}

		protected void DeleteButton_Click(object sender, EventArgs e)
		{
			Page.Validate("delete");
			if(!Page.IsValid) return;

			var serviceName = ServiceList.SelectedValue;

			var stateBlobName = new CloudServiceStateName(serviceName);

			_provider.DeleteBlob(stateBlobName);

			ServiceList.DataBind();
		}
	}
}
