<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="Lokad.Cloud.Web.Dashboard" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Dashboard - Lokad.Cloud administration console</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
		<h1>Dashboard</h1>
		
		Content of the assembly archive:
		<asp:GridView ID="ArchiveView" runat="server" />
		
		<asp:HyperLink ID="HomeLink" runat="server" NavigateUrl="~/Default.aspx" Text="Home" />
    </div>
    </form>
</body>
</html>
