﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Consolation.Common.Framework.OptionsSystem;
using TML.Patcher.Frontend.Common;
using TML.Patcher.Frontend.Common.Options;

namespace TML.Patcher.Frontend
{
    public static class Program
    {
        public static string ExePath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

        public static ConfigurationFile Configuration { get; set; } = null!;

        public static ConsoleOptions DefaultOptions { get; set; } = null!;

        public static bool LightweightLoad { get; set; }

        public static void Main(string[] args)
        {
            Console.Title = "TMLPatcher - by convicted tomatophile";
            Thread.CurrentThread.Name = "Main";

            Consolation.Consolation.Window = new Patcher(args);

            if (Configuration.ShowIlSpyCmdInstallPrompt)
                InstallILSpyCMD();

            ConfigurationFile.Save();

            if (LightweightLoad)
            {
                UnpackModOption.PerformExtraction(args[0]);
                return;
            }

            Consolation.Consolation.Window.WriteStaticText(false);
            Consolation.Consolation.GetWindow<Patcher>().CheckForUndefinedPath();
            Consolation.Consolation.SelectedOptionSet.ListForOption();
        }

        private static void InstallILSpyCMD()
        {
            Configuration.ShowIlSpyCmdInstallPrompt = false;

            Patcher window = Consolation.Consolation.GetWindow<Patcher>();

            window.WriteLine("Do you want to install ilspycmd?");
            window.WriteLine("<y/n>");

            ConsoleKeyInfo pressedKey = Console.ReadKey();
            window.WriteLine();

            if (pressedKey.Key != ConsoleKey.Y)
                return;

            const string dotNetCommand = "dotnet tool install ilspycmd -g";

            window.WriteLine("Attempting to install ilspycmd...");

            Process process = new();

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                case PlatformID.WinCE:
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/C " + dotNetCommand,
                        UseShellExecute = false
                    };
                    break;

                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = "-c \" " + dotNetCommand + " \"",
                        UseShellExecute = false
                    };
                    break;

                case PlatformID.Xbox:
                case PlatformID.Other:
                    window.WriteLine("Current platform is not supported.");
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            process.Start();
            process.WaitForExit();
        }

        internal static void PreLoadAssemblies()
        {
            List<Assembly> loaded = AppDomain.CurrentDomain.GetAssemblies().ToList();

            Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll")
                .Where(x => !loaded
                    .Select(y => y.Location)
                    .Contains(x, StringComparer.InvariantCultureIgnoreCase))
                .ToList()
                .ForEach(z => loaded
                    .Add(AppDomain.CurrentDomain
                        .Load(AssemblyName.GetAssemblyName(z))));
        }
    }
}