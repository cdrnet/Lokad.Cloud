<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="Lokad.Cloud.Web.Home" %>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>
<%@ Import Namespace="Lokad.Cloud.Azure"%>
<%@ Import Namespace="Lokad.Cloud.Web"%>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Home - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="NavPlaceHolder" runat="server">
	<uc:NavBar runat="server" Selected="Home" />
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1>Home</h1>
	<p>The administration console of <b>Lokad.Cloud</b> provides management
	features for your back-office apps running on Windows Azure.</p>
	
	<asp:GridView ID="AdminsView" runat="server" 
		EmptyDataText="No administrators set in the configuration file." />
	<br />
	<p>List of the users having access to this console.<br /> 
	(use the service configuration file to modify this list).</p>
	
	<h2 class="separator">WebRole cache</h2>
	<div class="warning">
		Purge the cache of the current webrole (other instances unaffected): 
		<asp:Button runat="server" OnClick="ClearCache_Click" Text="Clear" /></div>
	<br />
	
	<h2 class="separator">System status</h2>
	<div class="box">
		<ul>
			<li>Lokad.Cloud Version: <b>
				<asp:Label ID="LokadCloudVersion" runat="server" Text='<%# GlobalSetup.AssemblyVersion %>' /></b>
				<ul>
					<li>Status: <b>
						<asp:Label ID="LokadCloudUpdateStatus" runat="server" Text='<%# GlobalSetup.IsLokadCloudUpToDate.HasValue ? (GlobalSetup.IsLokadCloudUpToDate.Value ? "Up-to-date" : ("New version avilable (" + GlobalSetup.NewLokadCloudVersion + ")")) : "Could not retrieve version info" %>' />
						</b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<asp:HyperLink ID="DownloadLokadCloud" runat="server" Visible='<%# GlobalSetup.IsLokadCloudUpToDate.HasValue && !GlobalSetup.IsLokadCloudUpToDate.Value %>'
							NavigateUrl='<%# GlobalSetup.DownloadUrl %>' Text="Download" ToolTip="Go to the Lokad.Cloud download page (new window)" Target="_blank" />
					</li>
				</ul>
			</li>
			<li>Storage Account Name: <b>
				<asp:Label ID="StorageAccountName" runat="server" Text='<%# GlobalSetup.StorageAccountName %>' /></b>
			</li>
		</ul>
	</div>
</asp:Content>
