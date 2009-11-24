Lokad.Cloud is intended as an execution framework for back-end cloud apps.
Copyright (c) Lokad SAS, 2009

Authors: Joannes Vermorel, Rinat Abdullin, Dario Solera, Christoph Ruegg
Project page: http://code.google.com/p/lokad-cloud/
Company page: http://www.lokad.com/
Forums: http://forums.lokad.com/ 

BIG PICTURE

Lokad.Cloud comes basically as a single assembly named "Lokad.Cloud.Framework.dll".

This assembly is typically referenced in two places:
- in the library containing your cloud services (back-end processing).
- in the client app (eventually a web app) pushing and retrieving work.

Lokad.Cloud.Framework is the sole assembly refered by the client app and it is
based on the Nov.09 version of Windows Azure Tools, available here:
http://www.microsoft.com/downloads/details.aspx?FamilyID=6967ff37-813e-47c7-b987-889124b43abd&displaylang=en

Key roles:
- Lokad.Cloud.WorkerRole is the cloud host.
- Lokad.Cloud.WebRole is the administration console.

GETTING STARTED

The package contains an Azure package "Lokad.Cloud.cspkg" ready to be deployed
through the Windows Azure Developer Portal at https://windows.azure.com/

Then, the package contains "ServiceConfiguration.cscfg" the configuration file
that needs to be updated with your own settings (basically credentials for your
storage account).

Finally, you need something to run on top of Lokad.Cloud, 
http://code.google.com/p/lokad-cloud/wiki/GettingStarted
