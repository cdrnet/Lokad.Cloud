<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Lokad.Cloud.Web._Default" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Home - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1>Home</h1>
	<p>The administration console of <b>Lokad.Cloud</b> provides management
	features for your back-office apps running on Windows Azure.</p>
	
	<asp:GridView ID="AdminsView" runat="server" 
		EmptyDataText="No administrators set in the configuration file." />
	<br />
	<p>List of the users having access to this console.<br /> 
	(use the service configuration file to modify this list).</p>
</asp:Content>
