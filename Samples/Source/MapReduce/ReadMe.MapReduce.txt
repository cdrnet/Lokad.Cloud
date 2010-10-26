MapReduce sample for Lokad.Cloud
(Copyright Lokad (c) 2009)

Author: Joannes Vermorel, Dario Solera
Last updated: 2009-10
Contact: support@lokad.com
http://www.lokad.com/

The purpose of this sample is to illustrate the usage of Lokad.Cloud
through a minimalistic application that involves two components:
- a map/reduce service (running on Windows Azure) hosted in Lokad.Cloud
- a client application that queues work items (image histogram computation).

=== Running the sample ===
1. Build the solution (Build\go.bat).
2. Deploy Lokad.Cloud to Windows Azure (see Build\CloudReady folder),
   making sure your Storage Account settings are correct in the .cscfg file.
3. Prepare a ZIP file containing MapReduceClientLib.dll and MapReduceService.dll
   (see Samples\Source\MapReduce\MapReduceClient\bin\Release folder).
   These files contain the Cloud Service sample implementation that computes
   the histogram of a picture.
3. Login to the administration console, go to the Assemblies tab
4. Upload the ZIP file
5. Set your Storage Account credentials in the client .config file (MapReduceClient.exe.config)
6. Run the client application (MapReduceClient.exe),
   select an image from your hard disk, click the Start button, wait for the result.

=== How it works ===
- The map operation computes the histogram of a bitmap.
- The reduce operation aggregates the histograms of multiple bitmaps.
- The bitmap is split in several slices (see Helper class).
- The MapReduceBlobSet handles units of work.
- The MapReduceService performs the work, using the MapReduceBlobSet class.
- The MapReduceJob class is used by the client as a "controller" for the map/reduce task.

Note: the map/reduce functions use the Histogram class, that must be known to the service.
      For this reason the class lives in its own assembly (MapReduceClientLib) which is
      referenced by MapReduceClient and also uploaded to Lokad.Cloud, as instructed in step (3).