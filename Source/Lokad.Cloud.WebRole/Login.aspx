<%@ Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true"  CodeBehind="Login.aspx.cs" Inherits="Lokad.Cloud.Web.Login" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Login - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1>Login</h1>
	<RP:OpenIdLogin ID="_openIdLogin" runat="server" OnLoggingIn="OpenIdLogin_OnLoggingIn" />
</asp:Content>
