using System;
using System.IO;
using Orationi.CommunicationCore.Interfaces;
using Orationi.CommunicationCore.Model;
using Orationi.Master.Model;

namespace Orationi.Master.Interfaces
{
	public interface IOrationiEngine
	{
		void AbortConnection(string sessionId);

		void RemoveSlaveConnection(string sessionId);

		void AddSlaveConnection(string sessionId, IOrationiSlaveCallback callback);

		void Ping(string sessionId);

		DateTime? WhenSlaveWasActive(string ip);

		void SaveModule(ModuleVersion version, Stream stream);

		void PushModule(Guid slaveId, ModuleVersion version);

		void UndeployModule(Guid slaveId, int moduleId);

		Stream GetModule(int moduleId, string sessionId);

		SlaveConfiguration GetSlaveConfiguration(string sessionId);

		void PushMessage(string sessionId, PushedMessage message);

		void ExecutePowerShell(Guid slaveId, string script);
	}
}