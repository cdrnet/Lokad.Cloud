<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Services.aspx.cs" Inherits="Lokad.Cloud.Web.Services" %>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Services manager - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="NavPlaceHolder" runat="server">
	<uc:NavBar runat="server" Selected="Services" />
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1 class="separator">Services manager</h1>
	<p>Use the panel below to start or stop services.</p>
	<asp:GridView ID="ServicesView" runat="server" 
		EmptyDataText="No services listed yet."
		OnRowCommand="ServicesView_OnRowCommand" >
		<Columns>
			<asp:ButtonField ButtonType="Link" Text="Toggle" CommandName="Toggle" />
		</Columns>
	</asp:GridView>
</asp:Content>
