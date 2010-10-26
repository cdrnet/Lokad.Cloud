<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="Lokad.Cloud.Web.Home" %>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>

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
		<asp:Button runat="server" OnClick="ClearCache_Click" Text="Clear" />
	</div>
	<br />
	
	<h2 class="separator">System status</h2>
	<div class="box">
		<ul>
			<li>Lokad.Cloud Version: <b>
				<asp:Label ID="LokadCloudVersionLabel" runat="server" /></b>
				<ul>
					<li>Status: <b>
						<asp:Label ID="LokadCloudUpdateStatusLabel" runat="server" />
						</b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<asp:HyperLink ID="DownloadLokadCloudLink" runat="server"
							Text="Download" ToolTip="Go to the Lokad.Cloud download page (new window)" Target="_blank" />
					</li>
				</ul>
			</li>
			<li>Current Worker Instances: <b>
				<asp:Label ID="AzureWorkerInstancesLabel" runat="server" /></b><br />
			</li>
		</ul>
	</div>
	
	<asp:Panel ID="DeploymentStatusPanel" Visible="false" runat="server" CssClass="warning">
        <b>Azure Deployment update is in progress</b>
	</asp:Panel>
	
	<asp:Panel ID="WorkerInstancesPanel" Visible="false" runat="server" CssClass="warning">
        Update to
        <asp:TextBox ID="WorkerInstancesBox" runat="server" Width="25px" Text="4" />
        <asp:RequiredFieldValidator ID="WorkerInstancesValidator1" runat="server" Display="Dynamic" ErrorMessage="*" CssClass="resulterror"
            ControlToValidate="WorkerInstancesBox" ValidationGroup="set" />
        <asp:RangeValidator ID="WorkerInstancesValidator2" runat="server" Display="Dynamic" ErrorMessage="*" CssClass="resulterror"
            ControlToValidate="WorkerInstancesBox" Type="Integer" MinimumValue="1" MaximumValue="200" ValidationGroup="set" />
        Worker Instances:
        <asp:Button ID="SetWorkerInstancesButton" runat="server" Text="Request" ValidationGroup="set"
            OnClick="SetWorkerInstancesButton_Click" />
	</asp:Panel>
	
	<asp:Panel ID="_managementWarningPanel" Visible="false" runat="server" CssClass="warning">
	    <b>Azure Service Management is disabled</b><br />
	    Communicating with the Azure Management API failed:
		<asp:Label ID="_managementConfigurationMissing" Visible="false" runat="server"
			Text="The subscription id or certificate thumbprint configuration is missing, please verify the deployment configuration." />
		<asp:Label ID="_managementCertificateMissing" Visible="false" runat="server"
			Text="Failed to load the certificate, please verify the deployment configuration and the certificate in the Azure portal." />
		<asp:Label ID="_managementAuthenticationFailed" Visible="false" runat="server"
			Text="Authentication failed. The subscription id or certificate thumbprint configuration might be wrong, please verify the deployment configuration and the certificate in the Azure portal." />
		<asp:Label ID="_managementDeploymentNotFound" Visible="false" runat="server"
			Text="This running deployment does not match the configured deployment, please verify the deployment configuration and the certificate in the Azure portal. This is the expected behavior when running in the local development fabric." />
		<br />
	</asp:Panel>
	<br />
	
	<h2 class="separator">Azure Deployment</h2>
	<div class="box">
		<ul>
		    <li>Subscription: <b>
				<asp:Label ID="SubscriptionLabel" runat="server" /></b>
			</li>
			<li>Deployment: <b>
				<asp:Label ID="DeploymentLabel" runat="server" /></b>
			</li>
			<li>Hosted Service: <b>
				<asp:Label ID="HostedServiceLabel" runat="server" /></b>
			</li>
			<li>Storage Account: <b>
				<asp:Label ID="StorageAccountLabel" runat="server" /></b>
			</li>
		</ul>
	</div>
	<br />
	
	<h2 class="separator">Azure Management</h2>
	<div class="box">
		<ul>
		    <li>Certificate: <b>
				<asp:Label ID="CertificatesLabel" runat="server" /></b>
				<ul>
				    <li>Thumbprint: <asp:Label ID="CertificateThumbprintLabel" runat="server" /></li>
				</ul>
			</li>
		</ul>
	</div>
	
</asp:Content>
