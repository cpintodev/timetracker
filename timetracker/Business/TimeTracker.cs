﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace timetracker
{
    public class TimeTracker
    {
        SessionLog _sessionLog = new SessionLog();
        private HashSet<string> _scannedProcesses = new HashSet<string>();

        public void StartTimeTracker(List<string> processesToWatch)
        {
            if (File.Exists("session_log.json"))
            {
                _sessionLog = JsonConvert.DeserializeObject<SessionLog>(File.ReadAllText("session_log.json"));
            }
            var processScanner = new ProcessScanner(processesToWatch);
            processScanner.ProcessFound += ProcessFound;
            processScanner.ScanProcesses();
        }

        private void ProcessFound(object sender, ProcessFoundEventArgs e)
        {
            if (_scannedProcesses.Contains(e.ProcessWrapper.GetProcessName()))
            {
                return;
            }
            var processWatcher = new ProcessWatcher(e.ProcessWrapper);
            var processSession = new ProcessSession(processWatcher)
            {
                SessionName = processWatcher.ProcessWrapper.GetProcessName(),
                ActiveTimeLimit = TimeSpan.FromSeconds(5)
            };
            processSession.SessiongEnded += SessionEnded;
            processSession.ActiveThresholdReached += ActiveThresholdTimeReacher;
            processSession.StartSession();
            _scannedProcesses.Add(e.ProcessWrapper.GetProcessName());
        }

        private void SessionEnded(object sender, SessionEndedEventArgs e)
        {
            _scannedProcesses.Remove(e.ProcessSession.ProcessWatcher.ProcessWrapper.GetProcessName());
            _sessionLog.SquashSession(DateTime.Today.ToShortDateString(), e.ProcessSession);
            // Persisting
            var json = JsonConvert.SerializeObject(_sessionLog);
            Console.WriteLine(json);
            File.WriteAllText("session_log.json", json);
        }

        private void ActiveThresholdTimeReacher(object sender, ActiveThresholdReachedEventArgs e)
        {
            Console.WriteLine($"You've been playing for {e.ActiveTime}");
        }
    }
}
