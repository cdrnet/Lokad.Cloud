Lokad.Cloud is intended as an execution framework for back-end cloud apps.
Copyright (c) Lokad SAS, 2009

Authors: Joannes Vermorel, Rinat Abdullin
Project page: http://code.google.com/p/lokad-cloud/
Company page: http://www.lokad.com/
Forums: http://forums.lokad.com/ 

BIG PICTURE

Lokad.Cloud comes basically as a single assembly named "Lokad.Cloud.Framework.dll".

This assembly is typically referenced in two places:
- in the library containing your cloud services (back-end processing).
- in the client app (eventually a web app) pushing and retrieving work.

Lokad.Cloud.Framework is the sole assembly refered by the client app.
(Lokad.Cloud.Framework embeds the Microsoft StorageClient)

- Lokad.Cloud.WorkerRole is the cloud host.
- Lokad.Cloud.WebRole is the administration console.

GETTING STARTED

The package contains an Azure package "Lokad.Cloud.cspkg" ready to be deployed
through the Windows Azure Developer Portal at https://lx.azure.microsoft.com/

Then, the package contains "ServiceConfiguration.cscfg" the configuration file
that needs to be updated with your own settings (basically credentials for your
storage account).

Finally, you need something to run on top of Lokad.Cloud, 
http://code.google.com/p/lokad-cloud/wiki/GettingStarted



STORAGE SCHEMAS

Lokad.Cloud autogenerates a couple of storage items. All system items get prefixed 
by "lokad-cloud" in order to facilitate identification and eventually removal.

Blob containers part of the framework:

lokad-cloud-assemblies
lokad-cloud-cuids [planned]
lokad-cloud-locks [planned]
lokad-cloud-logs 
lokad-cloud-overflowing-queues
lokad-cloud-services
lokad-cloud-schedule

Queue containers part of the framework:

lokad-cloud-schedule
lokad-cloud-blobsets-map
lokad-cloud-blobsets-reduce
