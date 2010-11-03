#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Diagnostics.Rsm
{
    [Serializable]
    [DataContract(Name = "message", Namespace = "http://schemas.lokad.com/monitoring/1.0/")]
    public class MonitoringMessageReport
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "updated")]
        public DateTime Updated { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "summary", IsRequired = false)]
        public string Summary { get; set; }

        [DataMember(Name = "tags", IsRequired = false)]
        public string Tags { get; set; }

        [DataMember(Name = "link", IsRequired = false)]
        public string Link { get; set; }
    }
}
