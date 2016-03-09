using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Orationi.CommunicationCore.Interfaces;
using Orationi.CommunicationCore.Model;

namespace Orationi.Master.Services
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
	public class OrationiMasterApiService : IOrationiApiService
	{
		public string GetVersion()
		{
			throw new NotImplementedException();
		}

		public OrationiSlaveItem[] GetSlavesList()
		{
			throw new NotImplementedException();
		}

		public ModuleItem[] GetModulesList()
		{
			throw new NotImplementedException();
		}

		public ModuleVersionItem[] GetModuleVerionsList(int moduleId)
		{
			throw new NotImplementedException();
		}

		public ModuleItem RegisterModule(string moduleName)
		{
			throw new NotImplementedException();
		}

		public AssignedModule[] UnregisterModule(int moduleId)
		{
			throw new NotImplementedException();
		}

		public AssignedModule AssignModule(Guid slaveId, int moduleId)
		{
			throw new NotImplementedException();
		}

		public void UnassignModule(Guid slaveId, int moduleId)
		{
			throw new NotImplementedException();
		}

		public void ExecutePowerShell(Guid slaveId, string script)
		{
			throw new NotImplementedException();
		}

		public void UploadModuleVersion(int moduleId, string major, string minor, string build, string revision, Stream packageStream)
		{
			throw new NotImplementedException();
		}
	}
}
