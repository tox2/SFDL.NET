Imports System.Collections.Specialized
Imports System.Text.RegularExpressions
Imports SFDL.NET3

Public Class MainViewModel
    Inherits ViewModelBase

    Private _settings As New Settings

    Dim _eta_ts As New System.Threading.CancellationTokenSource()
    Dim _dl_item_ts As New System.Threading.CancellationTokenSource()

    Public Sub New()
        _instance = Me
        _settings = Application.Current.Resources("Settings")
        CreateView()
    End Sub

#Region "Private Subs"

    Private Sub CreateView()

        Dim view As CollectionView = DirectCast(CollectionViewSource.GetDefaultView(DownloadItems), CollectionView)

        'Dim groupDescription As New PropertyGroupDescription("PackageName")

        'view.GroupDescriptions.Add(groupDescription)

        Dim groupDescription As New PropertyGroupDescription("GroupDescriptionIdentifier")

        view.GroupDescriptions.Add(groupDescription)

    End Sub

    Friend Async Sub OpenSFDLFile(ByVal _sfdl_container_path As String)

        Dim _mytask As New AppTask("")
        Dim _mycontainer As New Container.Container
        Dim _mycontainer_session As ContainerSession
        Dim _decrypt_password As String
        Dim _decrypt As New SFDL.Container.Decrypt
        Dim _country_code As String = String.Empty

        AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent

        DispatchService.DispatchService.Invoke(Sub()
                                                   ActiveTasks.Add(_mytask)
                                               End Sub)

        _mytask.SetTaskStatus(TaskStatus.Running, String.Format("SFDL Datei {0} wird geöffnet...", _sfdl_container_path))

        Try

            Dim _bulk_result As Boolean

            If GetContainerVersion(_sfdl_container_path) = 0 Or GetContainerVersion(_sfdl_container_path) > 10 Then

                Throw New Exception("Diese SFDL Datei ist mit dieser Programmversion nicht kompatibel!")
            End If

            _mycontainer = XMLHelper.XMLDeSerialize(_mycontainer, _sfdl_container_path)

            If _mycontainer.Encrypted = True Then

                Try
Decrypt:
                    _decrypt_password = Await MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.ShowInputAsync(Me, "SFDL entschlüsseln", "Bitte gib ein Passwort ein um den SFDL Container zu entschlüsseln")

                    If String.IsNullOrWhiteSpace(_decrypt_password) Then
                        Throw New Exception("SFDL entschlüsseln abgebrochen")
                    End If

                    _decrypt.DecryptString(_mycontainer.Connection.Host, _decrypt_password)

                Catch ex As SFDL.Container.FalsePasswordException
                    GoTo Decrypt
                End Try

                DecryptSFDLContainer(_mycontainer, _decrypt_password)

            End If

            _mycontainer_session = New ContainerSession(_mycontainer)
            _mycontainer_session.ContainerFileName = IO.Path.GetFileNameWithoutExtension(_sfdl_container_path)
            _mycontainer_session.ContainerFilePath = _sfdl_container_path

            If String.IsNullOrWhiteSpace(_mycontainer.Description) Then
                _mycontainer_session.DisplayName = _mycontainer_session.ContainerFileName
            Else
                _mycontainer_session.DisplayName = _mycontainer.Description
            End If

            GenerateContainerFingerprint(_mycontainer_session)

            If Not ContainerSessions.Where(Function(mycon) mycon.Fingerprint.Equals(_mycontainer_session.Fingerprint)).Count = 0 Then
                Throw New Exception("Dieser SFDL Container ist bereits geöffnet!")
            End If

            If Not _mycontainer_session.ContainerFile.Packages.Where(Function(mypackage) mypackage.BulkFolderMode = True).Count = 0 Then
                _bulk_result = Await Task.Run(Function() GetBulkFileList(_mycontainer_session))
            End If

            GenerateContainerSessionDownloadItems(_mycontainer_session, _settings.NotMarkAllContainerFiles)

            If _bulk_result = False Or _mycontainer_session.DownloadItems.Count = 0 Then
                Throw New Exception("Öffnen fehlgeschlagen - Bulk Package konnte nicht gelesen werden")
            End If



#Region "Parse Unrar/InstatVideo Chain"


            For Each _item In _mycontainer_session.DownloadItems.Where(Function(_my_item As DownloadItem) IO.Path.GetExtension(_my_item.FileName).Equals(".rar"))

                Dim _unrarchain As New UnRARChain
                Dim _searchpattern As Regex
                Dim _count As Integer
                Dim _log As NLog.Logger = NLog.LogManager.GetLogger("RarChainParser")

                If Not _item.FileName.Contains(".part") Then

                    _count = 0

                    _item.FirstUnRarFile = True
                    _item.RequiredForInstantVideo = True
                    _unrarchain.MasterUnRarChainFile = _item

                    _log.Debug("First UnRar File: {0}", _item.FileName)

                    _searchpattern = New Regex("filename\.r[0-9]{1,2}".Replace("filename", IO.Path.GetFileNameWithoutExtension(_item.FileName)))

                    For Each _chainitem As DownloadItem In _mycontainer_session.DownloadItems.Where(Function(_my_item As DownloadItem) _searchpattern.IsMatch(_my_item.FileName))

                        _log.Debug("ChainItem FileName: {0}", _chainitem.FileName)

                        If _count < 1 Then
                            _chainitem.RequiredForInstantVideo = True
                        End If

                        _unrarchain.ChainMemberFiles.Add(_chainitem)

                        _count += 1

                    Next

                    _mycontainer_session.UnRarChains.Add(_unrarchain)

                Else

                    _searchpattern = New Regex("^((?!\.part(?!0*1\.rar$)\d+\.rar$).)*\.(?:rar|r?0*1)$")

                    _count = 0

                    If _searchpattern.IsMatch(_item.FileName) Then 'MasterFile

                        Dim _tmp_filename_replace As String

                        _log.Debug("First UnRar File: {0}", _item.FileName)
                        _item.FirstUnRarFile = True
                        _item.RequiredForInstantVideo = True
                        _unrarchain.MasterUnRarChainFile = _item

                        _tmp_filename_replace = _item.FileName.Remove(_item.FileName.IndexOf(".part"))

                        _searchpattern = New Regex("filename\.part[0-9]{1,3}.rar".Replace("filename", _tmp_filename_replace))

                        For Each _chainitem As DownloadItem In _mycontainer_session.DownloadItems.Where(Function(_my_item As DownloadItem) _searchpattern.IsMatch(_my_item.FileName) And Not _my_item.FileName.Equals(_unrarchain.MasterUnRarChainFile.FileName))

                            _log.Debug("ChainItem FileName: {0}", _chainitem.FileName)

                            If _count < 1 Then
                                _chainitem.RequiredForInstantVideo = True
                            End If

                            _unrarchain.ChainMemberFiles.Add(_chainitem)

                            _count += 1

                        Next

                        _mycontainer_session.UnRarChains.Add(_unrarchain)

                    End If

                End If

            Next

#End Region

            'ToDo: Parse/Generate InstantVideo Chain


            DispatchService.DispatchService.Invoke(Sub()

                                                       For Each _item In _mycontainer_session.DownloadItems
                                                           DownloadItems.Add(_item)
                                                       Next

                                                       ContainerSessions.Add(_mycontainer_session)

                                                   End Sub)

            If _bulk_result = False And Not _mycontainer_session.DownloadItems.Count = 0 Then

                _mytask.SetTaskStatus(TaskStatus.RanToCompletion, "SFDL teilweise geöffnet - Ein oder mehrere Packages konnten nicht gelesen werden.")
            Else
                _mytask.SetTaskStatus(TaskStatus.RanToCompletion, "SFDL geöffnet")
            End If


        Catch ex As Exception
            _mytask.SetTaskStatus(TaskStatus.Faulted, ex.Message)
        End Try


    End Sub

    Private Function PreDownloadCheck(ByVal _task As AppTask) As Boolean

        Dim _rt As Boolean = True

        Try

            If DownloadItems.Where(Function(myitem) myitem.DownloadStatus = DownloadItem.Status.Queued).Count = 0 Then
                Throw New Exception("You must select minumum 1 Item to Download!")
            End If

            If IO.Directory.Exists(_settings.DownloadDirectory) = False Then
                Throw New Exception("Download Verzeichnis existiert nicht!")
            End If

        Catch ex As Exception
            _rt = False
            _task.SetTaskStatus(TaskStatus.Faulted, ex.Message)
        End Try

        Return _rt

    End Function

    ''' <summary>
    ''' Konvertiert eine Dezimale Zeitangabe in eine lesbare Angabe.
    ''' </summary>
    Public Function ConvertDecimal2Time(ByVal value As Double) As String
        Try
            Dim valueTime As TimeSpan = TimeSpan.FromMinutes(value)
            If valueTime.Hours.Equals(0) Then
                Return String.Format("{0:D2}:{1:D2}", valueTime.Minutes, valueTime.Seconds)
            End If
            Return String.Format("{0:D2}:{1:D2}:{2:D2}", valueTime.Hours, valueTime.Minutes, valueTime.Seconds)
        Catch
            Return "0"
        End Try
    End Function

    Private Sub CalculateETA(ByVal _mytask As AppTask, _ct As System.Threading.CancellationToken)

        Dim _percent_downloaded As Integer = 0

        While _ct.IsCancellationRequested = False

            Dim _total_speed As Double = 0
            Dim _total_size As Double = 0
            Dim _total_size_downloaded As Double = 0
            Dim _size_remaining As Double = 0
            Dim _time_remaining As Double = 0
            Dim _percent_done As Integer = 0

            Try

                If (Me.WindowState = WindowState.Maximized Or Me.WindowState = WindowState.Normal) Or Me.TasksExpanded = True Then 'Nur berechnen wenn Window Sichtbar

                    System.Threading.Tasks.Parallel.ForEach(DownloadItems.Where(Function(myitem) Not myitem.DownloadStatus = DownloadItem.Status.None), Sub(_item As DownloadItem)

                                                                                                                                                            If Not _item.DownloadStatus = DownloadItem.Status.Stopped Then 'ToDO: Prüfen ob das sinn macht

                                                                                                                                                                If Not String.IsNullOrWhiteSpace(_item.DownloadSpeed) Then

                                                                                                                                                                    Dim _raw_speed As String = _item.DownloadSpeed.ToString

                                                                                                                                                                    If _raw_speed.Contains("KB/s") Then
                                                                                                                                                                        _raw_speed = _raw_speed.Replace("KB/s", "").Trim
                                                                                                                                                                        _total_speed += Double.Parse(_raw_speed)
                                                                                                                                                                    Else

                                                                                                                                                                        If _raw_speed.Contains("MB/s") Then
                                                                                                                                                                            _raw_speed = _raw_speed.Replace("MB/s", "").Trim
                                                                                                                                                                            _total_speed += Double.Parse(_raw_speed) * 1024
                                                                                                                                                                        End If

                                                                                                                                                                    End If

                                                                                                                                                                End If

                                                                                                                                                                _total_size += _item.FileSize
                                                                                                                                                                _total_size_downloaded += _item.SizeDownloaded

                                                                                                                                                            End If

                                                                                                                                                        End Sub)

                    _percent_done = CInt((_total_size_downloaded / _total_size) * 100)

                    If Not _percent_done = _percent_downloaded Then

                        _percent_downloaded = _percent_done

                        _size_remaining = _total_size - _total_size_downloaded

                        If _size_remaining > 0 And _total_speed > 0 Then

                            _time_remaining = Math.Round(Double.Parse(CStr(((_size_remaining / 1024) / _total_speed) / 60)), 2)

                            If _total_speed >= 1024 Then
                                _mytask.SetTaskStatus(TaskStatus.Running, String.Format("Download läuft - Speed: {0} MB/s | ETA: {1} | {2} %", Math.Round(_total_speed / 1024, 2), ConvertDecimal2Time(_time_remaining), CInt((_total_size_downloaded / _total_size) * 100)))
                            Else
                                _mytask.SetTaskStatus(TaskStatus.Running, String.Format("Download läuft - Speed: {0} KB/s | ETA: {1} | {2} %", Math.Round(_total_speed / 1024, 2), ConvertDecimal2Time(_time_remaining), CInt((_total_size_downloaded / _total_size) * 100)))
                            End If

                        Else
                            _mytask.SetTaskStatus(TaskStatus.Running, String.Format("Download läuft - {0} %", CInt((_total_size_downloaded / _total_size) * 100)))
                        End If

                    End If

                Else
                    Debug.WriteLine("Keine Berechnung Fenster ist runtergeklappt!")
                End If

            Catch ex As Exception

            Finally
                System.Threading.Thread.Sleep(1000)
            End Try

        End While

        Debug.WriteLine("ETA While beendet!")

    End Sub

    Private Async Sub StartDownload()

        Dim _download_helper As New DownloadHelper
        Dim _log As NLog.Logger = NLog.LogManager.GetLogger("StartDownload")
        Dim _tasklist As New List(Of System.Threading.Tasks.Task)
        Dim _mytask As New AppTask("Download wird gestartet...")
        Dim _thread_count_pool As Integer = 0
        Dim _error As Boolean = False

        Try

            _thread_count_pool = _settings.MaxDownloadThreads
            _eta_ts = New System.Threading.CancellationTokenSource
            _dl_item_ts = New System.Threading.CancellationTokenSource

#Region "Cleanup"


            Application.Current.Resources("DownloadStopped") = False

            For Each _session In ContainerSessions
                _session.SessionState = ContainerSessionState.Queued
                _session.ActiveThreads = 0
                _session.SingleSessionMode = False
            Next

            For Each _dlitem In DownloadItems.Where(Function(myitem) myitem.isSelected = True)
                _dlitem.DownloadStatus = DownloadItem.Status.Queued
                _dlitem.DownloadProgress = 0
                _dlitem.DownloadSpeed = String.Empty
            Next

#End Region
            If PreDownloadCheck(_mytask) = True Then

                Me.ButtonDownloadStartStop = False

                'Register Event Handlers
                AddHandler _download_helper.ItemDownloadComplete, AddressOf ItemDownloadCompleteEvent
                AddHandler _download_helper.ServerFull, AddressOf ServerFullEvent
                AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent

                ActiveTasks.Add(_mytask)

                _mytask.SetTaskStatus(TaskStatus.Running, "Download läuft...")

#Disable Warning BC42358 ' Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
                System.Threading.Tasks.Task.Run(Sub()
                                                    CalculateETA(_mytask, _eta_ts.Token)
                                                End Sub, _eta_ts.Token)
#Enable Warning BC42358 ' Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.



                While Not ContainerSessions.Where(Function(mysession) mysession.SessionState = ContainerSessionState.Queued Or mysession.SessionState = ContainerSessionState.DownloadRunning).Count = 0

#Region "Query Download Items"

                    Dim _itemdownloadlist As New Dictionary(Of ContainerSession, List(Of DownloadItem))

                    For Each _session In ContainerSessions.Where(Function(mysession) mysession.SessionState = ContainerSessionState.Queued Or mysession.SessionState = ContainerSessionState.DownloadRunning)

                        Dim _thread_count As Integer = 0
                        Dim _session_itemlist As New List(Of DownloadItem)

                        _log.Debug("Threads Verfügbar im Pool: {0}", _thread_count_pool)

                        SyncLock _session.SynLock 'Ensure Active Thread Count is not modified

                            _log.Debug("Session Maximal {0} Threads - Davon sind {1} aktiv", _session.ContainerFile.MaxDownloadThreads, _session.ActiveThreads)

                            If (_session.SingleSessionMode = True And Not _thread_count_pool = 0) Then
                                _thread_count = 1
                                _log.Debug("SingleSessionMode Override!")
                                'ToDo: Für Benutzer sichtbar machen das SingleSessionMode aktiv ist

                                If Not _session.ActiveThreads = 0 Then 'Warten bis alle anderen Threads fertig sind somit anschießend auch wirklich nur ein Thread läuft
                                    _thread_count = 0
                                End If

                            Else

                                If Not _session.ContainerFile.MaxDownloadThreads = _session.ActiveThreads Then

                                    If Not (_session.ContainerFile.MaxDownloadThreads - _session.ActiveThreads) > _thread_count_pool Then
                                        _thread_count = _session.ContainerFile.MaxDownloadThreads - _session.ActiveThreads
                                    Else
                                        If Not _thread_count_pool = 0 Then
                                            _thread_count = _thread_count_pool
                                        Else
                                            _log.Info("Alle verfügbaren Threads sind vergeben!")
                                        End If
                                    End If

                                End If

                            End If

                            _log.Debug("Thread Count: {0}", _thread_count)

                            _thread_count_pool -= _thread_count
                            _session.ActiveThreads += _thread_count

                            _log.Debug("Session hat nun {0} aktive Threads", _session.ActiveThreads)

                        End SyncLock

                        If Not _thread_count = 0 Then

                            _session.SessionState = ContainerSessionState.DownloadRunning

                            If _session.DownloadStartedTime = Date.MinValue Then
                                _session.DownloadStartedTime = Now
                            End If

                            For Each _dlitem In DownloadItems.Where(Function(myitem) (myitem.ParentContainerID.Equals(_session.ID) And (myitem.DownloadStatus = DownloadItem.Status.Queued Or myitem.DownloadStatus = DownloadItem.Status.Retry))).Take(_thread_count)

                                'Reset necessary Properties
                                _dlitem.DownloadStatus = DownloadItem.Status.Running
                                _dlitem.SizeDownloaded = 0
                                _dlitem.RetryPossible = False

                                If _dlitem.DownloadStatus = DownloadItem.Status.Queued Then
                                    _dlitem.RetryCount = 0
                                End If

                                _session_itemlist.Add(_dlitem)

                            Next

                            _itemdownloadlist.Add(_session, _session_itemlist)

                        End If

                    Next

#End Region

                    _log.Debug("Insgesamt {0} Items in der Downloadliste", _itemdownloadlist.Count)

#Region "Start Download Tasks"

                    If Not _itemdownloadlist.Count = 0 Then

                        For Each _object In _itemdownloadlist

                            For Each _item As DownloadItem In _object.Value

                                Dim _single_session_mode As Boolean = False

                                _single_session_mode = ContainerSessions.Where(Function(mycontainer) mycontainer.ID.Equals(_item.ParentContainerID))(0).SingleSessionMode

                                _tasklist.Add(System.Threading.Tasks.Task.Run(Sub()
                                                                                  _download_helper.DownloadContainerItems(New List(Of DownloadItem)() From {_item}, _settings.DownloadDirectory, _object.Key.ContainerFile.Connection, _dl_item_ts.Token, _single_session_mode)
                                                                              End Sub, _dl_item_ts.Token))

                            Next

                        Next

                    End If

#End Region

#Region "Await Any Task"


                    _tasklist.RemoveAll(Function(mytask) mytask.Status = TaskStatus.RanToCompletion)


                    If Not _tasklist.Count = 0 Then

                        Await Threading.Tasks.Task.WhenAny(_tasklist)

                        _thread_count_pool = _settings.MaxDownloadThreads - _tasklist.Where(Function(mytask) mytask.Status = TaskStatus.Running).Count

                    End If

                    _log.Debug("Noch {0} Freie Tasks im ThreadPool", _thread_count_pool)

#End Region


#Region "Check if Download or Any Session is Complete"


                    For Each _session In ContainerSessions.Where(Function(mysession) mysession.SessionState = ContainerSessionState.Queued Or mysession.SessionState = ContainerSessionState.DownloadRunning)

                        If DownloadItems.Where(Function(myitem) (myitem.DownloadStatus = DownloadItem.Status.Queued Or myitem.DownloadStatus = DownloadItem.Status.Running) Or myitem.DownloadStatus = DownloadItem.Status.Retry).Count = 0 Then 'Alle Items sind heruntergeladen

                            _session.SessionState = ContainerSessionState.DownloadComplete
                            _session.DownloadStoppedTime = Now

#Region "Speedreport Generation"


                            If _settings.SpeedReportSettings.SpeedreportView = SpeedreportVisibility.ShowGUI Or _settings.SpeedReportSettings.SpeedreportView = SpeedreportVisibility.Write2File Then

                                Dim _speedreport As String = String.Empty
                                Dim _sr_filepath As String = String.Empty
                                Dim _sr_task As New AppTask("Speedreport wird erstellt")

                                Try

                                    ActiveTasks.Add(_sr_task)

                                    _speedreport = SpeedreportHelper.GenerateSpeedreport(_session, _settings.SpeedReportSettings)
                                    'ToDO: Caution: Post Action!

                                    If String.IsNullOrWhiteSpace(_speedreport) Then
                                        Throw New Exception("Speedreport failed")
                                    End If

                                    If _settings.SpeedReportSettings.SpeedreportView = SpeedreportVisibility.Write2File Then

                                        _sr_filepath = IO.Path.GetDirectoryName(_session.DownloadItems(0).LocalFile)

                                        _sr_filepath = IO.Path.Combine(_sr_filepath, "speedreport.txt")

                                        My.Computer.FileSystem.WriteAllText(_sr_filepath, _speedreport, False, System.Text.Encoding.Default)

                                    End If

                                    If _settings.SpeedReportSettings.SpeedreportView = SpeedreportVisibility.ShowGUI Then

                                        'ToDo:Show Speedreport Gui

                                    End If

                                    _sr_task.SetTaskStatus(TaskStatus.RanToCompletion, "Speedreport erstellt")

                                Catch ex As Exception
                                    _sr_task.SetTaskStatus(TaskStatus.Faulted, "Speedreport Generation failed")
                                End Try

                            End If

#End Region

                        End If

                    Next

#End Region

                    If Application.Current.Resources("DownloadStopped") = True Then
                        _log.Info("Dowload wurde gestoppt!")
                        Exit While
                    End If

                End While

                _log.Info("Alle Downloads abgeschlossen!!")

            Else
                _log.Warn("Pre Check nicht bestanden -> Starte keinen Download")
            End If

        Catch ex As Exception
            _log.Error(ex, ex.Message)
            _error = True
        Finally

            System.Threading.Tasks.Task.Run(Sub()

                                                _eta_ts.Cancel()
                                                _dl_item_ts.Cancel()
                                                Application.Current.Resources("DownloadStopped") = True
                                                _download_helper.DisposeFTPClients()
                                                _eta_ts.Dispose()
                                                _dl_item_ts.Dispose()

                                            End Sub).Wait()

            If _error = False Then
                _mytask.SetTaskStatus(TaskStatus.RanToCompletion, "Download beendet!")
            Else
                _mytask.SetTaskStatus(TaskStatus.Faulted, "Download mit fehlern gestoppt!")
            End If

            Me.ButtonDownloadStartStop = True

        End Try

    End Sub

    Private Sub ServerFullEvent(_item As DownloadItem)


        System.Threading.Tasks.Task.Run(Sub()

                                            Dim _log As NLog.Logger = NLog.LogManager.GetLogger("ItemDownloadCompleteEvent")

                                            _log.Debug("Item {0} war als Download gequed und ist jetzt fertig - Reduziere aktiven Thread Count für diese Session", _item.FileName)

                                            _item.DownloadStatus = DownloadItem.Status.Queued

                                            SyncLock ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID)).SynLock

                                                If Not ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID)).ActiveThreads = 0 Then
                                                    ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID)).ActiveThreads -= 1
                                                End If

                                                ContainerSessions.Where(Function(mycontainer) mycontainer.ID.Equals(_item.ParentContainerID))(0).SingleSessionMode = True

                                            End SyncLock

                                            _log.Debug("Session hat nun {0} Aktive Threads", ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID)).ActiveThreads)

                                        End Sub)

    End Sub

    Private Sub ItemDownloadCompleteEvent(_item As DownloadItem)

        System.Threading.Tasks.Task.Run(Sub()

                                            Dim _log As NLog.Logger = NLog.LogManager.GetLogger("ItemDownloadCompleteEvent")

                                            _log.Debug("Item {0} war als Download gequed und ist jetzt fertig - Reduziere aktiven Thread Count für diese Session", _item.FileName)

                                            SyncLock ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID)).SynLock

                                                If Not ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID)).ActiveThreads = 0 Then
                                                    ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID)).ActiveThreads -= 1
                                                End If

                                            End SyncLock

                                            _log.Debug("Session hat nun {0} Aktive Threads", ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID)).ActiveThreads)

                                            _log.Info("Prüfe etwas entpackt werden kann...")

                                            Dim _mysession As ContainerSession

                                            _mysession = ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID))

                                            If Not _mysession.UnRarChains.Count = 0 Then

                                                For Each _chain In _mysession.UnRarChains

                                                    If isUnRarChainComplete(_chain) = True And (_chain.UnRARDone = False Or _chain.UnRARRunning = True) Then

                                                        _chain.UnRARRunning = True

                                                        _log.Debug("Chain {0} ist komplett!", _chain.MasterUnRarChainFile.FileName.ToString)

                                                        If _settings.UnRARSettings.UnRARAfterDownload = True And _chain.UnRARDone = False Then
                                                            'TODO: Block Application Exit while UnRAR is Running
                                                            ' UnRAR(AddTask(StringMessages.UnRARExtractingFilesTaskStartMessage), _chain, e.OLVItem.SFDLSessionName)
                                                        Else
                                                            '_block_app_exit = False
                                                            '_unrar_active = False
                                                        End If

                                                        _chain.UnRARRunning = False

                                                    Else
                                                        _log.Info("UnRARChain ist noch nicht vollständig oder diese wird bereits entapckt/bearbeitet")
                                                        'TODO: Check for InstatnVideo
                                                    End If

                                                Next

                                            Else
                                                _log.Info("Dieser Container hat keine UnRarChain")
                                            End If

                                        End Sub)

    End Sub

    Private Async Sub StopDownload()

        Await System.Threading.Tasks.Task.Run(Sub()
                                                  Application.Current.Resources("DownloadStopped") = True
                                                  _eta_ts.Cancel()
                                                  _dl_item_ts.Cancel()
                                              End Sub)

        Me.ButtonDownloadStartStop = False

    End Sub

    Sub Test()

        Dim _ftp_client As ArxOne.Ftp.FtpClient
        Dim _session As ArxOne.Ftp.FtpSession

        SetupFTPClient(_ftp_client, ContainerSessions(0).ContainerFile.Connection)

        _session = _ftp_client.Session

        ArxOne.Ftp.FtpClientUtility.List(_session.Connection.Client, "/")

        Debug.WriteLine("LIST 1 fertig")

        Debug.WriteLine("")

        ArxOne.Ftp.FtpClientUtility.List(_session.Connection.Client, "/")

        Debug.WriteLine("LIST 2 fertig")

        Debug.WriteLine("")

        _ftp_client.Dispose()

    End Sub

#End Region

#Region "Button States"

    Private _button_downloadstartstop_enabled As Boolean = True

    Public Property ButtonDownloadStartStop As Boolean
        Set(value As Boolean)
            _button_downloadstartstop_enabled = value
            RaisePropertyChanged("ButtonDownloadStartStop")
        End Set
        Get
            Return _button_downloadstartstop_enabled
        End Get
    End Property

    Private _button_instantvideo_enabled As Boolean = False
    Public Property ButtonInstantVideoEnabled As Boolean
        Set(value As Boolean)
            _button_instantvideo_enabled = value
            RaisePropertyChanged("ButtonInstantVideoEnabled")
        End Set
        Get
            Return _button_instantvideo_enabled
        End Get
    End Property



#End Region

#Region "Menu Commands"

    Public ReadOnly Property ShowSettingsDialogCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf ShowSettingsDialog)
        End Get
    End Property

    Private Sub ShowSettingsDialog()
        Dim _settings_dialog As New SettingsWindow
        _settings_dialog.ShowDialog()
    End Sub

    Public ReadOnly Property ExitApplicationCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf ExitApplication)
        End Get
    End Property

    Private Sub ExitApplication()
        Application.Current.Shutdown()
    End Sub

    Public ReadOnly Property OpenSFDLCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf OpenSFDL)
        End Get
    End Property

    Private Sub OpenSFDL()

        Dim _ofd As New Microsoft.Win32.OpenFileDialog()

        With _ofd

            .Multiselect = True
            .Title = "SFDL Datei(en) öffnen"
            .Filter = "SFDL Files (*.sfdl)|*.sfdl"

        End With

        If Not _ofd.ShowDialog = vbCancel Then

            For Each _file In _ofd.FileNames
                OpenSFDLFile(_file)
            Next

        End If

    End Sub

    Public ReadOnly Property StartDownloadCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf StartDownload)
        End Get
    End Property

    Public ReadOnly Property StopDownloadCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf StopDownload)
        End Get
    End Property

    Public ReadOnly Property ShowContainerInfoCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf ShowContainerInfo)
        End Get
    End Property

    Private Sub ShowContainerInfo()
        Me.ContainerInfoOpen = True
    End Sub

    Public ReadOnly Property MarkAllItemsCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf MarkAllItems)
        End Get
    End Property

    Private Sub MarkAllItems()

        For Each _item In DownloadItems
            _item.isSelected = True
        Next

    End Sub

    Public ReadOnly Property UnmarkAllItemsCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf UnmarkAllItems)
        End Get
    End Property

    Private Sub UnmarkAllItems()


        For Each _item In DownloadItems
            _item.isSelected = False
        Next

    End Sub


#End Region

#Region "ListView ContextMenu Commands and Properites"

    Private _curr_selected_item As DownloadItem = Nothing

    Public Property SelectedDownloadItem As DownloadItem
        Set(value As DownloadItem)
            _curr_selected_item = value
            RaisePropertyChanged("SelectedDownloadItem")
        End Set
        Get
            Return _curr_selected_item
        End Get
    End Property

    Public ReadOnly Property CloseSFDLContainerCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf CloseSFDLContainer)
        End Get
    End Property

    Private Sub CloseSFDLContainer(ByVal parameter As Object)

        If Not IsNothing(parameter) Then

            If Not String.IsNullOrWhiteSpace(parameter) And parameter.ToString.Contains(";") Then

                Dim _container_sessionid As Guid = Guid.Parse(parameter.ToString.Split(";")(1))

                Dim _tmp_list As New List(Of DownloadItem)

                _tmp_list = DownloadItems.Where(Function(myitem) _container_sessionid.Equals(myitem.ParentContainerID)).ToList

                For Each _item In _tmp_list
                    DownloadItems.Remove(_item)
                Next

                ContainerSessions.Remove(ContainerSessions.Where(Function(mysession) mysession.ID.Equals(_container_sessionid))(0))

            End If

        End If

        'If Not IsNothing(SelectedDownloadItem) Then

        '    Dim _container_sessionid As Guid

        '    _container_sessionid = SelectedDownloadItem.ParentContainerID

        '    Dim _tmp_list As New List(Of DownloadItem)

        '    _tmp_list = DownloadItems.Where(Function(myitem) SelectedDownloadItem.ParentContainerID.Equals(myitem.ParentContainerID)).ToList

        '    For Each _item In _tmp_list
        '        DownloadItems.Remove(_item)
        '    Next

        '    ContainerSessions.Remove(ContainerSessions.Where(Function(mysession) mysession.ID.Equals(_container_sessionid))(0))

        'End If


    End Sub

#End Region

#Region "Allgemeine Properties"

    Private _window_state As System.Windows.WindowState = WindowState.Normal
    Public Property WindowState As System.Windows.WindowState
        Set(value As System.Windows.WindowState)
            _window_state = value
            RaisePropertyChanged("WindowState")
        End Set
        Get
            Return _window_state
        End Get
    End Property

    Private Shared _instance As MainViewModel
    Public Shared ReadOnly Property ThisInstance As MainViewModel
        Get
            Return _instance
        End Get
    End Property

    Private _container_info_shown As Boolean = False

    Public Property ContainerInfoOpen As Boolean
        Set(value As Boolean)
            _container_info_shown = value
            RaisePropertyChanged("ContainerInfoOpen")
        End Set
        Get
            Return _container_info_shown
        End Get
    End Property

#End Region

#Region "Tasks"

    Private _active_tasks As New ObjectModel.ObservableCollection(Of AppTask)
    Public Property ActiveTasks As ObjectModel.ObservableCollection(Of AppTask)
        Set(value As ObjectModel.ObservableCollection(Of AppTask))
            _active_tasks = value
            RaisePropertyChanged("ActiveTasks")
        End Set
        Get
            Return _active_tasks
        End Get
    End Property

    Private _done_tasks As New ObjectModel.ObservableCollection(Of AppTask)
    Public Property DoneTasks As ObjectModel.ObservableCollection(Of AppTask)
        Set(value As ObjectModel.ObservableCollection(Of AppTask))
            _done_tasks = value
            RaisePropertyChanged("DoneTasks")
        End Set
        Get
            Return _done_tasks
        End Get
    End Property

    Private Sub TaskDoneEvent(e As AppTask)

        System.Threading.Tasks.Task.Run(Sub()

                                            'Wait 5 Seconds
                                            System.Threading.Thread.Sleep(5000)
                                            DispatchService.DispatchService.Invoke(Sub()
                                                                                       ActiveTasks.Remove(e)
                                                                                       DoneTasks.Add(e)
                                                                                   End Sub)

                                        End Sub)

    End Sub

    Private _tasks_expanded As Boolean = True

    Public Property TasksExpanded As Boolean
        Set(value As Boolean)
            _tasks_expanded = value
            RaisePropertyChanged("TasksExpanded")
        End Set
        Get
            Return _tasks_expanded
        End Get
    End Property


#End Region

#Region "Container Sessions"

    Private _container_sessions As New ObjectModel.ObservableCollection(Of ContainerSession)

    Public Property ContainerSessions As ObjectModel.ObservableCollection(Of ContainerSession)
        Set(value As ObjectModel.ObservableCollection(Of ContainerSession))
            _container_sessions = value
            RaisePropertyChanged("ContainerSessions")
        End Set
        Get
            Return _container_sessions
        End Get
    End Property

    Private _download_items As New ObjectModel.ObservableCollection(Of DownloadItem)

    Public Property DownloadItems As ObjectModel.ObservableCollection(Of DownloadItem)
        Set(value As ObjectModel.ObservableCollection(Of DownloadItem))
            _download_items = value
            RaisePropertyChanged("DownloadItems")
        End Set
        Get
            Return _download_items
        End Get
    End Property

#End Region




End Class
