using System.ComponentModel;
using System.Configuration.Install;

namespace Orationi.Master
{
	[RunInstaller(true)]
	public partial class ProjectInstaller : Installer
	{
		public ProjectInstaller()
		{
			InitializeComponent();
		}
	}
}
