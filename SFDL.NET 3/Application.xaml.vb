

Class Application

    Private Shared ReadOnly SingleInstance As New SingleInstance(New Guid("fb71faa3-4891-4525-80f4-a2085174df7e"))

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup

        Dim _settings As New Settings
        Dim _settings_xml_path As String = String.Empty
        Dim _log As NLog.Logger = Nothing

        Try

            If SingleInstance.IsFirstInstance Then

                AddHandler SingleInstance.ArgumentsReceived, AddressOf SingleInstanceParameter

                If e.Args.Where(Function(myarg) myarg.ToLowerInvariant.ToLower.Equals("/log")).Count = 1 Then
                    LogHelper.GenerateLogConfig(IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "log.log"))
                Else
                    LogHelper.GenerateLogConfig()
                End If



                My.Settings.Upgrade()

                _log = NLog.LogManager.GetLogger("Startup")

                _settings_xml_path = IO.Path.Combine(Environment.GetEnvironmentVariable("appdata"), "SFDL.NET 3\settings.xml")

                If IO.Directory.Exists(IO.Path.GetDirectoryName(_settings_xml_path)) = False Then
                    IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(_settings_xml_path))
                End If

                If IO.File.Exists(_settings_xml_path) Then
                    _settings = CType(XMLHelper.XMLDeSerialize(_settings, _settings_xml_path), Settings)
                Else
                    _settings = Settings.InitNewSettings
                    XMLHelper.XMLSerialize(_settings, _settings_xml_path)
                End If

                Application.Current.Resources.Add("Settings", _settings)
                Application.Current.Resources.Add("DownloadStopped", False)
                Application.Current.Resources.Add("UnRARBlock", False)

                If _settings.PreventStandby = True Then
                    StandyHandler.PreventStandby()
                    _log.Info("System Standby is now blocked")
                End If

                SingleInstance.ListenForArgumentsFromSuccessiveInstances()

            Else
                ' if there is an argument available, fire it
                If e.Args.Length > 0 Then
                    SingleInstance.PassArgumentsToFirstInstance(e.Args)
                End If

                Environment.[Exit](0)

            End If

        Catch ex As Exception

            If Not IsNothing(_log) Then
                _log.Fatal(ex, ex.Message)
            Else
                Console.WriteLine(ex.ToString)
            End If

        End Try

    End Sub

    Private Shared Sub SingleInstanceParameter(sender As Object, e As ArgumentsReceivedEventArgs)

        For Each _item In e.Args

            If Not String.IsNullOrWhiteSpace(_item) Then

                DispatchService.DispatchService.Invoke(Sub()
                                                           MainViewModel.ThisInstance.OpenSFDLFile(_item)
                                                       End Sub)

            End If
        Next

    End Sub

    Private Sub Application_Exit(sender As Object, e As ExitEventArgs) Handles Me.[Exit]


    End Sub

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

End Class
