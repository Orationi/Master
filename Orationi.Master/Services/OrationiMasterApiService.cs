using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Web;
using Orationi.CommunicationCore.Interfaces;
using Orationi.CommunicationCore.Model;
using Orationi.Master.Interfaces;
using Orationi.Master.Model;

namespace Orationi.Master.Services
{
	/// <summary>
	/// Master API-service for Rich-client.
	/// </summary>
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
	public class OrationiMasterApiService : IOrationiApiService
	{
		/// <summary>
		/// System engine.
		/// </summary>
		private readonly IOrationiEngine _orationiEngine;

		/// <summary>
		/// Base constructor.
		/// </summary>
		/// <param name="engine">System engine.</param>
		public OrationiMasterApiService(IOrationiEngine engine)
		{
			_orationiEngine = engine;
		}

		/// <summary>
		/// Get current version of master-service.
		/// </summary>
		/// <returns></returns>
		public string GetVersion()
		{
			return Assembly.GetExecutingAssembly().GetName().Version.ToString();
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
			_orationiEngine.ExecutePowerShell(slaveId, script);
		}

		public void UploadModuleVersion(int moduleId, string major, string minor, string build, string revision, Stream packageStream)
		{
			ModuleVersion moduleVersion = new ModuleVersion
			{
				ModuleId = moduleId,
				Major = int.Parse(major),
				Minor = int.Parse(minor),
				Build = int.Parse(build),
				Revision = int.Parse(revision)
			};

			_orationiEngine.SaveModule(moduleVersion, packageStream);

			throw new NotImplementedException();
		}
	}
}
