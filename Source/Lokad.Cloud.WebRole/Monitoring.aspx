<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Monitoring.aspx.cs" Inherits="Lokad.Cloud.Web.Monitoring" EnableViewState="false" %>
<%@ Import Namespace="Lokad.Cloud.Diagnostics"%>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>System Monitoring - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="NavPlaceHolder" runat="server">
	<uc:NavBar ID="NavBar1" runat="server" Selected="Monitoring" />
</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1 class="separator">Cloud Worker Statistics</h1>
	<p>Cloud partitions where the services are and have been running.</p>
	<asp:GridView ID="PartitionView" runat="server" EmptyDataText="No workers have been monitored yet." AutoGenerateColumns="False">
        <Columns>
            <asp:BoundField DataField="PartitionKey" HeaderText="Partition" />
            <asp:BoundField DataField="Runtime" HeaderText="Runtime" />
            <asp:BoundField DataField="ProcessorCount" HeaderText="Cores" />
            <asp:BoundField DataField="ThreadCount" HeaderText="Threads" />
            <asp:BoundField DataField="TotalProcessorTime" HeaderText="Processing Time" />
            <asp:BoundField DataField="MemoryPrivateSize" HeaderText="Memory" />
        </Columns>
    </asp:GridView>
    <br /><br />
    <h1 class="separator">Cloud Service Statistics</h1>
	<p>This table represents the cloud services that are or have been running.</p>
    <asp:GridView ID="ServiceView" runat="server" EmptyDataText="No services have been monitored yet." AutoGenerateColumns="False">
        <Columns>
            <asp:BoundField DataField="Name" HeaderText="Name" />
            <asp:BoundField DataField="TotalProcessorTime" HeaderText="Processing Time" />
            <asp:BoundField DataField="FirstStartTime" HeaderText="Since" />
        </Columns>
    </asp:GridView>
    <br /><br />
    <h1 class="separator">Execution Profiling Statistics</h1>
    <p>This table shows the resulting timing and counting statistics of selectively instrumented code blocks, aggregated by service or subsystem.</p>
    <asp:GridView ID="ExecutionProfilesView" runat="server" EmptyDataText="No execution profiles are available yet." AutoGenerateColumns="True" />
    <br /><br />
    <h1 class="separator">Exception Tracking Statistics</h1>
    <p>This table shows the most common exceptions and how many times they were tracked, aggregated by service or subsystem.</p>
    <asp:GridView ID="TrackedExceptionsView" runat="server" EmptyDataText="No execution profiles are available yet." AutoGenerateColumns="True" />

</asp:Content>
