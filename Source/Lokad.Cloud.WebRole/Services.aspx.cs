#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Lokad.Cloud.Management.Api10;

namespace Lokad.Cloud.Web
{
	public partial class Services : System.Web.UI.Page
	{
		readonly ICloudServicesApi _cloudServices = GlobalSetup.Container.Resolve<ICloudServicesApi>();

		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack)
			{
				ServicesView.DataBind();
				ServiceList.DataBind();
			}
		}

		protected void ServicesView_DataBinding(object sender, EventArgs e)
		{
			ServicesView.DataSource = _cloudServices.GetServices()
				.Select(info => new
					{
						Name = info.ServiceName,
						State = info.IsStarted ? "started" : "stopped",
						Info = info
					});
		}

		protected void ServicesView_RowDataBound(object sender, GridViewRowEventArgs e)
		{
			if(e.Row.RowType != DataControlRowType.DataRow)
			{
				return;
			}

			var info = (CloudServiceInfo)DataBinder.Eval(e.Row.DataItem, "Info");
			var stateCell = e.Row.Cells[e.Row.Cells.Count - 1];
			stateCell.CssClass = info.IsStarted ? "statusenabled" : "statusdisabled";
		}

		protected void ServiceList_DataBinding(object sender, EventArgs e)
		{
			ServiceList.DataSource = _cloudServices.GetUserServiceNames();
		}

		protected void ServicesView_OnRowCommand(object sender, GridViewCommandEventArgs e)
		{
			if (e.CommandName == "Toggle")
			{
				int row;
				if (!int.TryParse(e.CommandArgument as string, out row)) return;

				// inverting the service status
				var serviceName = ServicesView.Rows[row].Cells[1].Text;
				_cloudServices.ToggleServiceState(serviceName);

				ServicesView.DataBind();
			}
		}

		protected void DeleteButton_Click(object sender, EventArgs e)
		{
			Page.Validate("delete");
			if (!Page.IsValid)
			{
				return;
			}

			var serviceName = ServiceList.SelectedValue;
			_cloudServices.ResetServiceState(serviceName);

			ServiceList.DataBind();
		}
	}
}
