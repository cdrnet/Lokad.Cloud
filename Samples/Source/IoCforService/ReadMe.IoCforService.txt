IoCforService sample for Lokad.Cloud
(Copyright Lokad (c) 2009)

Author: Joannes Vermorel
Last updated: 2009-07
Contact: support@lokad.com
http://www.lokad.com/

The purpose of this sample is to illustrate the usage of Lokad.Cloud
with a IoC setup within the client app.

The sample contains a unique cloud service depending on a provider
that gets registered through an Autofac module. Below, the configuration
file needed for the service.


<?xml version="1.0" encoding="utf-8" ?>
<configuration>
	<configSections>
		<section name="autofac" type="Autofac.Configuration.SectionHandler, Lokad.Stack" requirePermission="false"/>
	</configSections>
	<autofac>
		<modules>
			<module type="IoCforService.MyModule, IoCforService" />
		</modules>
	</autofac>
</configuration>