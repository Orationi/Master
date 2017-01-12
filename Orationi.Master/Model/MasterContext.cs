using System;
using System.IO;
using System.Management;
using LiteDB;

namespace Orationi.Master.Model
{
	class MasterContext : IDisposable
	{
		private readonly LiteDatabase _dataBase;

		public LiteCollection<SlaveDescription> Slaves => _dataBase.GetCollection<SlaveDescription>("SlaveDescriptions");

		public MasterContext()
		{
			// Open database (or create if doesn't exist)
			_dataBase = new LiteDatabase(Path.Combine(this.GetDatabasePath(), @"OrationiMaster.db"));

			// Get a collection (or create, if doesn't exist)
			_dataBase.GetCollection<ModuleDescription>("ModuleDescriptions");
			_dataBase.GetCollection<ModuleVersion>("ModuleVersions");
			_dataBase.GetCollection<SlaveDescription>("SlaveDescriptions");
			_dataBase.GetCollection<SlaveModule>("SlaveModules");
		}

		public void Dispose()
		{
			_dataBase?.Dispose();
		}

		private string GetDatabasePath()
		{
			WqlObjectQuery wqlObjectQuery = new WqlObjectQuery(string.Format("SELECT * FROM Win32_Service WHERE Name = '{0}'", "OrationiMasterService"));
			ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(wqlObjectQuery);
			ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();

			foreach (ManagementObject managementObject in managementObjectCollection)
			{
				return Path.GetDirectoryName(managementObject.GetPropertyValue("PathName").ToString());
			}



			return string.Empty;
		}
	}
}
