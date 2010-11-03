#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Diagnostics.Rsm
{
    [Serializable]
    [DataContract(Name = "indicator", Namespace = "http://schemas.lokad.com/monitoring/1.0/")]
    public class MonitoringIndicatorReport
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "started", IsRequired = false)]
        public DateTime Started { get; set; }

        [DataMember(Name = "updated", IsRequired = false)]
        public DateTime Updated { get; set; }

        [DataMember(Name = "instance", IsRequired = false)]
        public string Instance { get; set; }

        [DataMember(Name = "value")]
        public string Value { get; set; }

        [DataMember(Name = "tags", IsRequired = false)]
        public string Tags { get; set; }

        [DataMember(Name = "link", IsRequired = false)]
        public string Link { get; set; }
    }
}
