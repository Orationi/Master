using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Runtime.InteropServices;
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
	public partial class Service : ServiceBase
	{
		public enum ServiceState
		{
			SERVICE_STOPPED = 0x00000001,
			SERVICE_START_PENDING = 0x00000002,
			SERVICE_STOP_PENDING = 0x00000003,
			SERVICE_RUNNING = 0x00000004,
			SERVICE_CONTINUE_PENDING = 0x00000005,
			SERVICE_PAUSE_PENDING = 0x00000006,
			SERVICE_PAUSED = 0x00000007,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ServiceStatus
		{
			public long dwServiceType;
			public ServiceState dwCurrentState;
			public long dwControlsAccepted;
			public long dwWin32ExitCode;
			public long dwServiceSpecificExitCode;
			public long dwCheckPoint;
			public long dwWaitHint;
		};

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

		private void SetServiceStatus(ServiceState state, int waitHint = 100000)
		{
			ServiceStatus serviceStatus = new ServiceStatus
			{
				dwCurrentState = state,
				dwWaitHint = waitHint
			};
			SetServiceStatus(this.ServiceHandle, ref serviceStatus);
		}

		private readonly EventLog _eventLog;

		public Service()
		{
			InitializeComponent();
			_eventLog = new EventLog();
			if (!EventLog.SourceExists("Orationi"))
			{
				EventLog.CreateEventSource("Orationi", "OrationiMasterLog");
			}
			_eventLog.Source = "Orationi";
			_eventLog.Log = "OrationiMasterLog";
		}

		private ServiceHost _masterHost;

		private ServiceHost _apiHost;

		protected override void OnStart(string[] args)
		{
			_eventLog.WriteEntry("Pending start");

			SetServiceStatus(ServiceState.SERVICE_START_PENDING);

			using (IUnityContainer container = new UnityContainer())
			{
				container.RegisterType<IOrationiEngine, OrationiMasterEngine>(new ContainerControlledLifetimeManager());

				Uri masterAddress = new Uri("net.tcp://localhost:57344/Orationi/Master/v1/");
				Uri apiAddress = new Uri("http://localhost:57345/Orationi/Master/Api/v1/");

				_masterHost = new UnityServiceHost(container, typeof(OrationiMasterService), masterAddress);
				_apiHost = new UnityServiceHost(container, typeof(OrationiMasterWebApiService), apiAddress);

				try
				{
					_masterHost.AddServiceEndpoint(typeof(IOrationiMasterService), new NetTcpBinding(SecurityMode.None), string.Empty);
					// ** DISCOVERY ** //
					// make the service discoverable by adding the discovery behavior
					ServiceDiscoveryBehavior discoveryBehavior = new ServiceDiscoveryBehavior();
					_masterHost.Description.Behaviors.Add(new ServiceDiscoveryBehavior());
					// send announcements on UDP multicast transport
					discoveryBehavior.AnnouncementEndpoints.Add(new UdpAnnouncementEndpoint());
					// ** DISCOVERY ** //
					// add the discovery endpoint that specifies where to publish the services
					_masterHost.Description.Endpoints.Add(new UdpDiscoveryEndpoint());
					_masterHost.Open();

					var apiEndpoint = _apiHost.AddServiceEndpoint(typeof(IOrationiWebApiService), new WebHttpBinding(WebHttpSecurityMode.None), string.Empty);
					apiEndpoint.EndpointBehaviors.Add(new WebHttpBehavior());
					apiEndpoint.Behaviors.Add(new CorsSupportBehavior());
					ServiceMetadataBehavior smb = _apiHost.Description.Behaviors.Find<ServiceMetadataBehavior>() ?? new ServiceMetadataBehavior();
					smb.HttpGetEnabled = true;
					smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
					_apiHost.Description.Behaviors.Add(smb);
					_apiHost.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
					_apiHost.Open();

					var eventMessage = "Master Service is running...\nAvailable Endpoints:";
					_masterHost.Description.Endpoints.ToList().ForEach(endpoint => eventMessage += endpoint.Address.ToString() + "\n");
					_eventLog.WriteEntry(eventMessage);
					Console.WriteLine(eventMessage);

					eventMessage = "\nMaster Api Service is running...\nAvailable Endpoints:";
					_apiHost.Description.Endpoints.ToList().ForEach(endpoint => eventMessage += endpoint.Address.ToString() + "\n");
					_eventLog.WriteEntry(eventMessage);
					Console.WriteLine(eventMessage);
				}
				catch (CommunicationException ce)
				{
					Console.WriteLine("An exception occurred: {0}", ce.Message);
					_eventLog.WriteEntry(ce.Message, EventLogEntryType.Error);
					_masterHost.Abort();
					_apiHost.Abort();
				}
			}

			_eventLog.WriteEntry("Running");
			SetServiceStatus(ServiceState.SERVICE_RUNNING);
		}

		protected override void OnStop()
		{
			_eventLog.WriteEntry("Pending stop");
			SetServiceStatus(ServiceState.SERVICE_STOP_PENDING);

			if (_masterHost.State == CommunicationState.Opened)
				_masterHost.Close();

			if (_apiHost.State == CommunicationState.Opened)
				_apiHost.Close();

			_eventLog.WriteEntry("Stopped");
			SetServiceStatus(ServiceState.SERVICE_STOPPED);
		}
	}
}
