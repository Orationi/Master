using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orationi.Master.Model
{

	[Table("Slaves")]
	public class SlaveDescription
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public Guid Id { get; set; }

		[MaxLength(50)]
		public string Name { get; set; }

		[MaxLength(250)]
		public string Description { get; set; }

		[MaxLength(50)]
		public string Address { get; set; }

		public DateTime? LastConnectionOn { get; set; }

		public DateTime RegistredOn { get; set; }
	}
}
