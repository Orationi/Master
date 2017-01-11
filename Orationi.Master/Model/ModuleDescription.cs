using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orationi.Master.Model
{
	public class ModuleDescription
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public virtual ICollection<ModuleVersion> ModuleVersions { get; set; }
	}
}
