using System.ServiceProcess;

namespace Orationi.Master
{
	class Program
	{
		static void Main(string[] args)
		{
			var servicesToRun = new ServiceBase[]
			{
				new Service()
			};
			ServiceBase.Run(servicesToRun);
		}
	}
}
