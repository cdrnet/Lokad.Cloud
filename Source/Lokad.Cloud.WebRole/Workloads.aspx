<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Workloads.aspx.cs" Inherits="Lokad.Cloud.Web.Workloads" %>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Queue Workload Viewer - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="NavPlaceHolder" runat="server">
	<uc:NavBar runat="server" Selected="Workloads" />
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1 class="separator">Queue Workload Viewer</h1>
	<p>This table reports the workload in the various queues of the account.</p>
	<asp:GridView ID="QueuesView" runat="server"
		EmptyDataText="No queues in your account" OnRowCommand="QueuesView_RowCommand">
		<Columns>
			<asp:ButtonField ButtonType="Link" Text="Delete" CommandName="DeleteQueue" />
		</Columns>
	</asp:GridView>
</asp:Content>
