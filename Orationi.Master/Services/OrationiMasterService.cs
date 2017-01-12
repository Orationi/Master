using System.IO;
using System.ServiceModel;
using Orationi.CommunicationCore.Interfaces;
using Orationi.CommunicationCore.Model;
using Orationi.Master.Interfaces;

namespace Orationi.Master.Services
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
	public class OrationiMasterService : IOrationiMasterService
	{
		private readonly IOrationiEngine _orationiEngine;

		public OrationiMasterService(IOrationiEngine engine)
		{
			_orationiEngine = engine;
		}

		/// <summary>
		/// Accept incoming connections from slaves.
		/// </summary>
		public SlaveConfiguration Connect()
		{
			string sessionId = OperationContext.Current.SessionId;
			IOrationiSlaveCallback callback = OperationContext.Current.GetCallbackChannel<IOrationiSlaveCallback>();
			//_orationiEngine.AddSlaveConnection(sessionId, callback);
			return new SlaveConfiguration()
			{
				Modules = new ModuleVersionItem[0]
			}; //_orationiEngine.GetSlaveConfiguration(sessionId);
		}

		/// <summary>
		/// Receive ping from slave.
		/// </summary>
		public void Ping()
		{
			_orationiEngine.Ping(OperationContext.Current.SessionId);
		}

		/// <summary>
		/// Disconnectio slave.
		/// </summary>
		public void Disconnect()
		{
			_orationiEngine.RemoveSlaveConnection(OperationContext.Current.SessionId);
		}

		/// <summary>
		/// Get stream of module package.
		/// </summary>
		/// <param name="moduleId">Module identifier.</param>
		/// <returns><c>Stream</c> of package or error.</returns>
		public Stream GetModule(int moduleId)
		{
			return _orationiEngine.GetModule(moduleId, OperationContext.Current.SessionId);
		}

		/// <summary>
		/// Push message to orationi engine.
		/// </summary>
		/// <param name="message">Pushed message.</param>
		public void PushMessage(PushedMessage message)
		{
			_orationiEngine.PushMessage(OperationContext.Current.SessionId, message);
		}
	}
}
