<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Assemblies.aspx.cs" Inherits="Lokad.Cloud.Web.Assemblies" %>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Assembly manager - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="NavPlaceHolder" runat="server">
	<uc:NavBar runat="server" Selected="Assemblies" />
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1 class="separator">Assembly manager</h1>

	<p>Content of the assembly archive:</p>
	<asp:GridView ID="AssembliesView" runat="server" EmptyDataText="No file in archive yet." OnDataBinding="AssembliesView_DataBinding" />
	<br />
	<p>Upload a stand-alone .NET assembly (DLL) or a ZIP archive containing multiple DLLs (previous assemblies will be deleted):
	</p>
	<p>
		<asp:FileUpload ID="AssemblyFileUpload" runat="server" />
		<asp:CustomValidator ID="UploadValidator" runat="server" 
			Display="Dynamic" 
			Text="A file must be selected before uploading, and it must either be a DLL assembly or a ZIP archive."
			ValidationGroup="Upload"
			OnServerValidate="UploadValidator_Validate" /> <br />
		<asp:Button runat="server" 
			Text="Upload new assemblies" 
			OnClick="UploadButton_Click"
			ValidationGroup="Upload" />
		<asp:Label ID="UploadSucceededLabel" runat="server" EnableViewState="false" Visible="false"
			Text="Upload succeeded" />
	</p>
	<asp:Panel ID="_assemblyWarningPanel" runat="server" CssClass="warning">
		<asp:Label ID="_invalidAssemblyPackage" runat="server"
			Text="At least one of the configured assemblies is corrupt or incompatible to this version and fails to load. Please check the error logs for more details, verify your package and try to upload again." />
	</asp:Panel>
	
</asp:Content>