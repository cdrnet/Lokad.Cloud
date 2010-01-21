//#region Copyright (c) Lokad 2009
//// This code is released under the terms of the new BSD licence.
//// URL: http://www.lokad.com/
//#endregion

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using Lokad.Threading;
//using Lokad.Cloud.Azure.Test;
//using NUnit.Framework;

//namespace Lokad.Cloud.Test
//{
//    [TestFixture]
//    public class ScheduledServiceTests
//    {
//        [Test]
//        public void OnlyOneWorkerRunScheduledServiceAtOnceTest()
//        {
//            var container = GlobalSetup.Container;

//            var counterName = TemporaryBlobName.GetNew(DateTimeOffset.Now + new TimeSpan(01, 01, 01, 01));
//            var counter = new BlobCounter(container.Resolve<IBlobStorageProvider>(), counterName);
//            counter.Reset(0);

//            //Service is Started
//            var stateBlobName = new CloudServiceStateName(new MockScheduledService().Name);
//            var storage = container.Resolve<IBlobStorageProvider>();
//            var cloudServiceState = CloudServiceState.Started;
//            storage.PutBlob(stateBlobName, cloudServiceState);

//            //ScheduleService is not busy
//            var stateName = new ScheduledServiceStateName(new MockScheduledService().Name);
//            var scheduledServiceState = new ScheduledServiceState()
//                {IsBusy = false, LastExecuted = DateTimeOffset.Now - new TimeSpan(01, 01, 01, 01), TriggerInterval = 7.Seconds()};
//            storage.PutBlob(stateName, scheduledServiceState);

//            const int threadsCount = 16;
//            var results = Range.Array(threadsCount).SelectInParallel(e =>
//                {
//                    var mockScheduledService = new MockScheduledService()
//                        {
//                            Providers = container.Resolve<CloudInfrastructureProviders>(), 
//                            CounterBlobName = counterName
//                        };

//                    for (int i = 0 ; i < 10;i++)
//                    {
//                        mockScheduledService.Start();
//                        Thread.Sleep(1523); //random delay
//                    }
//                    return true;
//                });

//            Assert.AreEqual(10, counter.GetValue(), "We should have incremented this counter only 10 times.");

//        }
//    }

//    [ScheduledServiceSettings(AutoStart = false, TriggerInterval = 7, // 7s
//        Description = "Used to ensure ScheduledService are run on only one worker at once.")]
//    class MockScheduledService : ScheduledService
//    {
//        public TemporaryBlobName CounterBlobName { get; set; }

//        protected override void StartOnSchedule()
//        {
//            var counter = new BlobCounter(BlobStorage, CounterBlobName);
//            counter.Increment(1);
//            Thread.Sleep(12000);
//        }
//    }

//}
