<%@ Page Language="C#" MasterPageFile="~/Nav.Master" AutoEventWireup="true" CodeBehind="Workloads.aspx.cs" Inherits="Lokad.Cloud.Web.Workloads" %>
<%@ Register Src="NavBar.ascx" TagName="NavBar" TagPrefix="uc" %>

<asp:Content ContentPlaceHolderID="HeadPlaceHolder" runat="server">
	<title>Queue Workload - Lokad.Cloud administration console</title>
</asp:Content>

<asp:Content ContentPlaceHolderID="NavPlaceHolder" runat="server">
	<uc:NavBar runat="server" Selected="Workloads" />
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyPlaceHolder" runat="server">
	<h1 class="separator">Queue Workload</h1>
	<p>This table reports the workload in the various queues of the account.</p>
	<asp:GridView ID="QueuesView" runat="server" EnableViewState="false"
		EmptyDataText="No queues in your account" OnRowCommand="QueuesView_RowCommand">
		<Columns>
			<asp:ButtonField ButtonType="Link" Text="Delete" CommandName="DeleteQueue" />
		</Columns>
	</asp:GridView>
	<br />
	
	<h2 class="separator">Failing Messages</h2>
	<p>Messages which fail repeatedly are persisted and removed from the queue in order to keep it healthy.
	<asp:Label ID="FailingMessagesLabel" Visible="false" runat="server" EnableViewState="false">The following messages have been considered as failing. Note that persisted messages may become unrestorable if their originating queue is deleted. No more than 50 messsages are shown at a time.</asp:Label>
	<asp:Label ID="NoFailingMessagesLabel" Visible="false" runat="server" EnableViewState="false">No message has been considered as failing so far.</asp:Label></p>
	
	<asp:Repeater ID="PersistedMessagesRepeater" runat="server" EnableViewState="false" OnItemCommand="PersistedMessagesRepeater_ItemCommand" OnItemDataBound="Repeater_ItemDataBound">
		<ItemTemplate>
			<asp:HiddenField ID="QueueName" runat="server" Value='<%# Eval("QueueName") %>' />
			<h3>Queue <%# Eval("QueueName") %>: <asp:LinkButton runat="server" CommandName="RestoreQueueMessages">Restore All</asp:LinkButton> 
					<asp:LinkButton runat="server" CommandName="DeleteQueueMessages">Delete All</asp:LinkButton></h3>
			<asp:Repeater runat="server" EnableViewState="false" OnItemCommand="ChildRepeater_ItemCommand" OnItemDataBound="Repeater_ItemDataBound"
				DataSource='<%# DataBinder.Eval(Container.DataItem, "Messages") %>'>
				<ItemTemplate>
					<asp:HiddenField ID="MessageKey" runat="server" Value='<%# Eval("Key") %>' />
					Inserted <%# Eval("Inserted") %> and removed <%# Eval("Persisted") %>: 
					<asp:LinkButton runat="server" CommandName="RestoreMessage">Restore</asp:LinkButton> 
					<asp:LinkButton runat="server" CommandName="DeleteMessage">Delete</asp:LinkButton> 
					<asp:LinkButton runat="server" CommandName="EditMessage" Enabled="false">Edit</asp:LinkButton>
					<div class="box">
						<%# Eval("Content") %>
					</div>
				</ItemTemplate>
			</asp:Repeater>
		</ItemTemplate>
	</asp:Repeater>

</asp:Content>
