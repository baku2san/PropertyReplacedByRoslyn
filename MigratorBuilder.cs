using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using System.IO;
using Microsoft.Build.Logging;
using System.Diagnostics;

namespace ModelCreator.MigratorControllers
{
    /// <summary>
    ///  MSBuild で別プロジェクトをコード実行中にBuildする試行。
    /// </summary>
    public class MigratorBuilder
    {
        private string ProjectRootPath = @"TargetPath";
        private string ProjectName = @"AutoMigrate";

        public MigratorBuilder()
        {
            
        }
        public bool Execute()
        {
            return ExecuteMigrator(BuildMigrator());
        }
        private bool ExecuteMigrator(FileInfo exeFile)
        {
            if (exeFile != null)
            {
                System.Diagnostics.Process.Start(exeFile.FullName);
                return true;
            }
            return false;
        }
        public FileInfo BuildMigrator()
        {
            // tools version が Bin の前のだから、"DefaultToolsVersion = 15.0 にしたいんだけど駄目。 対象PCにはInstallされてないけど、下記FolderにCopy済。何故？
            // 2.0/3.5/4.0 ならいけるって感じなので、対象PCのC:\Windows\Microsoft.NET\Framework　にあるVersionの可能性もある？？
            // インストール済みとしては、4.71 になってるんだけど・・ツールだからまた違う？？
            var msbuildRoot = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild";　// default はここにInstallされる VS2017 だと
            var msbuildToolsPath = Path.Combine(msbuildRoot, @"15.0\Bin");
            var msbuildExe = Path.Combine(msbuildRoot, @"15.0\Bin\MsBuild.exe");
            var sdkPath = Path.Combine(msbuildRoot, "Sdks");
            Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", msbuildExe);
            Environment.SetEnvironmentVariable("MSBUILDSDKSPATH", sdkPath);
            Environment.SetEnvironmentVariable("MSBuildExtensionsPath", msbuildRoot);
            // Environment.SetEnvironmentVariable("MSBuildToolsPath", msbuildRoot); これは要らないんだけどいれてたので削除
            Debug.Print(Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH"));
            Debug.Print(Environment.GetEnvironmentVariable("MSBUILDSDKSPATH"));
            Debug.Print(Environment.GetEnvironmentVariable("MSBuildExtensionsPath"));


            var projectFilePath = Path.Combine(ProjectRootPath, ProjectName + ".csproj");
            Debug.Print("Project file exists: " + new FileInfo(projectFilePath).Exists);
            var target = "Debug";
            target = "Release";
            var outputPath = Path.Combine(@"bin", target);

            var pc = new ProjectCollection();
            var logger = new ConsoleLogger();
            pc.Loggers.Add(logger);
            pc.DefaultToolsVersion = "4.0";
            //            pc.DefaultToolsVersion = "15.0";  "The tools version "15.0" is unrecognized. Available tools versions are "2.0", "3.5", "4.0".と

            var GlobalProperty = new Dictionary<string, string>();
            GlobalProperty.Add("Configuration", target);
            GlobalProperty.Add("Platform", "x86");
            GlobalProperty.Add("OutputPath", outputPath);

            var BuidlRequest = new BuildRequestData(projectFilePath, GlobalProperty, null, new string[] { "Build" }, null);
            var buildParameters = new BuildParameters(pc)
            {
                OnlyLogCriticalEvents = false,
                DetailedSummary = true,
                Loggers = new List<Microsoft.Build.Framework.ILogger> { logger }.AsEnumerable()
            };

            var buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, BuidlRequest);
            Debug.Print(buildResult.OverallResult.ToString());

            if (buildResult.OverallResult == BuildResultCode.Success)
            {
                var exeFullPath = new FileInfo(Path.Combine(this.ProjectRootPath, outputPath, ProjectName + ".exe"));
                if (exeFullPath.Exists)
                {
                    return exeFullPath;
                }
            }
            return null;
        }
    }
}
