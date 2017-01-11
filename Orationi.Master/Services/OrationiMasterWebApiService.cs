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
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
	public class OrationiMasterWebApiService : IOrationiWebApiService
	{
		private readonly IOrationiEngine _orationiEngine;

		public OrationiMasterWebApiService(IOrationiEngine engine)
		{
			_orationiEngine = engine;
		}

		public string GetVersion()
		{
			try
			{
				return Assembly.GetExecutingAssembly().GetName().Version.ToString();
			}
			catch (Exception)
			{
				throw new WebFaultException(HttpStatusCode.InternalServerError);
			}
		}

		/// <summary>
		/// Get all slaves registred in system.
		/// </summary>
		/// <returns>Array of <c>OrationiSlaveItem</c>.</returns>
		public OrationiSlaveItem[] GetSlavesList()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get all modules registred in system.
		/// </summary>
		/// <returns>Array of <c>ModuleItem</c>.</returns>
		public ModuleItem[] GetModulesList()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get existing versions of module.
		/// </summary>
		/// <param name="module">Module identifier.</param>
		/// <returns>Array of <c>ModuleVersionItem</c>.</returns>
		public ModuleVersionItem[] GetModuleVerionsList(string module)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Register module in system.
		/// </summary>
		/// <param name="moduleName">Module name.</param>
		/// <returns><c>ModuleItem</c></returns>
		public ModuleItem RegisterModule(string moduleName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Remove module from system.
		/// </summary>
		/// <param name="module">Module identifier.</param>
		/// <returns>Array of <c>AssignedModule</c> if module wasn't assigned. Empty array in case of sucessful unregistration.</returns>
		public AssignedModule[] UnregisterModule(string module)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Assugn module to slave.
		/// </summary>
		/// <param name="slave">Slave identifier.</param>
		/// <param name="module">Module identifier.</param>
		/// <returns></returns>
		public AssignedModule AssignModule(string slave, string module)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Unassign module from slave.
		/// </summary>
		/// <param name="slave">Slave identifier.</param>
		/// <param name="module">Mpdule identifier.</param>
		public void UnassignModule(string slave, string module)
		{
			throw new NotImplementedException();
		}

		public void ExecutePowerShell(string slave, string script)
		{
			Guid slaveId;

			if (string.IsNullOrEmpty(slave))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (!Guid.TryParse(slave, out slaveId))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			_orationiEngine.ExecutePowerShell(slaveId, script);
		}

		/// <summary>
		/// Upload version of module package.
		/// </summary>
		/// <param name="module">Module ideintifier.</param>
		/// <param name="major">Major version.</param>
		/// <param name="minor">Minor version.</param>
		/// <param name="build">Build version.</param>
		/// <param name="revision">Revision.</param>
		/// <param name="packageStream">Stream with module package.</param>
		public void UploadModuleVersion(string module, string major, string minor, string build, string revision, Stream packageStream)
		{
			int moduleId;
			int majorNumber;
			int minorNumber = 0;
			int buildNumber = 0;
			int revisionNumber = 0;

			if (string.IsNullOrEmpty(module))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (!int.TryParse(module, out moduleId))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (string.IsNullOrEmpty(major))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (!int.TryParse(major, out majorNumber))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (!string.IsNullOrEmpty(minor))
			{
				if (!int.TryParse(minor, out minorNumber))
					throw new WebFaultException(HttpStatusCode.BadRequest);
			}

			if (!string.IsNullOrEmpty(build))
			{
				if (!int.TryParse(build, out buildNumber))
					throw new WebFaultException(HttpStatusCode.BadRequest);
			}

			if (!string.IsNullOrEmpty(revision))
			{
				if (!int.TryParse(revision, out revisionNumber))
					throw new WebFaultException(HttpStatusCode.BadRequest);
			}

			ModuleVersion moduleVersion = new ModuleVersion
			{
				ModuleId = moduleId,
				Major = majorNumber,
				Minor = minorNumber,
				Build = buildNumber,
				Revision = revisionNumber
			};

			_orationiEngine.SaveModule(moduleVersion, packageStream);

			throw new NotImplementedException();
		}
	}
}
