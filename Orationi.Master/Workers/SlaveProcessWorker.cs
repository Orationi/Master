using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Orationi.CommunicationCore.Interfaces;
using Orationi.CommunicationCore.Model;
using Orationi.CommunicationCore.Net;
using Orationi.Master.Model;

namespace Orationi.Master.Workers
{
	/// <summary>
	/// Slave worker
	/// </summary>
	public class SlaveProcessWorker
	{
		/// <summary>
		/// Synchronization object.
		/// </summary>
		private readonly object _locker = new object();

		/// <summary>
		/// Get session id.
		/// </summary>
		public string SessionId { get; protected set; }

		/// <summary>
		/// Slave Ip.
		/// </summary>
		public string Ip { get; protected set; }

		private volatile int _inactiveTimeout = 10000;
		/// <summary>
		/// Get inactive timeout in milliseconds.
		/// </summary>
		public int InactiveTimeout
		{
			get { return _inactiveTimeout; }
			set
			{
				if (_inactiveTimeout == value)
					return;

				_inactiveTimeout = value;
			}
		}

		private DateTime _lastActivity = DateTime.Now;
		/// <summary>
		/// Datetime of last slave process activity.
		/// </summary>
		public DateTime LastActivity
		{
			get
			{
				lock (_locker)
				{
					return _lastActivity;
				}
			}
			set
			{
				lock (_locker)
				{
					_lastActivity = value;
				}
			}
		}

		/// <summary>
		/// Slave callback interface.
		/// </summary>
		private IOrationiSlaveCallback Callback { get; }

		private ICommunicationObject CommunicationObject
		{
			get
			{
				return Callback as ICommunicationObject;
			}
		}

		public bool IsReady
		{
			get
			{
				return State == CommunicationState.Opened
					&& DateTime.Now.Subtract(LastActivity) < TimeSpan.FromMilliseconds(InactiveTimeout);
			}
		}

		public CommunicationState State
		{
			get
			{
				lock (_locker)
				{
					return CommunicationObject.State;
				}
			}
		}

		private Guid _slaveId;
		public Guid SlaveId
		{
			get
			{
				lock (_locker)
				{
					return _slaveId;
				}
			}
			set
			{
				lock (_locker)
				{
					if (_slaveId == value)
						return;

					_slaveId = value;
				}
			}
		}

		private Queue<PushedMessage> PushedMessages { get; set; }

		public SlaveProcessWorker(string sessionId, IOrationiSlaveCallback callback, int inactiveTimeout = 30000)
		{
			SessionId = sessionId;
			Ip = NetworkUtility.GetClientIpFromOperationContext();
			Callback = callback;
			LastActivity = DateTime.Now;
			InactiveTimeout = inactiveTimeout;

			//Save information in Db
			using (MasterContext masterDb = new MasterContext())
			{
				//If slave exist in db - update some information.
				SlaveDescription slave = masterDb.Slaves.FirstOrDefault(s => s.Address == Ip);
				if (slave != null)
				{
					slave.LastConnectionOn = DateTime.Now;
					masterDb.SaveChanges();
					SlaveId = slave.Id;
					return;
				}

				//If slave not exist in db - create new record
				string hostName = NetworkUtility.GetHostName(Ip);
				slave = new SlaveDescription
				{
					Address = Ip,
					//Id = Guid.NewGuid(),
					RegistredOn = DateTime.Now,
					LastConnectionOn = DateTime.Now,
					Name = $"Unknown slave {hostName} ({Ip})"
				};

				masterDb.Slaves.Add(slave);
				masterDb.SaveChanges();

				SlaveId = slave.Id;
			}

#if DEBUG
			Console.WriteLine("Connection from {0} {1}", SessionId, DateTime.Now.ToString("O"));
#endif
		}

		#region Base Methods

		public void Ping()
		{
			LastActivity = DateTime.Now;

#if DEBUG
			Console.WriteLine("Pinged by {0} {1}", SessionId, LastActivity.ToString("O"));
#endif
		}

		/// <summary>
		/// Send abort connection command and abort communication object.
		/// </summary>
		public void AbortConnection()
		{
			if (CommunicationObject.State != CommunicationState.Opened)
				return;

			try
			{
				Callback.AbortConnection();
				CommunicationObject.Abort();
			}
			catch (Exception ex)
			{
				Console.WriteLine("{0}", ex.Message);
			}

#if DEBUG
			Console.WriteLine("Send AbortConnection {0} {1}", SessionId, DateTime.Now.ToString("O"));
#endif
		}

		#endregion

		public void PushModule(ModuleVersion moduleVersion)
		{
			using (FileStream stream = new FileStream(moduleVersion.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				Callback.PushModule(stream);
			}
		}

		public void UndeployModule(int moduleId)
		{
			Callback.UndeployModule(moduleId);
		}

		public bool HasModule(int moduleId)
		{
			using (MasterContext masterDb = new MasterContext())
			{
				SlaveModule slaveModule = masterDb.SlaveModules.FirstOrDefault(s => s.SlaveId == SlaveId && s.ModuleId == moduleId);
				return slaveModule != null;
			}
		}

		public SlaveConfiguration GetConfiguration()
		{
			SlaveConfiguration configuration = new SlaveConfiguration();

			using (MasterContext masterDb = new MasterContext())
			{
				IEnumerable<SlaveModule> slaveModules = masterDb.SlaveModules.Where(s => s.SlaveId == SlaveId);
				configuration.Modules = new ModuleVersionItem[slaveModules.Count()];

				int index = 0;
				foreach (SlaveModule slaveModule in slaveModules)
				{
					ModuleVersion moduleVersion = masterDb.ModuleVersions.Where(m => m.ModuleId == slaveModule.ModuleId)
																			.OrderByDescending(m => m.Major)
																			.ThenByDescending(m => m.Minor)
																			.ThenByDescending(m => m.Build)
																			.ThenByDescending(m => m.Revision)
																			.FirstOrDefault();

					if (moduleVersion == null)
						continue;

					ModuleVersionItem moduleVersionItem = new ModuleVersionItem
					{
						ModuleId = slaveModule.ModuleId,
						Major = moduleVersion.Major,
						Minor = moduleVersion.Minor,
						Build = moduleVersion.Build,
						Revision = moduleVersion.Revision
					};

					configuration.Modules[index] = moduleVersionItem;
					index++;
				}
			}
			return configuration;
		}

		/// <summary>
		/// Push message to queue.
		/// </summary>
		/// <param name="message">Pushed message.</param>
		public void PushMessage(PushedMessage message)
		{
			PushedMessages.Enqueue(message);

			if (PushedMessages.Count > 100)
				PushedMessages.Dequeue();
		}

		public void ExecutePowerShell(string script)
		{
			Callback.ExecutePowerShell(script);
		}
	}
}
