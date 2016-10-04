Imports System.Text.RegularExpressions

Module UnRARHelper

    Dim _log As NLog.Logger = NLog.LogManager.GetLogger("UnRARHelper")

    Function isUnRarChainComplete(ByVal _chain As UnRARChain) As Boolean

        Dim _rt As Boolean = True

        Try


            Select Case _chain.MasterUnRarChainFile.DownloadStatus

                Case DownloadItem.Status.Completed
                    'ok

                Case DownloadItem.Status.Completed_HashInvalid
                    'ok

                Case DownloadItem.Status.Completed_HashValid
                    'ok

                Case Else

                    _rt = False

            End Select


            For Each _chainmember As DownloadItem In _chain.ChainMemberFiles

                Select Case _chainmember.DownloadStatus

                    Case DownloadItem.Status.Completed
                    'ok

                    Case DownloadItem.Status.Completed_HashInvalid
                    'ok

                    Case DownloadItem.Status.Completed_HashValid
                        'ok
                    Case Else
                        _rt = False

                End Select

            Next


        Catch ex As Exception
            _log.Error(ex, ex.Message)
            _rt = False
        End Try

        Return _rt


    End Function

    Friend Function ParseUnRarProgress(ByVal _line As String) As Integer

        Dim _percent As String = String.Empty
        Dim sourcestring As String = _line
        Dim _percent_int As Integer = -1

        Dim re As Regex = New Regex("[0-9]{1,3}%")
        Dim mc As MatchCollection = re.Matches(sourcestring)
        Dim mIdx As Integer = 0

        For Each m As Match In mc
            For groupIdx As Integer = 0 To m.Groups.Count - 1
                If Not String.IsNullOrEmpty(m.Value.ToString) Then
                    _percent = m.Value.ToString.Trim.Replace("%", "")
                    _percent_int = Integer.Parse(_percent)
                End If
            Next
            mIdx = mIdx + 1
        Next

        Return _percent_int

    End Function

    Friend Function ParseUnRARVolumeFiles(ByVal _line As String) As String

        Dim _volume As String = String.Empty
        Dim sourcestring As String = _line

        If _line.Contains(".rar") Or _line.Contains(".part") Then

            Dim re As Regex = New Regex("^Extracting[ ]{1,2}.{1,255}\..{1,3}")
            Dim mc As MatchCollection = re.Matches(sourcestring)
            Dim mIdx As Integer = 0

            For Each m As Match In mc
                For groupIdx As Integer = 0 To m.Groups.Count - 1
                    If Not String.IsNullOrEmpty(m.Value.ToString) Then
                        _volume = m.Value.ToString.Replace("Extracting from", "").Trim
                    End If
                Next
                mIdx = mIdx + 1
            Next

        End If

        Return _volume

    End Function

    Friend Function ParseUnRARArchiveFiles(ByVal _line As String) As String

        Dim _archive_file As String = String.Empty
        Dim sourcestring As String = _line

        If Not _line.StartsWith("Extracting from") Then

            Dim re As Regex = New Regex("^Extracting[ ]{1,2}.{1,255}\..{1,3}")
            Dim mc As MatchCollection = re.Matches(sourcestring)
            Dim mIdx As Integer = 0

            For Each m As Match In mc
                For groupIdx As Integer = 0 To m.Groups.Count - 1
                    If Not String.IsNullOrEmpty(m.Value.ToString) Then
                        _archive_file = m.Value.ToString.Replace("Extracting", "").Trim
                    End If
                Next
                mIdx = mIdx + 1
            Next

        End If

        Return _archive_file


    End Function

End Module
