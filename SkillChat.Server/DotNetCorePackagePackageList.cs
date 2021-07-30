using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace SkillChat.Server
{
    class DotNetCorePackagePackageList : IDotNetPackageList
    {
        private ReadOnlyCollection<SemanticVersioning.Version> list = null;

        public IEnumerable<SemanticVersioning.Version> PackageList { get
        {
            if (list == null)
            {
                list = new ReadOnlyCollection<Version>(GetNETCorePackageList().ToList());
            }
            return list;
        } }

        private IEnumerable<SemanticVersioning.Version> GetNETCorePackageList()
        {
            List<SemanticVersioning.Version> runtimesNETCoreInstaled = new List<SemanticVersioning.Version>();

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
                        runtimesNETCoreInstaled.Add(new SemanticVersioning.Version(match.Groups["Version"].Value));
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
