namespace Orationi.Master.Model
{
	public class ModuleVersion
	{
		public int ModuleId { get; set; }

		public int Major { get; set; }

		public int Minor { get; set; }

		public int Build { get; set; }

		public int Revision { get; set; }

		public string Path { get; set; }
	}
}
