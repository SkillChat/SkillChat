using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace SkillChat.Server
{
    public class DotNetVersionHelper
    {
        /// <summary>
        /// Получение наиболее подходящего Runtime .NET
        /// </summary>
        public string GetNearestDotNetVersion(string targetVersion)
        {
            var cfgVersion = new Version(targetVersion);

            List<Version> _NETCoreRuntimes = new List<Version>();

            _NETCoreRuntimes = GetNETCorePackageList();

            if (_NETCoreRuntimes.Count > 0)
            {
                _NETCoreRuntimes.Sort((Version version1, Version version2) =>
                {
                    if (version1 == null && version2 == null) return 0;
                    else if (version1 == null) return 1;
                    else if (version2 == null) return -1;
                    else return version1.CompareTo(version2);
                });

                foreach (var netCoreVersion in _NETCoreRuntimes)
                {
                    if(netCoreVersion >= cfgVersion)
                        return netCoreVersion.ToString();
                }
                return _NETCoreRuntimes.Last().ToString();
            }

            return targetVersion;
        }



        public List<Version> GetNETCorePackageList()
        {
            List<Version> runtimesNETCoreInstaled = new List<Version>();

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
                    var match = Regex.Match(e.Data, "(?<Name>Microsoft\\.NETCore\\.App)\\s+(?<Version>([0-9a-z-]+\\.)+[0-9a-z-]+)");
                    if (match.Success)
                    {
                        runtimesNETCoreInstaled.Add(new Version(match.Groups["Version"].Value));
                    }
                }
            });

            proc.Start();
            proc.BeginOutputReadLine();
            //Задержка до получения списка (он получаестся асинхронно, что приводит программу к вылету)
            proc.WaitForExit();
            return runtimesNETCoreInstaled;
        }
    }
}
