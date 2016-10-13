Imports System.Text.RegularExpressions

Module UnRARHelper

    Dim _log As NLog.Logger = NLog.LogManager.GetLogger("UnRARHelper")

    Function isUnRarChainComplete(ByVal _chain As UnRARChain) As Boolean

        Dim _rt As Boolean = True

        Try

            If IO.File.Exists(_chain.MasterUnRarChainFile.LocalFile) Then

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


            Else
                _rt = False
            End If


            For Each _chainmember As DownloadItem In _chain.ChainMemberFiles

                If IO.File.Exists(_chainmember.LocalFile) Then

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

                Else
                    _rt = False
                End If

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

    Private Async Function IsUnRARPasswordValid(ByVal _filename As String, ByVal _password As String) As Task(Of Boolean)

        Dim _unrar_process As Process
        Dim _unrar_exe As String
        Dim _result As Boolean = False
        Dim _tmp_output As String = String.Empty

        Try

            _unrar_exe = IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "unrar.exe")

            If IO.File.Exists(_unrar_exe) = False Then
                Throw New Exception("UnRAR Executable is missing!")
            End If

            _unrar_process = New Process

            With _unrar_process.StartInfo

                .CreateNoWindow = True
                .FileName = _unrar_exe
                .WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory
                .RedirectStandardOutput = True
                .UseShellExecute = False

                If String.IsNullOrWhiteSpace(_password) Then
                    .Arguments = String.Format("t -p- {0}", Chr(34) & _filename & Chr(34))
                Else
                    .Arguments = String.Format("t -p{0} {1}", _password, Chr(34) & _filename & Chr(34))
                End If

            End With

            _unrar_process.Start()

            Await Task.Run(Sub() _unrar_process.WaitForExit(CInt(TimeSpan.FromSeconds(5).TotalMilliseconds)))

            _unrar_process.Kill()

            _tmp_output = _unrar_process.StandardOutput.ReadToEnd.ToLower

            If (_tmp_output.Contains("testing archive") And Not _tmp_output.Contains("no files to extract")) And Not String.IsNullOrWhiteSpace(_tmp_output) Then
                _result = True
            End If


        Catch ex As Exception
            _log.Error(ex, ex.Message)
        End Try

        Return _result

    End Function

    Private Async Function DoUnRAR(ByVal _filename As String, ByVal _extract_dir As String, ByVal _password As String, ByVal _app_task As AppTask) As Task(Of Boolean)

        Dim _result As Boolean = False
        Dim _unrar_process As Process
        Dim _unrar_exe As String
        Dim _tmp_output As String
        Dim _out_lines As New List(Of String)

        Try

            _unrar_exe = IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "unrar.exe")

            If IO.File.Exists(_unrar_exe) = False Then
                Throw New Exception("UnRAR Executable is missing!")
            End If

            _unrar_process = New Process

            With _unrar_process.StartInfo

                .CreateNoWindow = True
                .FileName = _unrar_exe
                .WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory
                .RedirectStandardOutput = True
                .UseShellExecute = False

                If String.IsNullOrWhiteSpace(_password) Then
                    .Arguments = String.Format("x -o+ -p- {0} {1}", Chr(34) & _filename & Chr(34), Chr(34) & _extract_dir & Chr(34))
                Else
                    .Arguments = String.Format("x -o+ -p{0} {1} {2}", _password, Chr(34) & _filename & Chr(34), Chr(34) & _extract_dir & Chr(34))
                End If

            End With

            _unrar_process.Start()

            While _unrar_process.HasExited = False

                Dim _line As String
                Dim _percent As Integer

                _line = _unrar_process.StandardOutput.ReadLine
                _out_lines.Add(_line)

                _log.Debug(_line)

                _percent = ParseUnRarProgress(_line.Trim)

                If Not _percent = -1 Then
                    _log.Debug("{0} % entpackt", _percent)
                    _app_task.SetTaskStatus(TaskStatus.Running, String.Format("{0} - {1}% Entpackt", _filename, _percent))
                End If

            End While

            Await Task.Run(Sub() _unrar_process.WaitForExit())

            'For Each _line In _out_lines

            '    Dim _volume As String
            '    Dim _archive_file As String

            '    _log.Debug(_line)

            '    _archive_file = ParseUnRARArchiveFiles(_line)
            '    _volume = ParseUnRARVolumeFiles(_line)

            '    If Not String.IsNullOrWhiteSpace(_volume) Then
            '        _log.Debug("RAR Volume:   {0}", _volume)
            '    End If

            'Next

            _tmp_output = _unrar_process.StandardOutput.ReadToEnd

            If _tmp_output.Contains("All OK") Then
                _result = True
            End If

        Catch ex As Exception
            _log.Error(ex, ex.Message)
            _result = False
        End Try

        Return _result


    End Function

    Public Async Function UnRAR(ByVal _unrarchain As UnRARChain, ByVal _app_task As AppTask, ByVal _unrar_settings As UnRARSettings) As Task

        Dim _unrar_password As String = String.Empty

        _log.Info("Checking if a UnRar Password is needed...")

        Try

            _app_task.SetTaskStatus(TaskStatus.Running, String.Format("Cracking Password {0}", IO.Path.GetFileName(_unrarchain.MasterUnRarChainFile.LocalFile)))

            If Await IsUnRARPasswordValid(_unrarchain.MasterUnRarChainFile.LocalFile, String.Empty) = False Then

                _log.Info("Damn....we need a password to extract this sh**t")

                If _unrar_settings.UseUnRARPasswordList = False Or _unrar_settings.UnRARPasswordList.Count = 0 Then
                    Throw New Exception("We need a password to extract but i am not allowed to do any password test or there no passwords in the list")
                End If

                For Each _pw In _unrar_settings.UnRARPasswordList

                    If Await IsUnRARPasswordValid(_unrarchain.MasterUnRarChainFile.LocalFile, _pw) = True Then
                        _unrar_password = _pw
                        _log.Info(String.Format("Unrar Password Found -> {0}", _pw))
                        Exit For
                    Else
                        _log.Info(String.Format("Not luck {0} was the wrong password", _pw))
                    End If

                Next

                If String.IsNullOrWhiteSpace(_unrar_password) Then
                    Throw New Exception("No matching UnRar Password found! - Automatic UnRar Canceled")
                End If

            Else
                _log.Info("Cool thing - we don't need a password")
            End If

            _log.Info("Now passing all needed Arguments to UnRar Binary and wait the extraction to finish")

            If Await DoUnRAR(_unrarchain.MasterUnRarChainFile.LocalFile, IO.Path.GetDirectoryName(_unrarchain.MasterUnRarChainFile.LocalFile), _unrar_password, _app_task) = True Then
                _app_task.SetTaskStatus(TaskStatus.Running, "Archive erfolgreich entapckt!")

                If _unrar_settings.DeleteAfterUnRAR = True Then

                    _app_task.SetTaskStatus(TaskStatus.Running, "Lösche Archive....")

                    For Each _file In _unrarchain.ChainMemberFiles
                        IO.File.Delete(_file.LocalFile)
                    Next

                    IO.File.Delete(_unrarchain.MasterUnRarChainFile.LocalFile)

                    _app_task.SetTaskStatus(TaskStatus.RanToCompletion, "Archive erfolgreich entpackt!")


                End If

            Else
                Throw New Exception("Entpacken ist fehlgeschlagen!")
            End If

        Catch ex As Exception
            _app_task.SetTaskStatus(TaskStatus.Faulted, ex.Message)
        End Try


    End Function

End Module
