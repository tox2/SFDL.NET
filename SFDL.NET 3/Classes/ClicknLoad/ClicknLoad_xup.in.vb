Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions

Public Class ClicknLoad_xup

#Region "Interne Plugin Funktionen"

    Private Async Function GetResponse(ByVal url As String) As Task(Of String)

        Dim _http_client As HttpClient = New HttpClient
        Dim _http_response As HttpResponseMessage

        _http_client.BaseAddress = New Uri(url)
        '_http_client.Timeout = TimeSpan.FromSeconds(_timeout)

        _http_response = Await _http_client.GetAsync(url)

        _http_response.EnsureSuccessStatusCode()

        Return Await _http_response.Content.ReadAsStringAsync
    End Function

    Private Function GetHostID(ByVal _web_response As String) As String

        Dim _rt As String = ""
        Dim sourcestring As String = _web_response
        Dim re As Regex = New Regex("(www)(\d+).*?(xup)(\.)((?:[a-z][a-z0-9_]*))")
        Dim mc As MatchCollection = re.Matches(sourcestring)
        Dim mIdx As Integer = 0

        For Each m As Match In mc
            For groupIdx As Integer = 0 To m.Groups.Count - 1
                If Not String.IsNullOrEmpty(m.Value.ToString) Then
                    _rt = m.Value.ToString
                End If
            Next
            mIdx = mIdx + 1
        Next

        Return _rt


    End Function

    Private Function GetvID(ByVal _web_response As String) As String

        Dim _rt As String = ""
        Dim sourcestring As String = _web_response
        Dim re As Regex = New Regex("[a-z0-9]{34}")
        Dim mc As MatchCollection = re.Matches(sourcestring)
        Dim mIdx As Integer = 0

        For Each m As Match In mc
            For groupIdx As Integer = 0 To m.Groups.Count - 1
                If Not String.IsNullOrEmpty(m.Value.ToString) Then
                    _rt = m.Value.ToString
                End If
            Next
            mIdx = mIdx + 1
        Next

        Return _rt

    End Function

    Private Function GetvTime(ByVal _web_response As String) As String

        Dim _rt As String = ""
        Dim sourcestring As String = _web_response
        Dim re As Regex = New Regex("[0-9]{10}" & Chr(34))
        Dim mc As MatchCollection = re.Matches(sourcestring)
        Dim mIdx As Integer = 0

        For Each m As Match In mc
            For groupIdx As Integer = 0 To m.Groups.Count - 1
                If Not String.IsNullOrEmpty(m.Value.ToString) Then
                    _rt = m.Value.ToString.Replace(Chr(34), "")
                End If
            Next
            mIdx = mIdx + 1
        Next

        Return _rt

    End Function

#End Region

    Public Async Function DownloadSFDL(_url As String) As Task(Of String)

        Dim _http_client As HttpClient = New HttpClient
        Dim _http_response As HttpResponseMessage
        Dim _content As HttpContent
        Dim _local_tmp_filepath As String = String.Empty
        Dim _post_values As New List(Of KeyValuePair(Of String, String))
        Dim _http_post_content As HttpContent

        Dim _tmp_response As String = String.Empty
        Dim _web_request_url As String = String.Empty

        Dim _vid As String = String.Empty
        Dim _vtime As String = String.Empty
        Dim _fid As String = String.Empty
        Dim _filename As String = String.Empty
        Dim _host_id As String = String.Empty

        Try

            Debug.WriteLine("RAW Link:" & _url)

            'FID ermitteln

            _fid = _url.Remove(0, _url.IndexOf(","))
            _fid = _fid.Remove(_fid.IndexOf("/"))
            _fid = _fid.Remove(0, 1)
            _fid = _fid.Trim

            Debug.WriteLine("FID=" & _fid)

            'Dateiname ermitteln

            _filename = _url.Remove(_url.Length - 1)
            _filename = _filename.Remove(0, _filename.LastIndexOf("/"))
            _filename = _filename.Remove(0, 1)
            _filename = _filename.Trim

            Debug.WriteLine("Filename=" & _filename)


            _tmp_response = Await GetResponse(_url)

            _vid = GetvID(_tmp_response)
            _vtime = GetvTime(_tmp_response)
            _host_id = "http://" & GetHostID(_tmp_response)

            Debug.WriteLine("VID=" & _vid)
            Debug.WriteLine("vTIME=" & _vtime)
            Debug.WriteLine("HostID=" & _host_id)

            'Web Request URL zusammenbauen

            _web_request_url = String.Format("{0}/exec/xddl.php?fid={1}&fname={2}&uid=0&key=", _host_id, _fid, _filename)

            Debug.WriteLine("Web Request URL=" & _web_request_url)

            _http_client.BaseAddress = New Uri(_web_request_url)

            _http_response = Await _http_client.GetAsync(_web_request_url)

            _http_response.EnsureSuccessStatusCode()

            _post_values.Add(New KeyValuePair(Of String, String)("vid", _vid))
            _post_values.Add(New KeyValuePair(Of String, String)("vtime", _vtime))

            _http_post_content = New FormUrlEncodedContent(_post_values)

            _http_response = Await _http_client.PostAsync(_web_request_url, _http_post_content)

            _http_response.EnsureSuccessStatusCode()

            Await _http_response.Content.LoadIntoBufferAsync()

            _content = _http_response.Content

            _local_tmp_filepath = IO.Path.Combine(My.Computer.FileSystem.SpecialDirectories.Temp, IO.Path.GetRandomFileName)

            Using _filestream As New FileStream(_local_tmp_filepath, FileMode.Create, FileAccess.Write)

                Await _content.CopyToAsync(_filestream)

            End Using

        Catch ex As Exception
            _local_tmp_filepath = ""
        End Try

        Return _local_tmp_filepath

    End Function

End Class
