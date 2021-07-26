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
                try
                {
                    //проверка на наличае точного совпадения
                    var version = _NETCoreRuntimes.FirstOrDefault(v => v == cfgVersion);
                    if (version != null) return version.ToString();

                    //первая подходящая
                    return _NETCoreRuntimes.First(v => v.Major == cfgVersion.Major && (v.Minor == cfgVersion.Minor || v.Minor == cfgVersion.Minor + 1)).ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
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
