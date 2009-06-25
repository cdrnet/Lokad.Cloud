<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Assemblies.aspx.cs" Inherits="Lokad.Cloud.Web.Assemblies" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Assembly manager - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1>Assembly manager</h1>

	Content of the assembly archive:
	<asp:GridView ID="AssembliesView" runat="server" EmptyDataText="No file in archive yet." />
	
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
</asp:Content>