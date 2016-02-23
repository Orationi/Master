using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
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
			return Assembly.GetExecutingAssembly().GetName().Version.ToString();
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
				return new ModuleVersionItem[0];

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
				return new AssignedModule[0];

			if (!int.TryParse(module, out moduleId))
				return new AssignedModule[0];

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

				ModuleDescription moduleEntity = masterDb.Modules.FirstOrDefault(m => m.Id == moduleId);
				if (moduleEntity == null)
					return new AssignedModule[0];

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
				return null;

			if (!Guid.TryParse(slave, out slaveId))
				return null;

			if (string.IsNullOrEmpty(module))
				return null;

			if (!int.TryParse(module, out moduleId))
				return null;

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
				return;

			if (!Guid.TryParse(slave, out slaveId))
				return;

			if (string.IsNullOrEmpty(module))
				return;

			if (!int.TryParse(module, out moduleId))
				return;

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
				return;

			if (!int.TryParse(module, out moduleId))
				return;

			if (string.IsNullOrEmpty(major))
				return;

			if (!int.TryParse(major, out majorNumber))
				return;

			if (!string.IsNullOrEmpty(minor))
			{
				if (!int.TryParse(minor, out minorNumber))
					return;
			}

			if (!string.IsNullOrEmpty(build))
			{
				if (!int.TryParse(build, out buildNumber))
					return;
			}

			if (!string.IsNullOrEmpty(revision))
			{
				if (!int.TryParse(revision, out revisionNumber))
					return;
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
