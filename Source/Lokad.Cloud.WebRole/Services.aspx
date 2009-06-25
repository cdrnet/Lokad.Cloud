<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Services.aspx.cs" Inherits="Lokad.Cloud.Web.Services" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Services manager - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1 class="separator">Services manager</h1>
	<asp:GridView ID="ServicesView" runat="server" 
		EmptyDataText="No services listed yet."
		OnRowCommand="ServicesView_OnRowCommand" >
		<Columns>
			<asp:ButtonField ButtonType="Link" Text="Toggle" CommandName="Toggle" />
		</Columns>
	</asp:GridView>
</asp:Content>
