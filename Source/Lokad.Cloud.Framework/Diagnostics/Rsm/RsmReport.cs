#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Lokad.Cloud.Diagnostics.Rsm
{
    /// <remarks></remarks>
    [DataContract(Name = "rsm", Namespace = "http://schemas.lokad.com/monitoring/1.0/")]
    public class RsmReport
    {
        /// <remarks></remarks>
        [DataMember(Name = "messages", IsRequired = false)]
        public IList<MonitoringMessageReport> Messages { get; set; }

        /// <remarks></remarks>
        [DataMember(Name = "indicators", IsRequired = false)]
        public IList<MonitoringIndicatorReport> Indicators { get; set; }

        /// <remarks></remarks>
        public RsmReport()
        {
            Messages = new List<MonitoringMessageReport>();
            Indicators = new List<MonitoringIndicatorReport>();
        }

        /// <summary>Returns the XML ready to be returned by the endpoint.</summary>
        public override string ToString()
        {
            var serializer = new DataContractSerializer(typeof(RsmReport));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, this);
                stream.Position = 0;
                var enc = new UTF8Encoding();
                return enc.GetString(stream.ToArray());
            }
        }

        /// <summary>Helper methods to concatenate the tags.</summary>
        public static string GetTags(params string[] tags)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < tags.Length; i++)
            {
                if (string.IsNullOrEmpty(tags[i])) continue;

                builder.Append(tags[i]);
                if (i < tags.Length - 1) builder.Append(" ");
            }
            return builder.ToString();
        }
    }
}
