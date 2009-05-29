Lokad.Cloud is intended as an infrastructure layer for cloud apps.

OVERALL DESIGN

- Lokad.Cloud.Framework is the sole assembly refered by the client app.
(ideally Lokad.Cloud.Framework does not even reference the StorageClient)

- Lokad.Cloud.Core contains the actual implementation.
(it's Lokad.Cloud.Core that interacts with the StorageClient)

- Lokad.Cloud.WorkerRole is the cloud host.

STORAGE SCHEMAS

Blob containers part of the framework:

lokad-cloud-locks
lokad-cloud-queues-metadata
lokad-cloud-queues-overflow

Queue containers part of the framework:

lokad-cloud-schedule

Table contains part of the framework:

lokad-cloud-logs

TECHNICALITIES

- overflowing queue items should be put into blob using the date of the date as prefix.
Though this prefix, it becomes easy to garbage collect those items 7 days afterward
if the message hasn't been processed.

- the notion of "service priority" is still pretty much undefined. Not sure how the 
priority should be defined (maybe as a the relevative weight of amount of attempts to 
process corresponding messages).

- timeouting items should not be bluntly discarded, instead they should be put into
a dedicated persistent storage (maybe a HashSet) for later processing and/or investigation.