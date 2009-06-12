#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Core
{
	/// <summary>The purpose of the <see cref="MessageWrapper"/> is to gracefully
	/// handle messages that are too large of the queue storage (or messages that happen
	/// to be already stored in the Blob Storage).</summary>
	[Serializable]
	public sealed class MessageWrapper : ISerializable
	{
		public bool IsOverflow { get; set; }

		public object InnerMessage { get; set; }

		public string ContainerName { get; set; }

		public string BlobName { get; set; }

		public MessageWrapper()
		{
		}

		/// <summary>Deserialization constructor.</summary>
		public MessageWrapper(SerializationInfo info, StreamingContext context)
		{
			IsOverflow = info.GetBoolean("io");

			if(IsOverflow)
			{
				ContainerName = info.GetString("cn");
				BlobName = info.GetString("bn");
			}
			else
			{
				var ty = (Type)info.GetValue("ty", typeof(Type));
				InnerMessage = info.GetValue("in", ty);
			}
		}

		/// <summary>Serialization method.</summary>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("vs", "1.0");
			info.AddValue("io", IsOverflow);

			if(IsOverflow)
			{
				info.AddValue("cn", ContainerName);
				info.AddValue("bn", BlobName);
			}
			else
			{
				info.AddValue("ty", InnerMessage.GetType());
				info.AddValue("in", InnerMessage);
			}
		}
	}
}
