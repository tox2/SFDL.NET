Module UpdateCheck

    Private _log As NLog.Logger = NLog.LogManager.GetLogger("UpdateCheck")

    Public Function IsNewUpdateAvailible() As Boolean

        Const UPDATEFILE As String = "https://sfdl.svn.codeplex.com/svn/SFDL.NET 2/UpdateCheck/version"

        Dim _current_app_version As Version = My.Application.Info.Version
        Dim _update_app_version As Version
        Dim _tmp_file As String = IO.Path.GetTempFileName
        Dim _rt As Boolean = False

        Try

            My.Computer.Network.DownloadFile(UPDATEFILE, _tmp_file)

            _update_app_version = Version.Parse(My.Computer.FileSystem.ReadAllText(_tmp_file).ToString.Trim)

            If _current_app_version.CompareTo(_update_app_version) = -1 Then '1 = älter 0=gleich -1=neuer
                _log.Info("Neue Version ist Online")
                _rt = True
            Else
                _log.Info("Keine neue Version verfügbar!")
            End If

        Catch ex As Exception
            _log.Error(ex, ex.Message)
        Finally
            IO.File.Delete(_tmp_file)
        End Try

        Return _rt

    End Function

End Module
