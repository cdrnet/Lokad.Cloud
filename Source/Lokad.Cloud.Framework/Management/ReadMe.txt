Lokad.Cloud.Management

Infrastructure to manage the cloud system.

Use these classes instead of accessing internal data structures directly,
so internal logic, data structure and persistence strategies don't spread
out into other ares (i.e. the management WebRole). This abstraction
also makes the management logic more testable.

This is the place where we later may provide a management API,
so Lokad.Cloud systems can be manageded from external applications
without having to run and pay for an extra WebRole all the time.

Note that these classes are only intended for management (by a user),
the service runtime environment should therefore not reference or
use anything in this namespace at all.

TODO:
* Further refactoring (some of the classes will likely end up in ServiceFabric)
* Unit Testing
* API