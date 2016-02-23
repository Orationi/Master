using System.ComponentModel.DataAnnotations.Schema;

namespace Orationi.Master.Model
{
	[Table("ModuleVersions")]
	public class ModuleVersion
	{
		public int ModuleId { get; set; }

		public int Major { get; set; }

		public int Minor { get; set; }

		public int Build { get; set; }

		public int Revision { get; set; }

		public string Path { get; set; }

		public virtual ModuleDescription Module { get; set; }
	}
}
