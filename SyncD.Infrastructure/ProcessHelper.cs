using System;
using System.Diagnostics;
using System.Management;

namespace SyncD.Infrastructure
{
    public class ProcessHelper
    {
        public static void KillAllProcessesSpawnedBy(UInt32 processId)
        {
            var searcher = new ManagementObjectSearcher(string.Format("SELECT * FROM Win32_Process WHERE ParentProcessId={0}", processId));
            var collection = searcher.Get();

            if (collection.Count > 0)
            {
                foreach (var item in collection)
                {
                    var childProcessId = (UInt32)item["ProcessId"];
                    if ((int)childProcessId != Process.GetCurrentProcess().Id)
                    {
                        KillAllProcessesSpawnedBy(childProcessId);

                        var childProcess = Process.GetProcessById((int)childProcessId);
                        childProcess.Kill();
                    }
                }
            }
        }
    }
}