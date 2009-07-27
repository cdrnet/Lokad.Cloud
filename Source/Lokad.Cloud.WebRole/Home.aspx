<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="Lokad.Cloud.Web.Home" %>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>
<%@ Import Namespace="Lokad.Cloud.Azure"%>
<%@ Import Namespace="Lokad.Cloud.Web"%>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Home - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="NavPlaceHolder" runat="server">
	<uc:NavBar runat="server" Selected="Home" />
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
	
	<h2 class="separator">WebRole cache</h2>
	<div class="warning">
		Purge the cache of the current webrole (other instances unaffected): 
		<asp:Button runat="server" OnClick="ClearCache_Click" Text="Clear" /></div>
</asp:Content>
