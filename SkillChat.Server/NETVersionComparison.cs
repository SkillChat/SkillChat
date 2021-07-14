using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace SkillChat.Server
{
    public class NETVersionComparison
    {
        /// <summary>
        /// Получение наиболее подходящего Runtime .NET
        /// </summary>
        public static Runtime GetBestDotNETRuntime(string cfgVersion)
        {
            Runtime cfgRuntime = new Runtime() {Version = cfgVersion};
            List<Runtime> _runtimesInstaled = new List<Runtime>();
            List<Runtime> _NETCoreRuntimes = new List<Runtime>();

            GetPackageList(_runtimesInstaled);

            foreach (var item in _runtimesInstaled)
            {
                if (item.Name == "Microsoft.NETCore.App")
                {
                    _NETCoreRuntimes.Add(item);
                }
            }

            if (_NETCoreRuntimes.Count > 0)
            {
                _NETCoreRuntimes.Sort((Runtime version1, Runtime version2) =>
                {
                    if (version1.Version == null && version2.Version == null) return 0;
                    else if (version1.Version == null) return 1;
                    else if (version2.Version == null) return -1;
                    else return version1.Version.CompareTo(version2.Version);
                });

                foreach (var VARIABLE in _NETCoreRuntimes)
                {
                    if(VARIABLE.Major == cfgRuntime.Major)
                        if(VARIABLE.Minor >= cfgRuntime.Minor)
                            if (VARIABLE.Patch >= cfgRuntime.Patch)
                                return VARIABLE;
                }
            }

            return cfgRuntime;
        }

        static void GetPackageList(List<Runtime> runtimesInstaled)
        {
            //Console.WriteLine("GetPackageList started");
            Process proc = new Process();
            proc.StartInfo = new ProcessStartInfo($@"dotnet", "--list-runtimes");
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.EnableRaisingEvents = true;

            proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    runtimesInstaled.Add(new Runtime()
                    {
                        Name = e.Data.Split(' ')[0], Version = e.Data.Split(' ')[1],
                        Path = e.Data.Split('[')[1].Trim(']')
                    });
                }
            });

            proc.Start();
            proc.BeginOutputReadLine();

            //Задержка до получения списка (он получаестся асинхронно, что приводит программу к вылету)
            uint Count = 0;
            while (runtimesInstaled.Count < 1)
            {
                if (Count >= 5000) break;
                Thread.Sleep(12);
                Count++;
            }
        }
    }

    public class Runtime
    {
        public string Name;
        public string Version = "5.0.0";
        public string Path;
        public int Major { get { return Convert.ToInt32(Version.Split('.')[0]); } }
        public int Minor { get { return Convert.ToInt32(Version.Split('.')[1]); } }
        public int Patch { get { return Convert.ToInt32(Version.Split('.')[2]); } }
    }
}
