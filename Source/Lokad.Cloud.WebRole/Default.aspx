<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Lokad.Cloud.Web._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Home - Lokad.Cloud administration console</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
		<h1>Home</h1>
		<asp:LoginName ID="_LoginName" runat="server" /> (<asp:LoginStatus ID="_LoginStatus" runat="server" />)
		<p>
			<asp:HyperLink ID="DashboardLink" runat="server" NavigateUrl="~/Dashboard.aspx" Text="Dashboard" />
		</p>
    </div>
    </form>
</body>
</html>
