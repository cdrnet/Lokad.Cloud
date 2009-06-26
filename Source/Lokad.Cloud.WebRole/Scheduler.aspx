<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Scheduler.aspx.cs" Inherits="Lokad.Cloud.Web.Scheduler" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Scheduler - Lokad.Cloud Administration Console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1>Scheduler</h1>
	<p>Manage scheduled execution of your services.</p>
	<asp:GridView ID="ScheduleView" runat="server" />
</asp:Content>
