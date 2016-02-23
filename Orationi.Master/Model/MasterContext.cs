using Microsoft.Data.Entity;

namespace Orationi.Master.Model
{
	class MasterContext : DbContext
	{
		public DbSet<SlaveDescription> Slaves { get; set; }

		public DbSet<ModuleDescription> Modules { get; set; }

		public DbSet<ModuleVersion> ModuleVersions { get; set; }

		public DbSet<SlaveModule> SlaveModules { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Filename=OrationiMaster.db");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<SlaveDescription>()
				.HasKey(s => s.Id);

			modelBuilder.Entity<ModuleDescription>()
				.HasKey(s => s.Id);

			modelBuilder.Entity<ModuleVersion>()
				.HasKey(s => new
						{
							s.ModuleId,
							s.Major,
							s.Minor,
							s.Build,
							s.Revision
						});

			modelBuilder.Entity<SlaveModule>()
				.HasKey(s => new
						{
							s.ModuleId,
							s.SlaveId
						});
		}
	}
}
