<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Workloads.aspx.cs" Inherits="Lokad.Cloud.Web.Workloads" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Queue Workload Viewer - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1 class="separator">Queue Workload Viewer</h1>
	<asp:GridView ID="QueuesView" runat="server" EmptyDataText="No queues in your account" />
</asp:Content>
