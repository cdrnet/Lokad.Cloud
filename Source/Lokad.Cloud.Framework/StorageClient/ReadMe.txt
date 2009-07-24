StorageClient is a cut-and-paste from the July 2009 CTP of the Windows Azure SDK.

StorageClient
--------------

The StorageClient sample implements a library that programmers can use to access the following three
storage services:
	- blob storage service
	- queue service
	- table storage service

Please refer to the Windows Azure SDK documentation if you need help starting Development Storage.


Configuring service endpoints
-----------------------------

Storage endpoints for running the API samples can be configured in the App.config file. 

Here is an example of the configuration:

  <appSettings>

    <add key = "AccountName" value="devstoreaccount1"/>
    <add key = "AccountSharedKey" value="Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="/>   
    
    <!-- Change this to point to the base URIs of the storage services against which this sample is run -->
    <!-- When using production-style URIs within this sample, make sure that the HTTP  endpoints do not contain 
         the account name, as the library code adds the account name internally and there is another configuration
         setting for the account name. An example of a valid production-style URI for this sample's 
         configuration file is:
            <add key="StorageEndpoint" value="http://blob.core.windows.net/"/>
         Note that it is NOT http://***accountname***.blob.core.windows.net/. 
    -->
         
    <add key="BlobStorageEndpoint" value="http://127.0.0.1:10000"/>

    <add key="QueueStorageEndpoint" value="http://127.0.0.1:10001"/>

    <add key="TableStorageEndpoint" value="http://127.0.0.1:10002"/>
    
    <!-- For production servers, set this to false, otherwise to true.
         For example, it must be true for and endpoint such as http://127.0.0.1:10000 and false
         for and endpoint such as http://blob.core.windows.net.
         The implementation of the storage client library
         derives this setting automatically.
         Please set this value only if you are sure the derived value is wrong. 
    -->
    <!--<add key="UsePathStyleUris" value="true" />-->


    <!-- Change this if you want to write to a different container to avoid clashing with others using this
    sample against the same instance of the storage service -->
    <add key="ContainerName" value="storagesamplecontainer"/>
    
  </appSettings>

