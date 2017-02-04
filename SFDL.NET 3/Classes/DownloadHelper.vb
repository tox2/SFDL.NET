
Imports Amib.Threading
Imports ArxOne.Ftp.Exceptions

Class DownloadHelper

    Private _log As NLog.Logger = NLog.LogManager.GetLogger("DownloadHelper")
    Private _ftp_client_list As New Dictionary(Of Guid, ArxOne.Ftp.FtpClient)
    Private _ftp_session_list As New Dictionary(Of Guid, ArxOne.Ftp.FtpSession)
    Private _obj_ftp_client_list_lock As New Object
    Private _settings As New Settings


    Public Sub New()
        _settings = Application.Current.Resources("Settings")
    End Sub

    Public Event ServerFull(ByVal _item As DownloadItem)

#Region "Private Subs"

    Private Sub ThrottleByteTransfer(maxBytesPerSecond As Integer, bytesTotal As Long, elapsed As TimeSpan, bytesPerSec As Integer)
        ' we only throttle if the maxBytesPerSecond is not zero (zero turns off the throttle)
        If maxBytesPerSecond > 0 Then
            ' we only throttle if our through-put is higher than what we want
            If bytesPerSec > maxBytesPerSecond Then
                Dim elapsedMilliSec As Double = If(elapsed.TotalSeconds = 0, elapsed.TotalMilliseconds, elapsed.TotalSeconds * 1000)

                ' need to calc a delay in milliseconds for the throttle wait based on how fast the 
                ' transfer is relative to the speed it needs to be
                Dim millisecDelay As Double = (bytesTotal / (maxBytesPerSecond / 1000) - elapsedMilliSec)

                ' can only sleep to a max of an Int32 so we need to check this since bytesTotal is a long value
                ' this should never be an issue but never say never
                If millisecDelay > 10000 Then
                    millisecDelay = 10000
                End If

                ' go to sleep
                System.Threading.Thread.Sleep(CInt(millisecDelay))
            End If
        End If
    End Sub

    Private Sub GetItemFileSize(ByRef _item As DownloadItem, ByVal _ftp_session As ArxOne.Ftp.FtpSession)

        Try

            For Each _ftpitem In ArxOne.Ftp.FtpClientUtility.List(_ftp_session.Connection.Client, _item.DirectoryPath, _ftp_session)

                Try

                    Dim _entry As ArxOne.Ftp.FtpEntry

                    _entry = FTPHelper.TryParseLine(_ftpitem, _item.DirectoryPath)

                    If _entry.Name.Equals(_item.FileName) Then
                        _item.FileSize = _entry.Size
                    End If

                Catch ex As Exception
                    _log.Debug(ex.Message)
                End Try

            Next

        Catch ex As Exception
            _log.Error(ex.Message)
        End Try

    End Sub

    Private Sub ParseFTPException(ByVal ex As ArxOne.Ftp.Exceptions.FtpException, ByVal _item As DownloadItem)

        Dim _err_message As String = String.Empty


        If Not IsNothing(ex.Message) Then
            _err_message = ex.Message
        End If

        If Not IsNothing(ex.InnerException) Then
            _err_message = _err_message & ex.InnerException.Message
        End If

        If String.IsNullOrWhiteSpace(_err_message) = False Then

            _item.DownloadStatus = NET3.DownloadItem.Status.Failed

            If _err_message.Contains("Code=421") Then ' Service not available, closing control connection. This may be a reply to any command if the service knows it must shut down.

                If _err_message.ToLower.Contains("maximum login limit has been reached.") Then
                    _item.DownloadStatus = NET3.DownloadItem.Status.Failed_ServerFull
                    _item.RetryPossible = True
                End If

                If _err_message.ToLower.Contains("Not logged in, only sessions from same IP allowed concurrently") Then
                    _item.DownloadStatus = NET3.DownloadItem.Status.Failed_ServerFull
                    _item.RetryPossible = True
                End If

                If _item.DownloadStatus = NET3.DownloadItem.Status.Failed Then
                    _item.DownloadStatus = NET3.DownloadItem.Status.Failed_ServerDown
                End If

            End If

            If _err_message.Contains("Code=425") Then '  Can't open data connection.
                _item.DownloadStatus = NET3.DownloadItem.Status.Failed_ConnectionError
                _item.RetryPossible = True
            End If

            If _err_message.Contains("Code=426") Then 'Connection closed; transfer aborted.
                _item.DownloadStatus = NET3.DownloadItem.Status.Failed_ConnectionError
                _item.RetryPossible = True
            End If

            If _err_message.Contains("Code=430") Then ' Invalid username or password
                _item.DownloadStatus = NET3.DownloadItem.Status.Failed_AuthError
                _item.RetryPossible = True
            End If

            If _err_message.Contains("Code=434") Then 'Requested host unavailable.
                _item.DownloadStatus = NET3.DownloadItem.Status.Failed_ServerDown
                _item.RetryPossible = True
            End If

            If _err_message.Contains("Code=450") Then 'Requested file action not taken.
                _item.DownloadStatus = NET3.DownloadItem.Status.Failed_FileNotFound
                _item.RetryPossible = False
            End If

            If _err_message.Contains("Code=451") Then 'Requested action aborted. Local error in processing
                _item.DownloadStatus = NET3.DownloadItem.Status.Failed_FileNotFound
                _item.RetryPossible = False
            End If

            If _err_message.Contains("Code=452") Then 'Requested action aborted. Local error in processing
                _item.DownloadStatus = NET3.DownloadItem.Status.Failed_FileNotFound
                _item.RetryPossible = False
            End If

            If _err_message.Contains("Code=501") Then 'Syntax error in parameters or arguments.
                _item.DownloadStatus = NET3.DownloadItem.Status.Failed_DirectoryNotFound
                _item.RetryPossible = False
            End If

            If _err_message.Contains("Code=550") Then 'Requested action not taken. File unavailable (e.g., file not found, no access).
                _item.DownloadStatus = NET3.DownloadItem.Status.Failed_FileNotFound
                _item.RetryPossible = False
            End If

            If _err_message.Contains("Code=530") Then 'Not Logged in
                _item.DownloadStatus = NET3.DownloadItem.Status.Failed_AuthError
                _item.RetryPossible = True
            End If

        Else

            _item.DownloadStatus = NET3.DownloadItem.Status.Failed

        End If

    End Sub


#End Region

    Sub DisposeFTPClients()

        For Each _client In _ftp_client_list
            _client.Value.Dispose()
        Next

    End Sub

    Function DownloadContainerItem(_item As DownloadItem, ByVal _download_dir As String, ByVal _connection_info As SFDL.Container.Connection, ByVal _single_session_mode As Boolean) As DownloadItem

        Dim _ftp_session As ArxOne.Ftp.FtpSession = Nothing
        Dim _ftp_client As ArxOne.Ftp.FtpClient = Nothing

        Try

            If SmartThreadPool.IsWorkItemCanceled = True Or Application.Current.Resources("DownloadStopped") = True Then
                Throw New DownloadStoppedException("Canceld!")
            End If

            SyncLock _obj_ftp_client_list_lock

                'Check if any FTP Client Exits for this Parent Container Session
                If Not _ftp_client_list.ContainsKey(_item.ParentContainerID) Then
                    SetupFTPClient(_ftp_client, _connection_info)
                    _ftp_client_list.Add(_item.ParentContainerID, _ftp_client)
                Else
                    _ftp_client = _ftp_client_list(_item.ParentContainerID)
                End If

                If _single_session_mode = True Then

                    If _ftp_session_list.ContainsKey(_item.ParentContainerID) Then
                        _ftp_session = _ftp_session_list(_item.ParentContainerID)
                    Else
                        _ftp_session = _ftp_client.Session
                        _ftp_session_list.Add(_item.ParentContainerID, _ftp_session)
                    End If

                Else
                    _ftp_session = _ftp_client.Session

                    If _ftp_session_list.ContainsKey(_item.ParentContainerID) Then
                        _ftp_session_list.Add(Guid.NewGuid, _ftp_session)
                    Else
                        _ftp_session_list.Add(_item.ParentContainerID, _ftp_session)
                    End If

                End If

            End SyncLock

            'ToDo: Prüfen ob Verbindung zum Server hergestellt werden kann ->> Fehlerbehandlung

            DownloadItem(_item, _ftp_session)

        Catch ex As DownloadStoppedException
            _log.Info("Download Stopped")
            _item.DownloadStatus = NET3.DownloadItem.Status.Stopped

        Catch ex As AggregateException
            _log.Error(ex, ex.Message)
            _item.DownloadStatus = NET3.DownloadItem.Status.Failed_AuthError

        Catch ex As Exception
            _log.Error(ex, ex.Message)
            _item.DownloadStatus = NET3.DownloadItem.Status.Failed_AuthError

            If ex.GetType Is GetType(FtpException) Then
                ParseFTPException(ex, _item)
            End If

        Finally
            PostDownload(_item, _ftp_session)
        End Try

        Return _item

    End Function

    Private Sub DownloadItem(ByVal _item As DownloadItem, ByVal _ftp_session As ArxOne.Ftp.FtpSession)

        Dim _filemode As IO.FileMode
        Dim _restart As Long = 0
        Dim _disk_free_space As Long

        'IO Stream Variablen
        Const Length As Integer = 256
        Dim buffer As [Byte]()
        Dim bytesRead As Integer = 0
        Dim bytestotalread As Integer = 0
        Dim _starttime As DateTime = DateTime.Now

        Dim _percent_downloaded As Integer = 0
        Dim _ctime As TimeSpan
        Dim elapsed As TimeSpan
        Dim bytesPerSec As Integer = 0
        Dim _skip_download As Boolean = False


        Try

            If SmartThreadPool.IsWorkItemCanceled = True Or Application.Current.Resources("DownloadStopped") = True Then
                Throw New Exception("Canceld")
            End If

            If String.IsNullOrWhiteSpace(_item.LocalFile) Then
                Throw New Exception("Dateipfad ist leer!")
            End If

            If IO.Directory.Exists(IO.Path.GetDirectoryName(_item.LocalFile)) = False Then
                _log.Warn("Ziel Verzeichnis existiert nicht - erstelle")
                IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(_item.LocalFile))
            End If

            If _item.LocalFile.Length >= 255 Then
                Throw New FileNameTooLongException("Dateipfad ist zu lang! - Kann Datei nicht schreiben!")
            End If

            If _item.FileSize = 0 Then
                _log.Warn("Keine Dateigröße hinterlegt - versuche diese nun zu ermitteln")
                GetItemFileSize(_item, _ftp_session)
            End If

            _disk_free_space = My.Computer.FileSystem.GetDriveInfo(IO.Path.GetPathRoot(_settings.DownloadDirectory)).AvailableFreeSpace

            _log.Info("Freier Speicherplatz: {0}", _disk_free_space)

            If _item.FileSize > _disk_free_space Then
                Throw New NotEnoughFreeDiskSpaceException("Zu wenig Speicherplatz!")
            End If

            If _settings.ExistingFileHandling = ExistingFileHandling.ResumeFile And IO.File.Exists(_item.LocalFile) Then

                _filemode = IO.FileMode.Append
                _restart = New IO.FileInfo(_item.LocalFile).Length

                If _item.FileSize.Equals(New IO.FileInfo(_item.LocalFile).Length) And Not _item.FileSize = 0 Then
                    _item.LocalFileSize = _item.FileSize
                    _skip_download = True
                Else
                    _item.LocalFileSize = _restart
                    _log.Info("Datei ist zwar bereits lokal vorhanden aber nicht vollständig")
                End If

            Else
                _filemode = IO.FileMode.Create
            End If

            If _skip_download = True Then
                _log.Info("Datei ist bereits vollständig - Überspringe FTP Connect!")
            Else

                Using _ftp_read_stream = ArxOne.Ftp.FtpClientUtility.Retr(_ftp_session.Connection.Client, New ArxOne.Ftp.FtpPath(_item.FullPath), ArxOne.Ftp.FtpTransferMode.Binary, _restart, _ftp_session)

                    buffer = New Byte(8192) {}
                    bytesRead = _ftp_read_stream.Read(buffer, 0, buffer.Length)

                    Using _local_write_stream As New IO.FileStream(_item.LocalFile, _filemode, IO.FileAccess.Write, IO.FileShare.None, 8192, False)

                        While bytesRead > 0 And (SmartThreadPool.IsWorkItemCanceled = False And Application.Current.Resources("DownloadStopped") = False)

                            Dim _tmp_percent_downloaded As Double = 0
                            Dim _new_perc As Integer = 0
                            Dim _download_speed As String = String.Empty

                            _local_write_stream.Write(buffer, 0, bytesRead)

                            bytesRead = _ftp_read_stream.Read(buffer, 0, Length)
                            bytestotalread += bytesRead

                            elapsed = DateTime.Now.Subtract(_starttime)
                            bytesPerSec = CInt(If(elapsed.TotalSeconds < 1, bytestotalread, bytestotalread / elapsed.TotalSeconds))

                            _item.LocalFileSize += bytesRead

#Region "Berechnung Download Speed / Fortschritt"

                            _tmp_percent_downloaded = CDbl(_local_write_stream.Position) / CDbl(_item.FileSize)
                            _new_perc = CInt(_tmp_percent_downloaded * 100)

                            If _new_perc <> _percent_downloaded Then 'Nicht jedesmal Updaten

                                Dim _tmp_speed As Double

                                _percent_downloaded = _new_perc

                                _ctime = DateTime.Now.Subtract(_starttime)

                                _tmp_speed = Math.Round(bytesPerSec / 1024, 2)

                                If _tmp_speed >= 1024 Then
                                    _download_speed = Math.Round(_tmp_speed / 1024, 2) & " MB/s"
                                Else
                                    _download_speed = _tmp_speed & " KB/s"
                                End If

                                _item.DownloadSpeed = _download_speed
                                _item.DownloadProgress = _percent_downloaded
                                _item.SizeDownloaded = bytestotalread

                            End If

#End Region

#Region "Limit Speed"


                            Dim _max_bytes_per_second As Integer
                            Dim _session_count As Integer

                            If Not String.IsNullOrWhiteSpace(MainViewModel.ThisInstance.MaxDownloadSpeed) Then

                                _max_bytes_per_second = Integer.Parse(MainViewModel.ThisInstance.MaxDownloadSpeed)

                                If Not _max_bytes_per_second <= 0 Then

                                    _session_count = _ftp_session_list.Where(Function(mysession) IsNothing(mysession.Value.Connection.ProtocolStream) = False).Count

                                    If Not _session_count <= 1 Then
                                        _max_bytes_per_second = CInt((_max_bytes_per_second * 1024) / _session_count)
                                    Else
                                        _max_bytes_per_second = CInt((_max_bytes_per_second * 1024))
                                    End If

                                    ThrottleByteTransfer(_max_bytes_per_second, bytestotalread, _ctime, bytesPerSec)

                                End If

                            End If

#End Region



                        End While

                    End Using

                End Using

            End If

            _item.DownloadStatus = NET3.DownloadItem.Status.Completed

        Catch ex As NotEnoughFreeDiskSpaceException
            _log.Error(ex, ex.Message)
            _item.DownloadStatus = NET3.DownloadItem.Status.Failed_NotEnoughDiskSpace

        Catch ex As FileNameTooLongException
            _log.Warn(ex, ex.Message)
            _item.DownloadStatus = NET3.DownloadItem.Status.Failed_FileNameTooLong

        Catch ex As ArxOne.Ftp.Exceptions.FtpException

            If Application.Current.Resources("DownloadStopped") = True Then
                _log.Info("Download wurde gestoppt!")
                _item.DownloadStatus = NET3.DownloadItem.Status.Stopped
            Else
                _log.Error(ex, ex.Message)
                ParseFTPException(ex, _item)
            End If

        Catch ex As Exception
            If Application.Current.Resources("DownloadStopped") = True Then
                _log.Info("Download wurde gestoppt!")
                _item.DownloadStatus = NET3.DownloadItem.Status.Stopped
            Else
                _log.Error(ex, ex.Message)

                If ex.GetType Is GetType(ArxOne.Ftp.Exceptions.FtpException) Then
                    ParseFTPException(ex, _item)
                Else
                    _item.DownloadStatus = NET3.DownloadItem.Status.Failed
                End If

            End If

        Finally
            _item.DownloadSpeed = String.Empty
            PostDownload(_item, _ftp_session)
        End Try

    End Sub

    Private Sub PostDownload(ByRef _item As DownloadItem, ByVal _ftp_session As ArxOne.Ftp.FtpSession)

        Dim _hashcommand As String = String.Empty
        Dim _reply As ArxOne.Ftp.FtpReply
        Dim _tmp_hash As String = String.Empty
        Dim _hashtype As Container.HashType

        Try

            If SmartThreadPool.IsWorkItemCanceled = True Or Application.Current.Resources("DownloadStopped") = True Then
                Throw New DownloadStoppedException("Cancel!")
            End If

            _item.DownloadSpeed = String.Empty

            If Application.Current.Resources("DownloadStopped") = False And _item.DownloadStatus = NET3.DownloadItem.Status.Completed Then

                If _item.HashType = Container.HashType.None Then

                    If _ftp_session.Connection.Client.ServerFeatures.HasFeature("MD5") Then
                        _hashcommand = "MD5"
                        _hashtype = Container.HashType.MD5
                    End If

                    If _ftp_session.Connection.Client.ServerFeatures.HasFeature("XMD5") Then
                        _hashcommand = "XMD5"
                        _hashtype = Container.HashType.MD5
                    End If

                    If _ftp_session.Connection.Client.ServerFeatures.HasFeature("XSHA1") Then
                        _hashcommand = "XSHA1"
                        _hashtype = Container.HashType.SHA1
                    End If

                    If _ftp_session.Connection.Client.ServerFeatures.HasFeature("XCRC") Then
                        _hashcommand = "XCRC"
                        _hashtype = Container.HashType.CRC
                    End If

                    If Not String.IsNullOrWhiteSpace(_hashcommand) Then
                        _log.Info("Server Support Hash Alogrightm {0}", _hashcommand)

                        _reply = _ftp_session.Expect(_ftp_session.SendCommand(_hashcommand, _item.FullPath), 250)

                        If _reply.Code.IsSuccess = True Then

                            _log.Info("Hash Serverseitig erfolgreich ermittelt!")

                            _item.HashType = _hashtype
                            _tmp_hash = _reply.Lines(0).ToString.Replace(_item.FullPath, "")
                            _tmp_hash = _tmp_hash.Replace(Chr(34), "")
                            _item.FileHash = _tmp_hash.Trim
                        Else
                            _log.Error("Hash konnte nicht ermittelt werden!")
                        End If

                    Else
                        _log.Info("Server does not Support any Hash Algorithm")
                    End If


                Else
                    _log.Info("Download Item has already an Hash (provided via SFDL Container)")
                End If

                Select Case _item.HashType

                    Case Container.HashType.None

                        _item.DownloadStatus = NET3.DownloadItem.Status.Completed

                    Case Container.HashType.MD5

                        _log.Info("Prüfe ob MD5 Hashes übereinstimmen")

                        If HashHelper.MD5FileHash(_item.LocalFile).ToLower.Equals(_item.FileHash.ToLower) Then
                            _log.Info("MD5 Hash is Valid!")
                            _item.DownloadStatus = NET3.DownloadItem.Status.Completed_HashValid
                        Else
                            _log.Info("MD5 Hash Invalid")
                            _item.DownloadStatus = NET3.DownloadItem.Status.Completed_HashInvalid
                        End If

                    Case Container.HashType.SHA1

                        _log.Info("Prüfe ob SHA1 Hashes übereinstimmen")

                        If HashHelper.SHA1FileHash(_item.LocalFile).ToLower.Equals(_item.FileHash.ToLower) Then
                            _log.Info("SHA1 Hash is Valid!")
                            _item.DownloadStatus = NET3.DownloadItem.Status.Completed_HashValid
                        Else
                            _log.Info("SHA1 Hash Invalid")
                            _item.DownloadStatus = NET3.DownloadItem.Status.Completed_HashInvalid
                        End If

                    Case Container.HashType.CRC

                        _log.Info("Prüfe ob CRC Hashes übereinstimmen")

                        If HashHelper.CRC32FileHash(_item.LocalFile).ToLower.Equals(_item.FileHash.ToLower) Then
                            _log.Info("CRC Hash is Valid!")
                            _item.DownloadStatus = NET3.DownloadItem.Status.Completed_HashValid
                        Else
                            _log.Info("CRC Hash Invalid")
                            _item.DownloadStatus = NET3.DownloadItem.Status.Completed_HashInvalid
                        End If

                End Select

            Else
                _log.Info("Download wurde gestoppt oder Item wurde nicht vollständig heruntergeladen - Überspringe Hash Check")
            End If

        Catch ex As DownloadStoppedException
            _log.Info("Download Stopped")
            _item.DownloadStatus = NET3.DownloadItem.Status.Stopped

        Catch ex As Exception
            _log.Error(ex.Message)
            _item.DownloadStatus = NET3.DownloadItem.Status.Completed_HashInvalid

        Finally

            If _item.DownloadStatus = NET3.DownloadItem.Status.Failed_ServerFull And _item.SingleSessionMode = False Then
                RaiseEvent ServerFull(_item)
            Else

                If (_item.RetryPossible And _item.RetryCount < 3) And Not _item.DownloadStatus = NET3.DownloadItem.Status.Stopped Then
                    _item.DownloadStatus = NET3.DownloadItem.Status.RetryWait
                    System.Threading.Thread.Sleep(_settings.RetryWaitTime * 1000)
                    _item.RetryCount += 1
                    _log.Info("Setze Item auf die Retry Warteliste")
                    _item.DownloadStatus = NET3.DownloadItem.Status.Retry
                Else
                    If Not IsNothing(_ftp_session) Then
                        _ftp_session.Invalidate()
                    End If
                End If

            End If

        End Try

    End Sub

End Class
