Lokad.Cloud.Diagnostics

Diagnostics for cloud services and their infrastructure

Terms:
* Instrumentation: Extending the codebase which shall be monitored using
  sensors like counters, profilers and loggers
* Statistics: Data sets resulting from instrumentation;
  can then be aggregated by type, time or context, and rendered for user analysis
  
Note: text-based logging is currently handled outside of the diagnostics namespace
by Azure.CloudLogger, although for convinience this class is automatically IoC
registered to its ILog intreface in the DiagosticsModule.

INSTRUMENTATION:

1. CloudService Processing Time Instrumentation

Is integrated automatically at the level of the service scheduler/balancer,
capturing total and user processing time per service.

2. Computing Environment/Partition Instrumentation (System Data, Processing, Memory)

Is run by the scheduled MonitoringService, capturing for every cloud partition the
total and user processing time, handles and threads, memory consumption as well
as the operating system, runtime version and number of CPU cores of the cloud node.

3. Custom Exception Tracking Counters

Is run by the scheduled MonitoringService and captures any exceptions reported
to the default exception counters provided by Lokad.Shared. In addition to the
default counters its also possible to push counter statistics of other,
non-default counters tagged by a context, using the
ExceptionTrackingMonitor.Update method.

4. Custom Execution Profiling Counters

Is run by the scheduled MonitoringService and captures any execution profiles
and counters reported to the default execution counters provided by Lokad.Shared.
In addition to the default counters its also possible to push counter statistics
of other, non-default counters tagged by a context, using the
ExecutionProfilingMonitor.Update method.

STATISTICS:

Although there are plans to support more interesting aggregations (including time,
see issue #78), we currently aggregate all statistics by object name and context.

Statistics can be retrieved:

* From the CloudStatistics management class (recommended, but is expected to change soon)
* From the monitors using their GetStatistics methods
* Directly from the ICloudDiagnosticsRepository repository (not recommended)