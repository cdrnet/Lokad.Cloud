using System;
using System.Linq;
using System.Web;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Diagnostics.Rsm;

namespace Lokad.Cloud.Web
{
    /// <summary>Really Simple Monitoring endpoint.</summary>
    /// <remarks>This class grabs data to be pushed through the monitoring endpoint.</remarks>
    public class RsmHttpHandler : IHttpHandler
    {
        readonly CloudLogger _logger = (CloudLogger)GlobalSetup.Container.Resolve<ILog>();

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/xml";

            var query = HttpContext.Current.Request.Url.Query;
            if (!query.Contains(CloudEnvironment.GetConfigurationSetting("MonitoringApiKey").Value))
            {
                context.Response.StatusCode = 403; // access forbidden
                context.Response.Write("You do not have access to the monitoring endpoint.");
                return;
            }

            try
            {
                var doc = new RsmReport
                    {
                        Messages = _logger.GetPagedLogs(0, 20, LogLevel.Warn)
                            .Select(entry => new MonitoringMessageReport
                                {
                                    Id = entry.DateTime.ToString("yyyy-MM-ddTHH-mm-ss-ffff"),
                                    Updated = entry.DateTime,
                                    Title = entry.Message,
                                    Summary = entry.Error,
                                    Tags = RsmReport.GetTags("log", entry.Level)
                                })
                            .ToList()
                    };

                context.Response.Write(doc.ToString());
            }
            catch (Exception ex)
            {
                // helper to facilitate troubleshooting the endpoint if needed
                context.Response.Write(ex.ToString());
            }
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}