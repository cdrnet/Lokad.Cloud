<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="Lokad.Cloud.Web.Login" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Login - Lokad.Cloud administration console</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
		<h1>Login</h1>
		
		<RP:OpenIdLogin ID="_openIdLogin" runat="server" OnLoggingIn="OpenIdLogin_LoggingIn"  />
    </div>
    </form>
</body>
</html>
