using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Demo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            ConfigureLogs();
        }

        private void ConfigureLogs()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = "hierarchygrid.log",
                DeleteOldFileOnStartup = true
            };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logfile);

            // Apply config
            NLog.LogManager.Configuration = config;
        }
    }
}