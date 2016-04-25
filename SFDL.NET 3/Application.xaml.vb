Class Application
    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup

        Dim _settings As New Settings
        Dim _settings_xml_path As String = String.Empty

        Try

            'ToDo: Load Settings From XML File
            _settings_xml_path = IO.Path.Combine(Environment.GetEnvironmentVariable("appdata"), "SFDL.NET 3\settings.xml")

            If IO.Directory.Exists(IO.Path.GetDirectoryName(_settings_xml_path)) = False Then
                IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(_settings_xml_path))
            End If

            If IO.File.Exists(_settings_xml_path) Then
                _settings = CType(XMLHelper.XMLDeSerialize(_settings, _settings_xml_path), Settings)
            End If

            Application.Current.Resources.Add("Settings", _settings)

        Catch ex As Exception
            'ToDo:Logging!
        End Try

    End Sub

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

End Class
