Lokad.Cloud.ServiceFabric.Runtime

Runtime environment for Cloud Services, including assembly loading and execution scheduling.

Note: No user or client code should need anything in this namespace. In fact it should be
possible to extract this namespace to a separate assembly later, in case we decide that runtime
aspects should no longer be part of the framework since they're completely orthogonal to
designing cloud serivces (intended to run in that runtime).