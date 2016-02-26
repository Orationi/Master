using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orationi.CommunicationCore.Interfaces;
using Orationi.CommunicationCore.Model;
using Orationi.CommunicationCore.Net;
using Orationi.Master.Interfaces;
using Orationi.Master.Model;
using Orationi.Master.Workers;

namespace Orationi.Master.Engine
{
	/// <summary>
	/// Master engine.
	/// </summary>
	public class OrationiMasterEngine : IOrationiEngine
	{
		/// <summary>
		/// Sync object.
		/// </summary>
		private object _locker = new object();

		public string BaseModuleDirectory { get; set; }

		/// <summary>
		/// Connections dictionary for safe multy-thread use.
		/// </summary>
		public ConcurrentDictionary<string, SlaveProcessWorker> SlaveConnections { get; }

		private volatile int _inactiveTimeout = 30000;
		public int InactiveTimeout
		{
			get
			{
				return _inactiveTimeout;
			}
			set
			{
				if (_inactiveTimeout == value)
					return;
				_inactiveTimeout = value;

				Parallel.ForEach(SlaveConnections.Values, (currentWorker) => { currentWorker.InactiveTimeout = _inactiveTimeout; });
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public OrationiMasterEngine()
		{
			SlaveConnections = new ConcurrentDictionary<string, SlaveProcessWorker>();
			InitializeDataContext();
			InitializeStorage();
			StartMonitoringInactive();
		}

		private void InitializeStorage()
		{
			BaseModuleDirectory = Path.Combine(Environment.CurrentDirectory, "Modules");
			if (!Directory.Exists(BaseModuleDirectory))
				Directory.CreateDirectory(BaseModuleDirectory);
		}

		private void InitializeDataContext()
		{
			using (MasterContext masterDb = new MasterContext())
				masterDb.Database.EnsureCreated();
		}

		#region activity monitoring
		readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
		private readonly AutoResetEvent _abortConnections = new AutoResetEvent(false);
		/// <summary>
		/// Launch activity monitor.
		/// </summary>
		private void StartMonitoringInactive()
		{
			var token = _shutdown.Token;
			Task.Run(
				() =>
				{
					while (!token.IsCancellationRequested)
					{
						IEnumerable<SlaveProcessWorker> removableSlaveProcessWorkers = SlaveConnections.Values.Where(s => !s.IsReady);
						foreach (SlaveProcessWorker removableSlaveProcessWorker in removableSlaveProcessWorkers)
							AbortConnection(removableSlaveProcessWorker.SessionId);

						_abortConnections.WaitOne(InactiveTimeout);
					}
				},
				token
			);
		}
		#endregion

		#region modules

		/// <summary>
		/// Save module to modules storage directory.
		/// </summary>
		/// <param name="version">Version of module.</param>
		/// <param name="stream">Module package stream.</param>
		public void SaveModule(ModuleVersion version, Stream stream)
		{
			string moduleFolder = Path.Combine(BaseModuleDirectory, version.ModuleId.ToString());
			if (!Directory.Exists(BaseModuleDirectory))
				Directory.CreateDirectory(BaseModuleDirectory);

			string modulePath = $"{moduleFolder}/{version.Major}.{version.Minor}.{version.Build}.{version.Revision}.zpg";
			using (FileStream fileStream = File.Create(modulePath))
			{
				stream.Seek(0, SeekOrigin.Begin);
				stream.CopyTo(fileStream);
			}

			version.Path = modulePath.Remove(0, BaseModuleDirectory.Length);
		}

		/// <summary>
		/// Push module to slave.
		/// </summary>
		/// <param name="slaveId">Guid of slave.</param>
		/// <param name="moduleVersion">Version of module to push.</param>
		public void PushModule(Guid slaveId, ModuleVersion moduleVersion)
		{
			SlaveProcessWorker slaveProcessWorker = SlaveConnections.Values.FirstOrDefault(s => s.SlaveId == slaveId);
			if (slaveProcessWorker == null)
				return;

			slaveProcessWorker.PushModule(moduleVersion);
		}

		/// <summary>
		/// Undeploy module on slave.
		/// </summary>
		/// <param name="slaveId">Guid of slave.</param>
		/// <param name="moduleId">Module identifier.</param>
		public void UndeployModule(Guid slaveId, int moduleId)
		{
			SlaveProcessWorker slaveProcessWorker = SlaveConnections.Values.FirstOrDefault(s => s.SlaveId == slaveId);
			if (slaveProcessWorker == null)
				return;

			slaveProcessWorker.UndeployModule(moduleId);
		}

		/// <summary>
		/// Return stream of package with module.
		/// </summary>
		/// <param name="moduleId">Module identificator.</param>
		/// <param name="sessionId">Session identificator.</param>
		/// <returns></returns>
		public Stream GetModule(int moduleId, string sessionId)
		{
			if (!SlaveConnections.ContainsKey(sessionId))
				throw new Exception("Slave connection not found.");

			if (!SlaveConnections[sessionId].HasModule(moduleId))
				throw new Exception("Requested module doesn't assigned to current slave.");

			ModuleVersion moduleVersion;
			using (MasterContext masterDb = new MasterContext())
			{
				moduleVersion = masterDb.ModuleVersions.Where(m => m.ModuleId == moduleId)
					.OrderByDescending(m => m.Major)
					.ThenByDescending(m => m.Minor)
					.ThenByDescending(m => m.Build)
					.ThenByDescending(m => m.Revision)
					.FirstOrDefault();

				if (moduleVersion == null)
					throw new Exception("Module version not found.");
			}

			using (FileStream stream = new FileStream(moduleVersion.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
				return stream;
		}

		public SlaveConfiguration GetSlaveConfiguration(string sessionId)
		{
			if (!SlaveConnections.ContainsKey(sessionId))
				throw new Exception("Session not found");

			SlaveProcessWorker slaveProcessWorker = SlaveConnections[sessionId];
			return slaveProcessWorker.GetConfiguration();
		}

		/// <summary>
		/// Push message to current session processor.
		/// </summary>
		/// <param name="sessionId"></param>
		/// <param name="message"></param>
		public void PushMessage(string sessionId, PushedMessage message)
		{
			if (!SlaveConnections.ContainsKey(sessionId))
				throw new Exception("Session not found");

			SlaveProcessWorker slaveProcessWorker = SlaveConnections[sessionId];
			slaveProcessWorker.PushMessage(message);
		}

		/// <summary>
		/// Execute power shell script on slave side.
		/// </summary>
		/// <param name="slaveId">Slave id.</param>
		/// <param name="script">PowerShell script.</param>
		public void ExecutePowerShell(Guid slaveId, string script)
		{
			SlaveProcessWorker slaveProcessWorker = SlaveConnections.Values.FirstOrDefault(s => s.SlaveId == slaveId);
			if (slaveProcessWorker == null)
				return;

			slaveProcessWorker.ExecutePowerShell(script);
		}

		#endregion

		#region connections

		/// <summary>
		/// Add slave connection to dictionary.
		/// </summary>
		/// <param name="sessionId">Slave service session id.</param>
		/// <param name="callback">IOrationiSlaveCallback</param>
		public void AddSlaveConnection(string sessionId, IOrationiSlaveCallback callback)
		{
			string ip = NetworkUtility.GetClientIpFromOperationContext();
			//Kick existing connection with same sessionId or ip.
			KickExistingConnections(sessionId, ip);

			//Create new slave process worker
			SlaveProcessWorker newSlaveProcessWorker = new SlaveProcessWorker(sessionId, callback, InactiveTimeout);
			SlaveConnections[sessionId] = newSlaveProcessWorker;
		}

		/// <summary>
		/// Drop expired sessions by id and ip. 
		/// </summary>
		/// <param name="sessionId">Session identifier.</param>
		/// <param name="ip">Slave ip address.</param>
		private void KickExistingConnections(string sessionId, string ip)
		{
			//Abort connections by sessionId.
			if (SlaveConnections.ContainsKey(sessionId))
				AbortConnection(sessionId);

			//Abort connections by source ip.
			IEnumerable<SlaveProcessWorker> slaveProcessWorkers = SlaveConnections.Values.Where(sc => sc.Ip == ip);
			foreach (SlaveProcessWorker processWorker in slaveProcessWorkers)
				AbortConnection(processWorker.SessionId);
		}

		/// <summary>
		/// Update last activity of slave service.
		/// </summary>
		/// <param name="sessionId">Slave service session id.</param>
		public void Ping(string sessionId)
		{
			SlaveConnections[sessionId].Ping();
		}

		/// <summary>
		/// Remove slave connection from dictionary and send abort connection command.
		/// </summary>
		/// <param name="sessionId">Slave service session id.</param>
		public void RemoveSlaveConnection(string sessionId)
		{
			SlaveProcessWorker slaveProcessWorker;
			SlaveConnections.TryRemove(sessionId, out slaveProcessWorker);
		}

		/// <summary>
		/// Abort session by Id.
		/// </summary>
		/// <param name="sessionId">Slave service session id.</param>
		public void AbortConnection(string sessionId)
		{
			SlaveProcessWorker slaveProcessWorker;
			SlaveConnections.TryRemove(sessionId, out slaveProcessWorker);
			slaveProcessWorker.AbortConnection();
#if DEBUG
			Console.WriteLine("Connection aborted with {0} {1}", slaveProcessWorker.SessionId, DateTime.Now.ToString("O"));
#endif
		}

		#endregion

		/// <summary>
		/// Return datetime of last slave activity.
		/// </summary>
		/// <param name="ip">Slave ip address.</param>
		/// <returns>If slave is active <c>DateTime</c>, else <c>null</c></returns>
		public DateTime? WhenSlaveWasActive(string ip)
		{
			SlaveProcessWorker slaveProcessWorker = SlaveConnections.Values.FirstOrDefault(s => s.Ip == ip && s.IsReady);
			return slaveProcessWorker?.LastActivity;
		}
	}
}
