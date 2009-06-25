<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Lokad.Cloud.Web._Default" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Home - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1>Home</h1>
	<p>The administration console of Lokad.Cloud provide management
	features for your back-office processes running on Windows Azure.</p>
	
	<p>List of the users having access to this console (use the
	service configuration file to change this list).</p>
	<asp:GridView ID="AdminsView" runat="server" 
		EmptyDataText="No administrators set in the configuration file." />
</asp:Content>
