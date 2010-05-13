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
    <asp:ScriptManager ID="MainScriptManager" runat="server" />
    <h1 class="separator">Cloud Services</h1>
    <p>This table represents the cloud services that are or have been running, how many times they were scheduled for execution and their CPU usage while executing.</p>
    <asp:UpdatePanel ID="ServicePanel" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <asp:RadioButtonList ID="ServiceSelector" runat="server"
                RepeatDirection="Horizontal" AutoPostBack="true">
                <asp:ListItem Selected="True">Today</asp:ListItem>
                <asp:ListItem>Yesterday</asp:ListItem>
                <asp:ListItem>This Month</asp:ListItem>
                <asp:ListItem>Last Month</asp:ListItem>
            </asp:RadioButtonList>
            <asp:GridView ID="ServiceView" runat="server"
                EmptyDataText="No services have been monitored yet." AutoGenerateColumns="True" 
                OnDataBinding="ServiceView_DataBinding" />
        </ContentTemplate>
    </asp:UpdatePanel>
    <br /><br />
    <h1 class="separator">Execution Profiling</h1>
    <p>This table shows the resulting timing and counting statistics of selectively instrumented code blocks, aggregated by service or subsystem.</p>
    <asp:UpdatePanel ID="ProfilesPanel" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <asp:RadioButtonList ID="ProfilesSelector" runat="server"
                RepeatDirection="Horizontal" AutoPostBack="true">
                <asp:ListItem Selected="True">Today</asp:ListItem>
                <asp:ListItem>Yesterday</asp:ListItem>
                <asp:ListItem>This Month</asp:ListItem>
                <asp:ListItem>Last Month</asp:ListItem>
            </asp:RadioButtonList>
            <asp:GridView ID="ProfilesView" runat="server"
                EmptyDataText="No execution profiles are available yet." AutoGenerateColumns="True" 
                OnDataBinding="ProfilesView_DataBinding" />
        </ContentTemplate>
    </asp:UpdatePanel>
    <br /><br />
    <h1 class="separator">Cloud Workers</h1>
	<p>Cloud partitions where the services are and have been running.</p>
    <asp:UpdatePanel ID="PartitionPanel" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <asp:RadioButtonList ID="PartitionSelector" runat="server"
                RepeatDirection="Horizontal" AutoPostBack="true">
                <asp:ListItem Selected="True">Today</asp:ListItem>
                <asp:ListItem>Yesterday</asp:ListItem>
                <asp:ListItem>This Month</asp:ListItem>
                <asp:ListItem>Last Month</asp:ListItem>
            </asp:RadioButtonList>
	        <asp:GridView ID="PartitionView" runat="server" 
                EmptyDataText="No workers have been monitored yet." AutoGenerateColumns="True" 
                OnDataBinding="PartitionView_DataBinding" />
        </ContentTemplate>
    </asp:UpdatePanel>
    
</asp:Content>
