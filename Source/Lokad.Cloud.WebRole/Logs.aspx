<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Logs.aspx.cs" Inherits="Lokad.Cloud.Web.Logs" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Error Logs - Lokad.Cloud admininistration console</title>
</head>
<body>
    <form runat="server">
    <div>
		<h1>Error Logs</h1>
		<asp:GridView ID="LogsView" runat="server" />
		<p>
			<asp:HyperLink runat="server" NavigateUrl="~/Default.aspx" Text="Home" />
		</p>
    </div>
    </form>
</body>
</html>
