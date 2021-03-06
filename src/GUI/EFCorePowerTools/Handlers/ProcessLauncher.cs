﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace EFCorePowerTools.Handlers
{
    public class ProcessLauncher
    {
        private const string Ptexe = "efpt.exe";

        public string GetOutput(string outputPath, bool isNetCore, bool generateDdl)
        {
            var launchPath = isNetCore ? DropNetCoreFiles() : DropFiles(outputPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Path.GetDirectoryName(launchPath) ?? throw new InvalidOperationException(), Ptexe),
                Arguments = "\"" + outputPath + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            if (generateDdl)
            {
                startInfo.Arguments = "ddl \"" + outputPath + "\"";
            }

            if (isNetCore)
            {
                startInfo.WorkingDirectory = launchPath;
                startInfo.FileName = "dotnet";
                startInfo.Arguments = " efpt.dll \"" + outputPath + "\"";
                if (generateDdl)
                {
                    startInfo.Arguments = " efpt.dll ddl \"" + outputPath + "\"";
                }
            }

            var standardOutput = new StringBuilder();
            using (var process = Process.Start(startInfo))
            {
                while (process != null && !process.HasExited)
                {
                    standardOutput.Append(process.StandardOutput.ReadToEnd());
                }
                if (process != null) standardOutput.Append(process.StandardOutput.ReadToEnd());
            }
            return standardOutput.ToString();
        }

        private string DropFiles(string outputPath)
        {
            var toDir = Path.GetDirectoryName(outputPath);
            var fromDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            Debug.Assert(fromDir != null, nameof(fromDir) + " != null");
            Debug.Assert(toDir != null, nameof(toDir) + " != null");

            File.Copy(Path.Combine(fromDir, Ptexe), Path.Combine(toDir, Ptexe), true);
            File.Copy(Path.Combine(fromDir, "efpt.exe.config"), Path.Combine(toDir, "efpt.exe.config"), true);
            //TODO Handle 2.0.1 and newer!
            File.Copy(Path.Combine(fromDir, "Microsoft.EntityFrameworkCore.Design.dll"), Path.Combine(toDir, "Microsoft.EntityFrameworkCore.Design.dll"), true);

            return outputPath;
        }

        private string DropNetCoreFiles()
        {
            var toDir = Path.Combine(Path.GetTempPath(), "efpt");
            var fromDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            Debug.Assert(fromDir != null, nameof(fromDir) + " != null");
            Debug.Assert(toDir != null, nameof(toDir) + " != null");

            if (Directory.Exists(toDir))
            {
                Directory.Delete(toDir, true);
            }

            Directory.CreateDirectory(toDir);

            ZipFile.ExtractToDirectory(Path.Combine(fromDir, Ptexe + ".zip"), toDir);

            return toDir;
        }
    }
}
