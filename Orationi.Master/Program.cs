using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Discovery;
using Microsoft.Practices.Unity;
using Orationi.CommunicationCore.Interfaces;
using Orationi.Master.Engine;
using Orationi.Master.Interfaces;
using Orationi.Master.Services;
using Orationi.Master.Unity;
using Orationi.Master.Utils;

namespace Orationi.Master
{
	class Program
	{
		static void Main(string[] args)
		{
#if DEBUG
			Console.WriteLine("Mode=Debug");
#else
			Console.WriteLine("Mode=Release");
#endif

			using (IUnityContainer container = new UnityContainer())
			{
				container.RegisterType<IOrationiEngine, OrationiMasterEngine>(new ContainerControlledLifetimeManager());

				Uri masterAddress = new Uri("net.tcp://localhost:57344/Orationi/Master/v1/");
				Uri apiAddress = new Uri("http://localhost:57345/Orationi/Master/Api/v1/");

				ServiceHost masterHost = new UnityServiceHost(container, typeof(OrationiMasterService), masterAddress);
				ServiceHost apiHost = new UnityServiceHost(container, typeof(OrationiMasterWebApiService), apiAddress);

				try
				{
					masterHost.AddServiceEndpoint(typeof(IOrationiMasterService), new NetTcpBinding(SecurityMode.None), string.Empty);
					// ** DISCOVERY ** //
					// make the service discoverable by adding the discovery behavior
					ServiceDiscoveryBehavior discoveryBehavior = new ServiceDiscoveryBehavior();
					masterHost.Description.Behaviors.Add(new ServiceDiscoveryBehavior());
					// send announcements on UDP multicast transport
					discoveryBehavior.AnnouncementEndpoints.Add(new UdpAnnouncementEndpoint());
					// ** DISCOVERY ** //
					// add the discovery endpoint that specifies where to publish the services
					masterHost.Description.Endpoints.Add(new UdpDiscoveryEndpoint());
					masterHost.Open();

					var apiEndpoint = apiHost.AddServiceEndpoint(typeof(IOrationiApiService), new WebHttpBinding(WebHttpSecurityMode.None), string.Empty);
					apiEndpoint.EndpointBehaviors.Add(new WebHttpBehavior());
					apiEndpoint.Behaviors.Add(new CorsSupportBehavior());
					ServiceMetadataBehavior smb = apiHost.Description.Behaviors.Find<ServiceMetadataBehavior>() ?? new ServiceMetadataBehavior();
					smb.HttpGetEnabled = true;
					smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
					apiHost.Description.Behaviors.Add(smb);
					apiHost.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
					apiHost.Open();

					Console.WriteLine("\nMaster Service is running...");
					Console.WriteLine("\nAvailable Endpoints:");
					masterHost.Description.Endpoints.ToList().ForEach(endpoint => Console.WriteLine(endpoint.Address.ToString()));

					Console.WriteLine("\nMaster Api Service is running...");
					Console.WriteLine("\nAvailable Endpoints:");
					apiHost.Description.Endpoints.ToList().ForEach(endpoint => Console.WriteLine(endpoint.Address.ToString()));

					Console.WriteLine("Press <ENTER> to terminate service.");
					Console.WriteLine();

					Console.ReadLine();

					// Close the ServiceHostBase to shutdown the service.
					masterHost.Close();
					apiHost.Close();
				}
				catch (CommunicationException ce)
				{
					Console.WriteLine("An exception occurred: {0}", ce.Message);
					masterHost.Abort();
					apiHost.Abort();
				}
			}
		}
	}
}
