IoCforService sample for Lokad.Cloud
(Copyright Lokad (c) 2009)

Author: Dario Solera
Last updated: 2009-10
Contact: support@lokad.com
http://www.lokad.com/

The purpose of this sample is to illustrate how to test applications 
based on Lokad.Cloud and in particular using storage providers.

The sample includes the following classes, which implement 
scenarios requiring extensive testing:
- BlobBackupTool: a class that allows to backup blobs
- EmailDispatcher: a class that sends email messages using a queue

This sample makes use of the following Lokad.Cloud concepts:
- Strongly-typed containers names (BlobBackupTool)
- Temporary containers names (BlobBackupTool)
- Automatic management of queue messages (EmailDispatcher)