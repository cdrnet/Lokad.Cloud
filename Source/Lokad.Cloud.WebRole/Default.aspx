<%@ Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true"  CodeBehind="Default.aspx.cs" Inherits="Lokad.Cloud.Web.Login" %>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Login - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="NavPlaceHolder" runat="server">
	<uc:NavBar runat="server" />
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1 class="separator">Login</h1>
	<RP:OpenIdLogin ID="_openIdLogin" runat="server" OnLoggingIn="OpenIdLogin_OnLoggingIn" />
</asp:Content>
