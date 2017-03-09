Imports System.Net.Http

Module UpdateCheck

    Private _log As NLog.Logger = NLog.LogManager.GetLogger("UpdateCheck")
    Const UPDATEFILE As String = "https://raw.githubusercontent.com/n0ix/SFDL.NET/master/SFDL.NET%203/UpdateCheck/Version.txt"

    Private Async Function DownloadVersionFile(ByVal _url As String) As Task(Of String)

        Dim _http_client As HttpClient = New HttpClient
        Dim _http_response As HttpResponseMessage
        Dim _content As HttpContent
        Dim _local_tmp_filepath As String = String.Empty

        Try

            _http_client.BaseAddress = New Uri(_url)
            ' _http_client.Timeout = TimeSpan.FromSeconds(_timeout)

            _http_response = Await _http_client.GetAsync(_url)

            _http_response.EnsureSuccessStatusCode()

            Await _http_response.Content.LoadIntoBufferAsync()

            _content = _http_response.Content

            _local_tmp_filepath = IO.Path.Combine(My.Computer.FileSystem.SpecialDirectories.Temp, IO.Path.GetRandomFileName)

            Using _filestream As New IO.FileStream(_local_tmp_filepath, IO.FileMode.Create, IO.FileAccess.Write)

                Await _content.CopyToAsync(_filestream)

            End Using

        Catch ex As Exception

        End Try

        Return _local_tmp_filepath

    End Function

    Public Async Function IsNewUpdateAvailible() As Task(Of Boolean)

        Dim _current_app_version As Version = My.Application.Info.Version
        Dim _update_app_version As Version
        Dim _tmp_file As String = String.Empty
        Dim _rt As Boolean = False

        Try

            _tmp_file = Await DownloadVersionFile(UPDATEFILE)

            _update_app_version = Version.Parse(My.Computer.FileSystem.ReadAllText(_tmp_file).ToString.Trim)

            If _current_app_version.CompareTo(_update_app_version) = -1 Then '1 = älter 0=gleich -1=neuer
                _log.Info("New version is availible")
                _rt = True
            Else
                _log.Info("No new version availible!")
            End If

        Catch ex As Exception
            _log.Error(ex, ex.Message)
        Finally
            If IO.File.Exists(_tmp_file) Then
                IO.File.Delete(_tmp_file)
            End If
        End Try

        Return _rt

    End Function

End Module
