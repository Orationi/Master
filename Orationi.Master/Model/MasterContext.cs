using System.Data.Entity;

namespace Orationi.Master.Model
{
	class MasterContext : DbContext
	{
		public DbSet<SlaveDescription> Slaves { get; set; }

		public DbSet<ModuleDescription> Modules { get; set; }

		public DbSet<ModuleVersion> ModuleVersions { get; set; }

		public DbSet<SlaveModule> SlaveModules { get; set; }

		public MasterContext() : base(typeof(MasterContext).Name)
		{

		}
	}
}
