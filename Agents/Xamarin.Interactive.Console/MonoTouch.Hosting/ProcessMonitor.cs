
using System;
using System.Runtime.InteropServices;

using Darwin;

namespace MonoTouch.Hosting
{

	/// <summary>
	/// A utility class to wait on processes that are not necessarily child processes.
	/// </summary>
	public static class ProcessMonitor
	{
		/// <summary>
		/// Synchronously waits for the given PID and returns the exit code.
		/// </summary>
		public static unsafe int WaitPid (int pid, TimeSpan? timeout = null)
		{
			using (var kqueue = new KernelQueue ()) {
				var events = stackalloc KernelEvent [1];
				events [0] = new KernelEvent {
					Ident = (IntPtr)pid,
					Filter = EventFilter.Proc,
					// header says NOTE_EXITSTATUS only valid for child processes, but this seems to work for any arbitrary process
					FilterFlags = (uint)FilterFlags.ProcExitStatus,
					Flags = EventFlags.Add
				};

				var tsTimeout = timeout.GetValueOrDefault ().ToTimespec ();
				var eventCount = KQueue.kevent ((int)kqueue.Handle, events, 1, events, 1,
					timeout.HasValue ? &tsTimeout : (TimeSpec*)0);

				switch (eventCount) {

				case -1:
					throw new SystemException ("kqueue error: " + Marshal.GetLastWin32Error ());
				case 0:
					throw new TimeoutException ();
				}
				if (events [0].Flags.HasFlag (EventFlags.Error))
					throw new Exception ("Unix IO event error: " + (int)events [0].Data);

				return (int)events [0].Data >> 8;
			}
		}
	}
}