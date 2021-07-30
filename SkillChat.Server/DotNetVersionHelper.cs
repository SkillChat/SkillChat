using System;
using System.Linq;

namespace SkillChat.Server
{
    public class DotNetVersionHelper
    {
        private IDotNetPackageList _iDotNetPackageList;

        public DotNetVersionHelper(IDotNetPackageList iDotNetPackageList)
        {
            _iDotNetPackageList = iDotNetPackageList;

        }
        /// <summary>
        /// Получение наиболее подходящего Runtime .NET
        /// </summary>
        public string GetNearestDotNetVersion(string targetVersion)
        {
            var target = new SemanticVersioning.Version(targetVersion);
            var range = new SemanticVersioning.Range($"^{target.Major}.{target.Minor}.x");
            var _NETCoreRuntimes = range.Satisfying(_iDotNetPackageList.PackageList, true);
            if (_NETCoreRuntimes.Count() > 0)
            {
                try
                {
                    return range.MaxSatisfying(_NETCoreRuntimes, true).ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            throw new Exception($"Нет установленных пакетов подходящих под условие {range}");
        }
    }
}
