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

		public ModuleVersionItem[] GetModuleVerionsList(int moduleId)
		{
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

		public AssignedModule[] UnregisterModule(int moduleId)
		{
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

		public AssignedModule AssignModule(Guid slaveId, int moduleId)
		{
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

		public void UnassignModule(Guid slaveId, int moduleId)
		{
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

			using (MasterContext masterDb = new MasterContext())
			{
				masterDb.ModuleVersions.Add(moduleVersion);
				masterDb.SaveChanges();
			}
		}
	}
}
