#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Web
{
	public partial class Workloads : Page
	{
		// TODO: reuse constant, or abstract with management class
		const string FailingMessagesStoreName = "failing-messages";
		const string DataNotAvailableMessage = "Raw data was lost, message not restoreable. Maybe the queue was deleted in the meantime.";
		const string XmlNotAvailableMegssage = "XML representation not available, but message is restoreable.";

		readonly IQueueStorageProvider _provider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();

		protected void Page_Load(object sender, EventArgs e)
		{
			QueuesView.DataSource = GetQueues();
			QueuesView.DataBind();

			var failingMessages = GetFailingMessages().ToArray();
			if(failingMessages.Length > 0)
			{
				FailingMessagesLabel.Visible = true;
			}
			else
			{
				NoFailingMessagesLabel.Visible = true;
			}

			PersistedMessagesRepeater.DataSource = failingMessages;
			PersistedMessagesRepeater.DataBind();
		}

		IEnumerable<object> GetQueues()
		{
			foreach(var queueName in _provider.List(null))
			{
				var inQueueCount = _provider.GetApproximateCount(queueName);
				var latency = _provider.GetApproximateLatency(queueName);

				yield return new
					{
						QueueName = queueName,
						Messages = inQueueCount,
						Latency = latency.Convert(ts => ts.PrettyFormat(), string.Empty),
					};
			}
		}

		IEnumerable<object> GetFailingMessages()
		{
			// TODO: paging or other mechanism to be able to show more than 50 entries

			return _provider.ListPersisted(FailingMessagesStoreName)
				.Take(50)
				.Select(key => _provider.GetPersisted(FailingMessagesStoreName, key))
				.Where(m => m.HasValue)
				.Select(m => m.Value)
				.GroupBy(m => m.QueueName)
				.Select(group => new
					{
						QueueName = group.Key,
						Messages = group.OrderBy(m => m.InsertionTime)
							.Select(m => new
								{
									Inserted = m.InsertionTime.PrettyFormatRelativeToNow(),
									Persisted = m.PersistenceTime.PrettyFormatRelativeToNow(),
									Reason = m.Reason ?? string.Empty,
									Content = FormatContent(m),
									Key = m.Key
								})
							.ToArray()
					}).Cast<object>();
		}

		static string FormatContent(PersistedMessage message)
		{
			if (!message.IsDataAvailable)
			{
				return DataNotAvailableMessage;
			}

			if (!message.DataXml.HasValue)
			{
				return XmlNotAvailableMegssage;
			}

			var sb = new StringBuilder();
			var settings = new XmlWriterSettings
				{
					Indent = true,
					IndentChars = "  ",
					NewLineChars = Environment.NewLine,
					NewLineHandling = NewLineHandling.Replace,
					OmitXmlDeclaration = true
				};

			using (var writer = XmlWriter.Create(sb, settings))
			{
				message.DataXml.Value.WriteTo(writer);
				writer.Flush();
			}

			var encoded = HttpUtility.HtmlEncode(sb.ToString());
			return encoded.Replace(Environment.NewLine, "<br />").Replace("  ", "&nbsp;&nbsp;");
		}
		 
		protected void QueuesView_RowCommand(object sender, GridViewCommandEventArgs e)
		{
			if(e.CommandName == "DeleteQueue")
			{
				int row;
				if(!int.TryParse(e.CommandArgument as string, out row))
				{
					return;
				}

				var queueName = QueuesView.Rows[row].Cells[1].Text;

				_provider.DeleteQueue(queueName);

				QueuesView.DataSource = GetQueues();
				QueuesView.DataBind();
			}
		}

		protected void ChildRepeater_ItemCommand(object sender, RepeaterCommandEventArgs e)
		{
			var key = ((HiddenField) e.Item.FindControl("MessageKey")).Value;

			switch(e.CommandName)
			{
				case "DeleteMessage":
					_provider.DeletePersisted(FailingMessagesStoreName, key);
					e.Item.Visible = false;
					break;
				case "RestoreMessage":
					_provider.RestorePersisted(FailingMessagesStoreName, key);
					e.Item.Visible = false;
					break;
			}
		}

		protected void PersistedMessagesRepeater_ItemCommand(object sender, RepeaterCommandEventArgs e)
		{
			var queueName = ((HiddenField)e.Item.FindControl("QueueName")).Value;
			var messageKeys = new List<string>();

			foreach(var group in (IEnumerable)PersistedMessagesRepeater.DataSource)
			{
				if(queueName != (string)DataBinder.Eval(group, "QueueName"))
				{
					continue;
				}

				foreach(var item in (IEnumerable)DataBinder.Eval(group, "Messages"))
				{
					messageKeys.Add((string)DataBinder.Eval(item, "Key"));
				}

				break;
			}

			switch (e.CommandName)
			{
				case "DeleteQueueMessages":
					foreach(var key in messageKeys)
					{
						_provider.DeletePersisted(FailingMessagesStoreName, key);
					}
					e.Item.Visible = false;
					break;
				case "RestoreQueueMessages":
					foreach (var key in messageKeys)
					{
						_provider.RestorePersisted(FailingMessagesStoreName, key);
					}
					e.Item.Visible = false;
					break;
			}
		}

		protected void Repeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
		{
			e.Item.Visible = true;
		}
	}
}
