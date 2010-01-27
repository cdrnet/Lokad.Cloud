<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Scheduler.aspx.cs" Inherits="Lokad.Cloud.Web.Scheduler" %>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Scheduler - Lokad.Cloud Administration Console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="NavPlaceHolder" runat="server">
	<uc:NavBar runat="server" Selected="Scheduler" />
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1>Scheduler</h1>
	<p>Manage scheduled execution of your services.<br/>
	Current server time: <%= DateTime.UtcNow %></p>
	
	<p><asp:GridView ID="ScheduleView" runat="server" OnDataBinding="ScheduleView_DataBinding" /></p>
	<br />
	<p>Update trigger interval of scheduled service:<br />
		<asp:DropDownList ID="ScheduleList" runat="server" OnDataBinding="ScheduleList_DataBinding" />
		<asp:TextBox ID="NewIntervalBox" runat="server" Text="3600"/> seconds<br />
		<asp:Button ID="UpdateIntervalButton" runat="server" Text="Update" 
			OnClick="UpdateIntervalButton_OnClick"
			ValidationGroup="UpdateInterval" />
		<asp:RangeValidator runat="server" 
			ValidationGroup="UpdateInterval"
			ControlToValidate="NewIntervalBox" Type="Integer"
			ErrorMessage="Value must be between 1 and 10e9." 
			MaximumValue="1000000000" MinimumValue="1"></asp:RangeValidator>
	</p>
	
	<p>Delete data for unused scheduled services:<br />
		<asp:DropDownList ID="ServiceList" runat="server" OnDataBinding="ServiceList_DataBinding" /><br />
		<asp:Button ID="DeleteButton" runat="server" Text="Delete" OnClick="DeleteButton_Click" />
		<asp:RequiredFieldValidator ID="ServiceListValue" runat="server" ControlToValidate="ServiceList"
			ErrorMessage="You must select a service." CssClass="resulterror" ValidationGroup="delete" />
	</p>
	
	<p>Forcibly remove a lease. Note that removing a lease while the lease owner is still running can cause unexpected behavior.<br />
		<asp:DropDownList ID="LeaseList" runat="server" OnDataBinding="LeaseList_DataBinding" /><br />
		<asp:Button ID="ReleaseButton" runat="server" Text="Release" OnClick="ReleaseButton_Click" />
		<asp:RequiredFieldValidator ID="LeaseListValue" runat="server" ControlToValidate="LeaseList"
			ErrorMessage="You must select a service." CssClass="resulterror" ValidationGroup="release" />
	</p>
	
</asp:Content>
