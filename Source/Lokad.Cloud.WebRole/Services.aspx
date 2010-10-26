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
		OnRowCommand="ServicesView_OnRowCommand"
		OnDataBinding="ServicesView_DataBinding"
		OnRowDataBound="ServicesView_RowDataBound">
		<Columns>
			<asp:ButtonField ButtonType="Link" Text="Toggle" CommandName="Toggle" />
		</Columns>
	</asp:GridView>
	<br />
	
	<p>Delete data for unused services:<br />
		<asp:DropDownList ID="ServiceList" runat="server" OnDataBinding="ServiceList_DataBinding" /><br />
		<asp:Button ID="DeleteButton" runat="server" Text="Delete" OnClick="DeleteButton_Click" />
		<asp:RequiredFieldValidator ID="ServiceListValue" runat="server" ControlToValidate="ServiceList"
			ErrorMessage="You must select a service." CssClass="resulterror" ValidationGroup="delete" />
	</p>
</asp:Content>
