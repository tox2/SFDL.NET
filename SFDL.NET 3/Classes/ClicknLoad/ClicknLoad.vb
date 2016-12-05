Public Class ClicknLoad

    Public Async Function ProcessClicknLoad(ByVal _url As String) As Task(Of String)

        Dim _cnl_xup As New ClicknLoad_xup
        Dim _cnl_generic As New ClicknLoad_generic
        Dim _result As String = String.Empty

        Select Case True

            Case _url Like "*xup.to*"

                _result = Await _cnl_xup.DownloadSFDL(_url)

            Case _url Like "*xup.in*"

                _result = Await _cnl_xup.DownloadSFDL(_url)

            Case Else

                'Try to download directly

                _result = Await _cnl_generic.DownloadSFDL(_url)

        End Select

        Return _result

    End Function

End Class
