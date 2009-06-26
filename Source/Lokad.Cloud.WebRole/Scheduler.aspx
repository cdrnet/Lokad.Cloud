<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Scheduler.aspx.cs" Inherits="Lokad.Cloud.Web.Scheduler" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Scheduler - Lokad.Cloud Administration Console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1>Scheduler</h1>
	<p>Manage scheduled execution of your services.</p>
	
	<p><asp:GridView ID="ScheduleView" runat="server" /></p>
	<br />
	<p>
		Update trigger interval of scheduled service:<br />
		<asp:DropDownList ID="ScheduleList" runat="server" />
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
	
</asp:Content>
