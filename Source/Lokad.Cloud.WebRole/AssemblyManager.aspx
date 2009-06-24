<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AssemblyManager.aspx.cs" Inherits="Lokad.Cloud.Web.Dashboard" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Assembly manager - Lokad.Cloud administration console</title>
</head>
<body>
    <form runat="server">
    <div>
		<h1>Assembly manager</h1>
		
		Content of the assembly archive:
		<asp:GridView ID="ArchiveView" runat="server" />
		
		<p>Upload a new assembly archive:
		</p>
		<p>
			<asp:FileUpload ID="AssemblyFileUpload" runat="server" />
			<asp:CustomValidator ID="UploadValidator" runat="server" 
				Display="Dynamic" 
				Text="A file must be selected before uploading, and it must be a ZIP archive."
				ValidationGroup="Upload"
				OnServerValidate="UploadValidator_Validate" /> <br />
			<asp:Button runat="server" 
				Text="Upload new assemblies" 
				OnClick="UploadButton_Click"
				ValidationGroup="Upload" />
			<asp:Label ID="UploadSucceededLabel" runat="server" EnableViewState="false" Visible="false"
				Text="Upload succeeded" />
		</p>
		<p>
			<asp:HyperLink runat="server" NavigateUrl="~/Default.aspx" Text="Home" />
		</p>
    </div>
    </form>
</body>
</html>
