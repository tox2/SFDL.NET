Module LogHelper

    Public Sub GenerateLogConfig(ByVal Optional _logfile As String = "")

        Dim _log_config As New NLog.Config.LoggingConfiguration
        Dim _default_log_rule As New NLog.Config.LoggingRule
        Dim _error_log_rule As New NLog.Config.LoggingRule
        Dim _console_target As New NLog.Targets.ColoredConsoleTarget

        _console_target.UseDefaultRowHighlightingRules = True
        _log_config.AddTarget("Console", _console_target)

        _default_log_rule = New NLog.Config.LoggingRule("*", NLog.LogLevel.Info, _console_target)

#Region "Debug Log"

#If DEBUG Then

        Dim _debug_log_rule As New NLog.Config.LoggingRule
        Dim _debug_target As New NLog.Targets.DebuggerTarget

        _log_config.AddTarget("Debug", _debug_target)

        _debug_log_rule = New NLog.Config.LoggingRule("*", NLog.LogLevel.Debug, _debug_target)
        _log_config.LoggingRules.Add(_debug_log_rule)

#End If

#End Region

#Region "FileLog"

        If Not String.IsNullOrWhiteSpace(_logfile) Then

            Dim _filelog_target As New NLog.Targets.FileTarget

            With _filelog_target


                .AutoFlush = True
                .FileName = _logfile
                .CreateDirs = True
                .ConcurrentWrites = True
                .KeepFileOpen = True
                .DeleteOldFileOnStartup = True

            End With

            _log_config.AddTarget("file", _filelog_target)

            _error_log_rule = New NLog.Config.LoggingRule("*", NLog.LogLevel.Info, _filelog_target) 'Nur Fehler in die Datei loggen
            _log_config.LoggingRules.Add(_error_log_rule)

        End If

#End Region

        _log_config.LoggingRules.Add(_default_log_rule)
        NLog.LogManager.Configuration = _log_config

    End Sub

End Module
