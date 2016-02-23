using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orationi.Master.Model
{
	[Table("Modules")]
	public class ModuleDescription
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public string Name { get; set; }

		public virtual ICollection<ModuleVersion> ModuleVersions { get; set; }
	}
}
