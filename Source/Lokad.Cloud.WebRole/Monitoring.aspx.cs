using System;
using System.Linq;
using System.Web.Caching;
using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.Web
{
	public partial class Monitoring : System.Web.UI.Page
	{
		readonly ServiceMonitor _services = (ServiceMonitor)GlobalSetup.Container.Resolve<IServiceMonitor>();
		readonly PartitionMonitor _partitions = new PartitionMonitor(GlobalSetup.Container.Resolve<IBlobStorageProvider>());

		protected void Page_Load(object sender, EventArgs e)
		{
			PartitionView.DataSource = Cached(() => _partitions.GetStatistics().ToList(), "lokad-cloud-diag-partitions");
			PartitionView.DataBind();

			ServiceView.DataSource = Cached(() => _services.GetStatistics().ToList(), "lokad-cloud-diag-services");
			ServiceView.DataBind();
		}

		T Cached<T>(Func<T> f, string key)
			where T : class
		{
			T value = Cache[key] as T;
			if (value == null)
			{
				Cache.Add(
					key,
					value = f(),
					null,
					DateTime.Now + TimeSpan.FromMinutes(5),
					Cache.NoSlidingExpiration,
					CacheItemPriority.Normal,
					null);
			}

			return value;
		}
	}
}
