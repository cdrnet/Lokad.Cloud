﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Lokad.Cloud.Management;

namespace Lokad.Cloud.Web
{
	public partial class Services : System.Web.UI.Page
	{
		readonly CloudServices _cloudServices = GlobalSetup.Container.Resolve<CloudServices>();

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
						State = info.State.ToString(),
						Info = info
					});
		}

		protected void ServicesView_RowDataBound(object sender, GridViewRowEventArgs e)
		{
			if(e.Row.RowType != DataControlRowType.DataRow)
			{
				return;
			}

			var info = (ServiceInfo)DataBinder.Eval(e.Row.DataItem, "Info");
			var stateCell = e.Row.Cells[e.Row.Cells.Count - 1];
			switch (info.State)
			{
				case CloudServiceState.Started:
					stateCell.CssClass = "statusenabled";
					break;
				case CloudServiceState.Stopped:
					stateCell.CssClass = "statusdisabled";
					break;
			}
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
			_cloudServices.RemoveServiceState(serviceName);

			ServiceList.DataBind();
		}
	}
}
