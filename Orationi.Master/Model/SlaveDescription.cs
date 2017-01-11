using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orationi.Master.Model
{
	public class SlaveDescription
	{
		public Guid Id { get; set; }
		
		public string Name { get; set; }
		
		public string Description { get; set; }

		public string Address { get; set; }

		public DateTime? LastConnectionOn { get; set; }

		public DateTime RegistredOn { get; set; }
	}
}
