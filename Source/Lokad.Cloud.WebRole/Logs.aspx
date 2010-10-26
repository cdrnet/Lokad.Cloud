<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Logs.aspx.cs" Inherits="Lokad.Cloud.Web.Logs" %>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Error Logs - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="NavPlaceHolder" runat="server">
	<uc:NavBar runat="server" Selected="Logs" />
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1 class="separator">Error Logs</h1>
	
	<div class="warning">
		Delete log entries older than
		<asp:TextBox ID="WeeksBox" runat="server" Width="25px" Text="4" />
		<asp:RequiredFieldValidator ID="WeeksValidator1" runat="server" Display="Dynamic" ErrorMessage="*" CssClass="resulterror"
			ControlToValidate="WeeksBox" ValidationGroup="del" />
		<asp:RangeValidator ID="WeeksValidator2" runat="server" Display="Dynamic" ErrorMessage="*" CssClass="resulterror"
			ControlToValidate="WeeksBox" Type="Integer" MinimumValue="1" MaximumValue="52" ValidationGroup="del" />
		weeks:
		<asp:Button ID="DeleteButton" runat="server" Text="Delete" ValidationGroup="del"
			OnClick="DeleteButton_Click" /></div>
	<br />
	
	<asp:HiddenField ID="PageIndex" runat="server" Value="0" />
	<asp:RadioButtonList ID="LevelSelector" runat="server" 
        RepeatDirection="Horizontal" AutoPostBack="true" 
        OnSelectedIndexChanged="OnLevelChanged">
        <asp:ListItem>Debug</asp:ListItem>
        <asp:ListItem>Info</asp:ListItem>
        <asp:ListItem>Warn</asp:ListItem>
        <asp:ListItem>Error</asp:ListItem>
    </asp:RadioButtonList>
	<asp:GridView ID="LogsView" runat="server" EmptyDataText="No logs yet." OnDataBinding="LogsView_DataBinding" />
	<center>
		<asp:LinkButton ID="PrevPage" runat="server" Text="&laquo; Prev" OnClick="PrevPage_Click" />
		[<asp:Label ID="CurrentPage" runat="server" Text="1" />]
		<asp:LinkButton ID="NextPage" runat="server" Text="Next &raquo;" OnClick="NextPage_Click" />
	</center>
</asp:Content>
