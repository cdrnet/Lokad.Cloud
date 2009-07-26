PingPong sample for Lokad.Cloud
(Copyright Lokad (c) 2009)

Author: Joannes Vermorel
Last updated: 2009-07
Contact: support@lokad.com
http://www.lokad.com/

The purpose of this sample is to illustrate the usage of Lokad.Cloud
through a minimalistic application that involve two components:
- a queue service (running on Windows Azure) hosted in Lokad.Cloud
- a client executable that push workload toward the service and retrieve results.

The client pushes items (here 'double' numbers) to the queue named 'ping'. The
queue service consumes the 'ping' queue and outputs the results to the queue
named 'pong'. Finally, the client retrieves the items from 'pong' and display
them.