Lokad.Cloud is an O/C mapper (object to cloud) for Windows Azure.
Copyright (c) Lokad SAS, 2009-2010

Authors: Joannes Vermorel, Christoph Ruegg, Rinat Abdullin

Project page: http://code.google.com/p/lokad-cloud/
Company page: http://www.lokad.com/
Community Forums: http://ask.lokad.com/ 

BIG PICTURE

Lokad.Cloud comes basically as a single assembly named "Lokad.Cloud.Framework.dll".

This assembly is typically referenced in two places:
- in the library containing your cloud services (back-end processing).
- in the client app (eventually a web app) pushing and retrieving work.

Lokad.Cloud depends on
- Lokad.Shared.dll
- Lokad.Stack.dll
which are also open source, see 
http://code.google.com/p/lokad-shared-libraries/


GETTING STARTED

Please refer to the instructions at
http://code.google.com/p/lokad-cloud/wiki/GettingStarted
