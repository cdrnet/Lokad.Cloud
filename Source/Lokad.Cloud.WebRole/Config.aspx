<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Config.aspx.cs" 
	Inherits="Lokad.Cloud.Web.Config" ValidateRequest="false" %>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>
<%@ Import Namespace="Lokad.Cloud.Web"%>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Configuration - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="NavPlaceHolder" runat="server">
	<uc:NavBar runat="server" Selected="Config" />
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1>Configuration</h1>
	<p>Local configuration settings for the client app.</p>
	<asp:TextBox ID="ConfigurationBox" runat="server" TextMode="MultiLine" Columns="80" Rows="20" />
	<asp:Button ID="SaveConfigButton" runat="server" Text="Save" OnClick="SaveConfigButton_OnClick" />
	<asp:Label ID="SavedLabel" runat="server" Text="Configuration saved." Visible="false" EnableViewState="false" />
</asp:Content>
