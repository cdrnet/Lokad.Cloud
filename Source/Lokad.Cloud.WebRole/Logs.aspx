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
	<asp:GridView ID="LogsView" runat="server" EmptyDataText="No logs yet." />
</asp:Content>
