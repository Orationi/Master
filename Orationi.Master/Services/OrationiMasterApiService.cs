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
	public class OrationiMasterApiService : IOrationiApiService
	{
		private readonly IOrationiEngine _orationiEngine;

		public OrationiMasterApiService(IOrationiEngine engine)
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
			using (MasterContext masterDb = new MasterContext())
			{
				IEnumerable<SlaveDescription> slaveDescriptions = masterDb.Slaves;
				OrationiSlaveItem[] orationiSlaveItems = new OrationiSlaveItem[slaveDescriptions.Count()];

				int index = 0;
				foreach (SlaveDescription slaveDescription in slaveDescriptions)
				{
					OrationiSlaveItem orationiSlaveItem = new OrationiSlaveItem
					{
						Address = slaveDescription.Address,
						Description = slaveDescription.Description,
						Name = slaveDescription.Name,
						Id = slaveDescription.Id
					};
					orationiSlaveItem.LastActivity = _orationiEngine.WhenSlaveWasActive(orationiSlaveItem.Address);
					orationiSlaveItems[index] = orationiSlaveItem;
					index++;
				}

				return orationiSlaveItems;
			}
		}

		/// <summary>
		/// Get all modules registred in system.
		/// </summary>
		/// <returns>Array of <c>ModuleItem</c>.</returns>
		public ModuleItem[] GetModulesList()
		{
			using (MasterContext masterDb = new MasterContext())
			{
				IEnumerable<ModuleDescription> modules = masterDb.Modules;
				ModuleItem[] result = new ModuleItem[modules.Count()];

				int index = 0;
				foreach (ModuleDescription moduleDescription in modules)
				{
					ModuleItem moduleItem = new ModuleItem
					{
						Id = moduleDescription.Id,
						Name = moduleDescription.Name
					};
					result[index] = moduleItem;
					index++;
				}
				return result;
			}
		}

		/// <summary>
		/// Get existing versions of module.
		/// </summary>
		/// <param name="module">Module identifier.</param>
		/// <returns>Array of <c>ModuleVersionItem</c>.</returns>
		public ModuleVersionItem[] GetModuleVerionsList(string module)
		{
			int moduleId;
			if (!int.TryParse(module, out moduleId))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			using (MasterContext masterDb = new MasterContext())
			{

				IEnumerable<ModuleVersion> moduleVersions = masterDb.ModuleVersions.Where(mv => mv.ModuleId == moduleId);
				ModuleVersionItem[] result = new ModuleVersionItem[moduleVersions.Count()];

				int index = 0;
				foreach (ModuleVersion moduleVersion in moduleVersions)
				{
					ModuleVersionItem moduleVersionItem = new ModuleVersionItem
					{
						ModuleId = moduleId,
						Major = moduleVersion.Major,
						Minor = moduleVersion.Minor,
						Build = moduleVersion.Build,
						Revision = moduleVersion.Revision
					};
					result[index] = moduleVersionItem;
					index++;
				}
				return result;
			}
		}

		/// <summary>
		/// Register module in system.
		/// </summary>
		/// <param name="moduleName">Module name.</param>
		/// <returns><c>ModuleItem</c></returns>
		public ModuleItem RegisterModule(string moduleName)
		{
			using (MasterContext masterDb = new MasterContext())
			{
				ModuleDescription module = masterDb.Modules.FirstOrDefault(m => string.Equals(m.Name, moduleName, StringComparison.CurrentCultureIgnoreCase));
				ModuleItem result = new ModuleItem();

				if (module == null)
				{
					module = new ModuleDescription();
					module.Name = moduleName;
					masterDb.Modules.Add(module);
					masterDb.SaveChanges();
				}

				result.Id = module.Id;
				result.Name = module.Name;

				return result;
			}
		}

		/// <summary>
		/// Remove module from system.
		/// </summary>
		/// <param name="module">Module identifier.</param>
		/// <returns>Array of <c>AssignedModule</c> if module wasn't assigned. Empty array in case of sucessful unregistration.</returns>
		public AssignedModule[] UnregisterModule(string module)
		{
			int moduleId;

			if (string.IsNullOrEmpty(module))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (!int.TryParse(module, out moduleId))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			using (MasterContext masterDb = new MasterContext())
			{
				IEnumerable<SlaveModule> slaveModules = masterDb.SlaveModules.Where(s => s.ModuleId == moduleId);
				AssignedModule[] assignedModules = new AssignedModule[slaveModules.Count()];
				int index = 0;
				foreach (SlaveModule slaveModule in slaveModules)
				{
					AssignedModule assignedModule = new AssignedModule();
					assignedModule.SlaveId = slaveModule.SlaveId;

					ModuleVersionItem moduleVersionItem = new ModuleVersionItem { ModuleId = moduleId };

					ModuleVersion moduleVersion = masterDb.ModuleVersions.Where(mv => mv.ModuleId == moduleId)
						.OrderByDescending(mv => mv.Major)
						.ThenByDescending(mv => mv.Minor)
						.ThenByDescending(mv => mv.Build)
						.ThenByDescending(mv => mv.Revision)
						.FirstOrDefault();

					if (moduleVersion != null)
					{
						moduleVersionItem.Major = moduleVersion.Major;
						moduleVersionItem.Minor = moduleVersion.Minor;
						moduleVersionItem.Build = moduleVersion.Build;
						moduleVersionItem.Revision = moduleVersion.Revision;
					};

					assignedModule.ModuleVersion = moduleVersionItem;

					assignedModules[index] = assignedModule;
					index++;
				}

				if (assignedModules.Any())
					return assignedModules;

				ModuleDescription moduleEntity = masterDb.Modules.FirstOrDefault(m => m.Id == moduleId);
				if (moduleEntity == null)
					throw new WebFaultException(HttpStatusCode.NotFound);

				masterDb.Modules.Remove(moduleEntity);
				masterDb.SaveChanges();
			}

			return new AssignedModule[0];
		}

		/// <summary>
		/// Assugn module to slave.
		/// </summary>
		/// <param name="slave">Slave identifier.</param>
		/// <param name="module">Module identifier.</param>
		/// <returns></returns>
		public AssignedModule AssignModule(string slave, string module)
		{
			Guid slaveId;
			int moduleId;

			if (string.IsNullOrEmpty(slave))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (!Guid.TryParse(slave, out slaveId))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (string.IsNullOrEmpty(module))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (!int.TryParse(module, out moduleId))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			using (MasterContext masterDb = new MasterContext())
			{
				SlaveModule slaveModule = masterDb.SlaveModules.FirstOrDefault(s => s.SlaveId == slaveId && s.ModuleId == moduleId);
				if (slaveModule == null)
				{
					slaveModule = new SlaveModule()
					{
						ModuleId = moduleId,
						SlaveId = slaveId
					};
					masterDb.SlaveModules.Add(slaveModule);
					masterDb.SaveChanges();
				}
				ModuleVersionItem moduleVersionItem = new ModuleVersionItem { ModuleId = moduleId };

				ModuleVersion moduleVersion = masterDb.ModuleVersions.Where(mv => mv.ModuleId == moduleId)
					.OrderByDescending(mv => mv.Major)
					.ThenByDescending(mv => mv.Minor)
					.ThenByDescending(mv => mv.Build)
					.ThenByDescending(mv => mv.Revision)
					.FirstOrDefault();

				if (moduleVersion != null)
				{
					moduleVersionItem.Major = moduleVersion.Major;
					moduleVersionItem.Minor = moduleVersion.Minor;
					moduleVersionItem.Build = moduleVersion.Build;
					moduleVersionItem.Revision = moduleVersion.Revision;
				};

				_orationiEngine.PushModule(slaveId, moduleVersion);

				AssignedModule assignedModule = new AssignedModule
				{
					SlaveId = slaveId,
					ModuleVersion = moduleVersionItem
				};

				return assignedModule;
			}
		}

		/// <summary>
		/// Unassign module from slave.
		/// </summary>
		/// <param name="slave">Slave identifier.</param>
		/// <param name="module">Mpdule identifier.</param>
		public void UnassignModule(string slave, string module)
		{
			Guid slaveId;
			int moduleId;

			if (string.IsNullOrEmpty(slave))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (!Guid.TryParse(slave, out slaveId))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (string.IsNullOrEmpty(module))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			if (!int.TryParse(module, out moduleId))
				throw new WebFaultException(HttpStatusCode.BadRequest);

			using (MasterContext masterDb = new MasterContext())
			{
				SlaveModule slaveModule = masterDb.SlaveModules.FirstOrDefault(s => s.SlaveId == slaveId && s.ModuleId == moduleId);
				if (slaveModule == null)
					return;

				masterDb.SlaveModules.Remove(slaveModule);
				masterDb.SaveChanges();

				_orationiEngine.UndeployModule(slaveId, moduleId);
			}
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

			using (MasterContext masterDb = new MasterContext())
			{
				masterDb.ModuleVersions.Add(moduleVersion);
				masterDb.SaveChanges();
			}
		}
	}
}
