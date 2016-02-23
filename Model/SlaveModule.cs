using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orationi.Master.Model
{
	[Table("SlaveModules")]
	public class SlaveModule
	{
		public Guid SlaveId { get; set; }

		public SlaveDescription Slave { get; set; }

		public int ModuleId { get; set; }

		public ModuleDescription Module { get; set; }
	}
}
