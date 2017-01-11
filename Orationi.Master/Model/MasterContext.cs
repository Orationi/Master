using System;
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
			_dataBase = new LiteDatabase(@"OrationiMaster.db");

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
	}
}
