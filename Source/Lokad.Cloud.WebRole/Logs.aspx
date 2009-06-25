<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Logs.aspx.cs" Inherits="Lokad.Cloud.Web.Logs" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Error Logs - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1>Error Logs</h1>
	<asp:GridView ID="LogsView" runat="server" />
</asp:Content>
