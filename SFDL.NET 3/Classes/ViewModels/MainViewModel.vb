Imports System.Collections.Specialized
Imports System.Text.RegularExpressions
Imports Amib.Threading
Imports SFDL.NET3

Public Class MainViewModel
    Inherits ViewModelBase

    Private _settings As New Settings

    Private _stp As New SmartThreadPool
    Private _lock_active_tasks As New Object
    Private _lock_done_tasks As New Object
    Private _lock_download_items As New Object
    Private _lock_container_sessions As New Object
    Private _eta_thread As IWorkItemResult(Of Boolean)


    Public Sub New()

        _instance = Me
        _settings = Application.Current.Resources("Settings")
        Application.Current.Resources("DownloadStopped") = True
        Application.Current.Resources("UnRARBlock") = False

        'Init ThreadSafe Observ Collections

        BindingOperations.EnableCollectionSynchronization(ActiveTasks, _lock_active_tasks)
        BindingOperations.EnableCollectionSynchronization(DoneTasks, _lock_done_tasks)
        BindingOperations.EnableCollectionSynchronization(DownloadItems, _lock_download_items)
        BindingOperations.EnableCollectionSynchronization(ContainerSessions, _lock_container_sessions)

        CreateView()

    End Sub

#Region "Private Subs"

    Private Sub CreateView()

        Dim view As CollectionView = DirectCast(CollectionViewSource.GetDefaultView(DownloadItems), CollectionView)

        Dim groupDescription As New PropertyGroupDescription("GroupDescriptionIdentifier")

        view.GroupDescriptions.Add(groupDescription)

    End Sub

    Friend Async Sub OpenSFDLFile(ByVal _sfdl_container_path As String)

        Dim _mytask As New AppTask("")
        Dim _mycontainer As New Container.Container
        Dim _mylegacycontainer As New Container.Legacy.SFDLFile
        Dim _mycontainer_session As ContainerSession
        Dim _decrypt_password As String
        Dim _decrypt As New SFDL.Container.Decrypt
        Dim _country_code As String = String.Empty

        AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent

        ActiveTasks.Add(_mytask)

        _mytask.SetTaskStatus(TaskStatus.Running, String.Format("SFDL Datei '{0}' wird geöffnet...", _sfdl_container_path))

        Try

            Dim _bulk_result As Boolean

            Select Case GetContainerVersion(_sfdl_container_path)

                Case 0 'Invalid
                    Throw New Exception(String.Format("'{0}' - Diese SFDL Datei ist mit dieser Programmversion nicht kompatibel!", IO.Path.GetFileName(_sfdl_container_path)))

                Case <= 5 'SFDL v1 - not supported anymore
                    Throw New Exception(String.Format("'{0}' - Diese SFDL Datei ist mit dieser Programmversion nicht kompatibel!", IO.Path.GetFileName(_sfdl_container_path)))

                Case <= 9 'SFDL v2  - try to convert
                    _mylegacycontainer = CType(XMLHelper.XMLDeSerialize(_mylegacycontainer, _sfdl_container_path), SFDL.Container.Legacy.SFDLFile)
                    Converter.ConvertSFDLv2ToSFDLv3(_mylegacycontainer, _mycontainer)

                Case > 10 'Invalid
                    Throw New Exception(String.Format("'{0}' - Diese SFDL Datei ist mit dieser Programmversion nicht kompatibel!", IO.Path.GetFileName(_sfdl_container_path)))

                Case Else 'Valid v3 Container

                    _mycontainer = XMLHelper.XMLDeSerialize(_mycontainer, _sfdl_container_path)

            End Select


            If _mycontainer.Encrypted = True Then

                Try
Decrypt:
                    _decrypt_password = Await MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.ShowInputAsync(Me, "SFDL entschlüsseln", String.Format("Bitte gib ein Passwort ein um den SFDL Container {0} zu entschlüsseln", IO.Path.GetFileName(_sfdl_container_path)))

                    If String.IsNullOrWhiteSpace(_decrypt_password) Then
                        Throw New Exception(String.Format("'{0}' - SFDL entschlüsseln abgebrochen", IO.Path.GetFileName(_sfdl_container_path)))
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

            CheckAndFixPackageName(_mycontainer_session)

            GenerateContainerFingerprint(_mycontainer_session)

            If Not ContainerSessions.Where(Function(mycon) mycon.Fingerprint.Equals(_mycontainer_session.Fingerprint)).Count = 0 Then
                Throw New Exception(String.Format("SFDL Container '{0}' ist bereits geöffnet!", IO.Path.GetFileName(_sfdl_container_path)))
            End If

            If Not _mycontainer_session.ContainerFile.Packages.Where(Function(mypackage) mypackage.BulkFolderMode = True).Count = 0 Then
                _bulk_result = Await Task.Run(Function() GetBulkFileList(_mycontainer_session))
            End If

            GenerateContainerSessionDownloadItems(_mycontainer_session, _settings.NotMarkAllContainerFiles)

            If _bulk_result = False Or _mycontainer_session.DownloadItems.Count = 0 Then
                Throw New Exception(String.Format("'{0}' - Öffnen fehlgeschlagen - Bulk Package konnte nicht gelesen werden", IO.Path.GetFileName(_sfdl_container_path)))
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

                    _searchpattern = New Regex("^((?!\.part(?!0*1\.rar$)\d+\.rar$).)*\.(?:rar|r?0*1)$") 'THX @ http://stackoverflow.com/a/2537935

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


            For Each _item In _mycontainer_session.DownloadItems
                DownloadItems.Add(_item)
            Next

            ContainerSessions.Add(_mycontainer_session)


            If _bulk_result = False And Not _mycontainer_session.DownloadItems.Count = 0 Then
                _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format("SFDL '{0}' teilweise geöffnet - Ein oder mehrere Packages konnten nicht gelesen werden.", IO.Path.GetFileName(_sfdl_container_path)))
            Else

                If _settings.DeleteSFDLAfterOpen = True Then
                    IO.File.Delete(_sfdl_container_path)
                    _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format("SFDL '{0}' geöffnet und anschließend gelöscht", IO.Path.GetFileName(_sfdl_container_path)))
                Else
                    _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format("SFDL '{0}' geöffnet", IO.Path.GetFileName(_sfdl_container_path)))
                End If

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

            If String.IsNullOrWhiteSpace(_settings.DownloadDirectory) Then
                Throw New Exception("Du hat keinen Download Pfad in den Einstellungen hinterlegt!")
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

    Private Function CalculateETA(ByVal _mytask As AppTask) As Boolean

        Dim _percent_downloaded As Integer = 0

        While SmartThreadPool.IsWorkItemCanceled = False And Application.Current.Resources("DownloadStopped") = False

            Dim _total_speed As Double = 0
            Dim _total_size As Double = 0
            Dim _total_size_downloaded As Double = 0
            Dim _size_remaining As Double = 0
            Dim _time_remaining As Double = 0
            Dim _percent_done As Integer = 0

            Try

                If Me.WindowState = WindowState.Maximized Or Me.WindowState = WindowState.Normal Then 'Nur berechnen wenn Window Sichtbar


                    _total_speed = DownloadItems.Where(Function(myitem) Not myitem.DownloadStatus = DownloadItem.Status.None).Sum(Function(_item)

                                                                                                                                      Dim _rt As Integer = 0

                                                                                                                                      If Not _item.DownloadStatus = DownloadItem.Status.Stopped Then 'ToDO: Prüfen ob das sinn macht

                                                                                                                                          If Not String.IsNullOrWhiteSpace(_item.DownloadSpeed) Then

                                                                                                                                              Dim _raw_speed As String = _item.DownloadSpeed.ToString

                                                                                                                                              If _raw_speed.Contains("KB/s") Then
                                                                                                                                                  _raw_speed = _raw_speed.Replace("KB/s", "").Trim
                                                                                                                                                  _rt += Double.Parse(_raw_speed)
                                                                                                                                              Else

                                                                                                                                                  If _raw_speed.Contains("MB/s") Then
                                                                                                                                                      _raw_speed = _raw_speed.Replace("MB/s", "").Trim
                                                                                                                                                      _rt += Double.Parse(_raw_speed) * 1024
                                                                                                                                                  End If

                                                                                                                                              End If

                                                                                                                                          End If

                                                                                                                                      End If

                                                                                                                                      Return _rt

                                                                                                                                  End Function)

                    _total_size = DownloadItems.Where(Function(myitem) Not myitem.DownloadStatus = DownloadItem.Status.None).Select(Function(_item) _item.FileSize).Sum

                    _total_size_downloaded = DownloadItems.Where(Function(myitem) Not myitem.DownloadStatus = DownloadItem.Status.None).Select(Function(_item) _item.SizeDownloaded).Sum


                    _percent_done = CInt((_total_size_downloaded / _total_size) * 100)

                    If Not _percent_done = _percent_downloaded Then

                        _percent_downloaded = _percent_done

                        _size_remaining = _total_size - _total_size_downloaded

                        If _size_remaining > 0 And _total_speed > 0 Then

                            _time_remaining = Math.Round(Double.Parse(CStr(((_size_remaining / 1024) / _total_speed) / 60)), 2)

                            If _total_speed >= 1024 Then
                                _mytask.SetTaskStatus(TaskStatus.Running, String.Format("Download läuft - Speed: {0} MB/s | ETA: {1} | {2} %", Math.Round(_total_speed / 1024, 2), ConvertDecimal2Time(_time_remaining), CInt((_total_size_downloaded / _total_size) * 100)))
                            Else
                                _mytask.SetTaskStatus(TaskStatus.Running, String.Format("Download läuft - Speed: {0} KB/s | ETA: {1} | {2} %", Math.Round(_total_speed, 2), ConvertDecimal2Time(_time_remaining), CInt((_total_size_downloaded / _total_size) * 100)))
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
                System.Threading.Thread.Sleep(500)
            End Try

        End While

        Debug.WriteLine("ETA While beendet!")

        Return True

    End Function

    Private Sub QueryDownloadItems()

        Dim _download_helper As New DownloadHelper
        Dim _log As NLog.Logger = NLog.LogManager.GetLogger("QueryDownloadItems")

        AddHandler _download_helper.ServerFull, AddressOf ServerFullEvent

        For Each _session In ContainerSessions.Where(Function(mysession) mysession.SessionState = ContainerSessionState.Queued Or mysession.SessionState = ContainerSessionState.DownloadRunning)

            Dim _wig As IWorkItemsGroup
            Dim _wig_start As New WIGStartInfo
            Dim _thread_pull_count As Integer
            Dim _threads_availible As Integer = 0

            SyncLock _session.SynLock

                If IsNothing(_session.WIG) Then

                    _wig_start.CallToPostExecute = CallToPostExecute.Always
                    _wig_start.PostExecuteWorkItemCallback = AddressOf DownloadCompleteCallback

                    _wig = _stp.CreateWorkItemsGroup(_session.ContainerFile.MaxDownloadThreads, _wig_start)

                    _session.WIG = _wig

                End If

                If _session.SingleSessionMode = True Then
                    _session.WIG.Concurrency = 1
                End If

                _log.Debug("STP InUseTHreads:{0}", _stp.InUseThreads)

                _threads_availible = _stp.MaxThreads - _stp.InUseThreads

                _log.Debug("Threads availible:{0}", _threads_availible)

                If _threads_availible >= _session.ContainerFile.MaxDownloadThreads Then
                    _thread_pull_count = _session.ContainerFile.MaxDownloadThreads
                Else
                    _thread_pull_count = _threads_availible
                End If

                _threads_availible -= _thread_pull_count

                _log.Debug("InUseTHreads:{0}", _session.WIG.InUseThreads)
                _log.Debug("Waiting Callbacks:{0}", _session.WIG.WaitingCallbacks)

                _thread_pull_count -= _session.WIG.InUseThreads

                _log.Debug("Threads to pull: {0}", _thread_pull_count)

                For Each _dlitem In DownloadItems.Where(Function(myitem) (myitem.ParentContainerID.Equals(_session.ID) And (myitem.DownloadStatus = DownloadItem.Status.Queued Or myitem.DownloadStatus = DownloadItem.Status.Retry))).Take(_thread_pull_count)

                    If _session.DownloadStartedTime = Date.MinValue And ContainerSessionState.Queued Then
                        _session.DownloadStartedTime = Now
                        _session.SessionState = ContainerSessionState.DownloadRunning
                    End If

                    _dlitem.SizeDownloaded = 0
                    _dlitem.RetryPossible = False
                    _dlitem.DownloadStatus = DownloadItem.Status.Running

                    _session.WIG.QueueWorkItem(New Func(Of DownloadItem, String, SFDL.Container.Connection, Boolean, DownloadItem)(AddressOf _download_helper.DownloadContainerItem), _dlitem, _settings.DownloadDirectory, _session.ContainerFile.Connection, False, WorkItemPriority.Normal)

                Next

            End SyncLock

        Next

    End Sub

    Private Sub StartDownload()

        Dim _log As NLog.Logger = NLog.LogManager.GetLogger("StartDownload")
        Dim _mytask As New AppTask("Download wird gestartet...", "ETATask")
        Dim _error As Boolean = False

        Try

            _stp = New SmartThreadPool
            _stp.MaxThreads = _settings.MaxDownloadThreads + 1 '+1 for ETA Thread

            AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent

            ActiveTasks.Add(_mytask)




#Region "Cleanup"

            Application.Current.Resources("DownloadStopped") = False
            Application.Current.Resources("UnRARBlock") = False

            For Each _session In ContainerSessions
                _session.SessionState = ContainerSessionState.Queued
                _session.SingleSessionMode = False
                _session.WIG = Nothing
                _session.UnRARSynLock = New Object
                _session.SynLock = New Object
                _session.DownloadStartedTime = Date.MinValue
                _session.DownloadStoppedTime = Date.MinValue
            Next

            For Each _dlitem In DownloadItems.Where(Function(myitem) myitem.isSelected = True)
                _dlitem.DownloadStatus = DownloadItem.Status.Queued
                _dlitem.DownloadProgress = 0
                _dlitem.DownloadSpeed = String.Empty
                _dlitem.SingleSessionMode = False
                _dlitem.RetryCount = 0
                _dlitem.RetryPossible = False
                _dlitem.SizeDownloaded = 0
            Next

#End Region

            If PreDownloadCheck(_mytask) = False Then
                Throw New Exception("PreDownload Check Fehlgeschlagen!")
            End If

            Application.Current.Resources("DownloadStopped") = False
            Me.ButtonDownloadStartStop = False

            _mytask.SetTaskStatus(TaskStatus.Running, "Download läuft...")

            _eta_thread = _stp.QueueWorkItem(New Func(Of AppTask, Boolean)(AddressOf CalculateETA), _mytask)

            QueryDownloadItems()

        Catch ex As Exception
            _log.Error(ex, ex.Message)
            _error = True
        End Try

    End Sub

    Private Async Sub DownloadCompleteCallback(wir As IWorkItemResult)

        Dim _log As NLog.Logger = NLog.LogManager.GetLogger("DownloadCompleteCallback")
        Dim _unrar_task As AppTask
        Dim _mysession As ContainerSession
        Dim _item As DownloadItem

        Try

            _item = wir.Result

            _mysession = ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID))

            _log.Info("Prüfe etwas entpackt werden kann...")

            If Not _mysession.UnRarChains.Count = 0 And _settings.UnRARSettings.UnRARAfterDownload = True Then

                Application.Current.Resources("UnRARBlock") = True

                For Each _chain In _mysession.UnRarChains

                    If (isUnRarChainComplete(_chain) = True And _chain.UnRARDone = False) And _chain.UnRARRunning = False Then

                        _chain.UnRARRunning = True

                        _log.Debug("Chain {0} ist komplett!", _chain.MasterUnRarChainFile.FileName.ToString)

                        If _settings.UnRARSettings.UnRARAfterDownload = True And _chain.UnRARDone = False Then

                            _unrar_task = New AppTask(String.Format("Archiv {0} wird entpackt....", IO.Path.GetFileName(_chain.MasterUnRarChainFile.LocalFile)))

                            AddHandler _unrar_task.TaskDone, AddressOf TaskDoneEvent

                            ActiveTasks.Add(_unrar_task)

                            'TODO: Block Application Exit while UnRAR is Running
                            Await UnRAR(_chain, _unrar_task, _settings.UnRARSettings)

                            _chain.UnRARDone = True

                        Else
                            '_block_app_exit = False
                            '_unrar_active = False
                        End If

                        _chain.UnRARRunning = False

                    Else
                        _log.Info("UnRARChain ist noch nicht vollständig oder diese wird bereits entpackt/bearbeitet")
                        'TODO: Check for InstatnVideo
                    End If

                Next

                Application.Current.Resources("UnRARBlock") = False

            Else
                _log.Info("Dieser Container hat keine UnRarChain")
            End If


            Await System.Threading.Tasks.Task.Run(Sub()

#Region "Check if Download or Any Session is Complete"

                                                      SyncLock _mysession.SynLock

                                                          If _mysession.SessionState = ContainerSessionState.Queued Or _mysession.SessionState = ContainerSessionState.DownloadRunning Then

                                                              If DownloadItems.Where(Function(myitem) (myitem.DownloadStatus = DownloadItem.Status.Queued Or myitem.DownloadStatus = DownloadItem.Status.Running) Or (myitem.DownloadStatus = DownloadItem.Status.Retry Or myitem.DownloadStatus = DownloadItem.Status.RetryWait)).Count = 0 Then 'Alle Items sind heruntergeladen

                                                                  _mysession.SessionState = ContainerSessionState.DownloadComplete
                                                                  _mysession.DownloadStoppedTime = Now
#Region "Speedreport"
                                                                  If _settings.SpeedReportSettings.SpeedreportEnabled = True Then

                                                                      Dim _speedreport As String = String.Empty
                                                                      Dim _sr_filepath As String = String.Empty
                                                                      Dim _sr_task As New AppTask("Speedreport wird erstellt")

                                                                      Try

                                                                          AddHandler _sr_task.TaskDone, AddressOf TaskDoneEvent

                                                                          ActiveTasks.Add(_sr_task)

                                                                          _speedreport = SpeedreportHelper.GenerateSpeedreport(_mysession, _settings.SpeedReportSettings)
                                                                          'ToDO: Caution: Post Action!

                                                                          If String.IsNullOrWhiteSpace(_speedreport) Then
                                                                              Throw New Exception("Speedreport failed")
                                                                          End If

                                                                          _sr_filepath = IO.Path.GetDirectoryName(_mysession.DownloadItems(0).LocalFile)

                                                                          _sr_filepath = IO.Path.Combine(_sr_filepath, "speedreport.txt")

                                                                          My.Computer.FileSystem.WriteAllText(_sr_filepath, _speedreport, False, System.Text.Encoding.Default)

                                                                          _sr_task.SetTaskStatus(TaskStatus.RanToCompletion, "Speedreport erstellt")

                                                                      Catch ex As Exception
                                                                          _sr_task.SetTaskStatus(TaskStatus.Faulted, "Speedreport Generation failed")
                                                                      End Try

                                                                  End If

#End Region
                                                              End If


                                                          End If

                                                      End SyncLock
#End Region
                                                  End Sub)


            If ContainerSessions.Where(Function(mysession) mysession.SessionState = ContainerSessionState.Queued Or mysession.SessionState = ContainerSessionState.DownloadRunning).Count = 0 Then

                Dim _mytask As AppTask

                'Alle DL Fertig
                _log.Info("Alle Downloads Abgeschlossen/Gestoppt")
                _eta_thread.Cancel()

                _mytask = ActiveTasks.Where(Function(mytask) mytask.TaskName = "ETATask").FirstOrDefault

                If Application.Current.Resources("DownloadStopped") = False Then
                    _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format("{0} Download beendet", Now.ToString))
                Else
                    _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format("{0} Download gestoppt", Now.ToString))
                End If

                Application.Current.Resources("DownloadStopped") = True
                Me.ButtonDownloadStartStop = True

                PostDownload()

            Else
                QueryDownloadItems()
            End If

        Catch ex As Exception
            _log.Error(ex, ex.Message)
        End Try

    End Sub

    Private Async Sub PostDownload()

        Dim _wait As Boolean = True

        Await System.Threading.Tasks.Task.Run(Sub()

                                                  While _wait = True

                                                      For Each _session In ContainerSessions

                                                          If Not _session.SessionState = ContainerSessionState.DownloadRunning And _session.UnRarChains.Where(Function(mychain) mychain.UnRARRunning = True).Count = 0 Then
                                                              _wait = False
                                                          Else
                                                              _wait = True
                                                          End If

                                                      Next

                                                  End While

                                              End Sub)


        If Me.CheckedPostDownloadShutdownComputer = True Then

            Dim _shutdown_cmd As String = "shutdown -s -t 30"

            System.Diagnostics.Process.Start("cmd", String.Format("/c {0}", _shutdown_cmd))

            DispatchService.DispatchService.Invoke(Sub()
                                                       Application.Current.Shutdown()
                                                   End Sub)

        Else
            If Me.CheckedPostDownloadExitApp = True Then

                DispatchService.DispatchService.Invoke(Sub()
                                                           Application.Current.Shutdown()
                                                       End Sub)

            End If
        End If


    End Sub


    Private Sub ServerFullEvent(_item As DownloadItem)


        System.Threading.Tasks.Task.Run(Sub()

                                            Dim _log As NLog.Logger = NLog.LogManager.GetLogger("ItemDownloadCompleteEvent")

                                            _log.Debug("Item {0} war als Download gequed und ist jetzt fertig - Reduziere aktiven Thread Count für diese Session", _item.FileName)

                                            _item.DownloadStatus = DownloadItem.Status.Queued

                                            SyncLock ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID)).SynLock

                                                ContainerSessions.Where(Function(mycontainer) mycontainer.ID.Equals(_item.ParentContainerID))(0).SingleSessionMode = True

                                                For Each _item In ContainerSessions.Where(Function(mycontainer) mycontainer.ID.Equals(_item.ParentContainerID))(0).DownloadItems
                                                    _item.SingleSessionMode = True
                                                Next

                                            End SyncLock

                                        End Sub)

    End Sub

    Private Async Sub StopDownload()

        Await System.Threading.Tasks.Task.Run(Sub()

                                                  Application.Current.Resources("DownloadStopped") = True

                                                  For Each _session In ContainerSessions
                                                      If Not IsNothing(_session.WIG) Then
                                                          _session.WIG.Cancel()
                                                      End If
                                                  Next

                                                  _eta_thread.Cancel()
                                                  '_stp.Cancel()

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

    Private _checked_exit_loader_after_download As Boolean = False
    Public Property CheckedPostDownloadExitApp As Boolean
        Set(value As Boolean)
            _checked_exit_loader_after_download = value
            RaisePropertyChanged("CheckedPostDownloadExitApp")
        End Set
        Get
            Return _checked_exit_loader_after_download
        End Get
    End Property

    Private _checked_shutdown_computer_after_download As Boolean = False
    Public Property CheckedPostDownloadShutdownComputer As Boolean
        Set(value As Boolean)
            _checked_shutdown_computer_after_download = value
            RaisePropertyChanged("CheckedPostDownloadShutdownComputer")

            If value = True Then
                Me.CheckedPostDownloadExitApp = True
            Else
                Me.CheckedPostDownloadExitApp = False
            End If

        End Set
        Get
            Return _checked_shutdown_computer_after_download
        End Get
    End Property




#End Region

#Region "Menu Commands"

    Public ReadOnly Property RemoveAllCompletedDownloadsCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf RemoveAllCompletedDownloads)
        End Get
    End Property

    Private Sub RemoveAllCompletedDownloads()

        Dim _mytask As New AppTask("Entferne/Schließe fertiggestellte Container...")
        Dim _tmp_list As New List(Of DownloadItem)
        Dim _container_session_list As List(Of ContainerSession)

        AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent

        ActiveTasks.Add(_mytask)

        _container_session_list = ContainerSessions.Where(Function(mysession) mysession.SessionState = ContainerSessionState.DownloadComplete).ToList

        For Each _session In _container_session_list

            _tmp_list = DownloadItems.Where(Function(myitem) myitem.ParentContainerID.Equals(_session.ID)).ToList

            For Each _item In _tmp_list
                DownloadItems.Remove(_item)
            Next

            ContainerSessions.Remove(_session)

        Next

        If Not _container_session_list.Count = 0 Then
            _mytask.SetTaskStatus(TaskStatus.RanToCompletion, "Fertiggestellte Container entfernt/geschlossen")
        Else
            _mytask.SetTaskStatus(TaskStatus.Faulted, "Keine fertiggestellte Container vorhanden")
        End If



    End Sub

    Public ReadOnly Property RemoveAllDownloadsCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf RemoveAllDownloads)
        End Get
    End Property

    Private Sub RemoveAllDownloads()

        Dim _mytask As New AppTask("Entferne/Schließe alle Container...")

        AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent

        ActiveTasks.Add(_mytask)

        If Application.Current.Resources("DownloadStopped") = False Then
            _mytask.SetTaskStatus(TaskStatus.Canceled, "Diese Funktion kann nicht genutzt werden so lange der Download aktiv ist")
        Else

            DownloadItems.Clear()
            ContainerSessions.Clear()

            _mytask.SetTaskStatus(TaskStatus.RanToCompletion, "Alle Container entfernt/geschlossen")

        End If

    End Sub

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
        DownloadItems.Select(Function(myitem)
                                 myitem.isSelected = True
                                 Return myitem
                             End Function).ToList
    End Sub

    Public ReadOnly Property UnmarkAllItemsCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf UnmarkAllItems)
        End Get
    End Property

    Private Sub UnmarkAllItems()
        DownloadItems.Select(Function(myitem)
                                 myitem.isSelected = False
                                 Return myitem
                             End Function).ToList
    End Sub

    Public ReadOnly Property ExpandAllPackagesCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf ExpandAllPackages)
        End Get
    End Property

    Private Sub ExpandAllPackages()

        For Each _item In DownloadItems.Select(Function(myitem) myitem.GroupDescriptionIdentifier).Distinct
            DownloadItems.Where(Function(myitem) myitem.GroupDescriptionIdentifier.Equals(_item)).FirstOrDefault.IsExpanded = True
        Next

    End Sub

    Public ReadOnly Property CollapseAllPackagesCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf CollapseAllPackages)
        End Get
    End Property

    Private Sub CollapseAllPackages()

        For Each _item In DownloadItems.Select(Function(myitem) myitem.GroupDescriptionIdentifier).Distinct
            DownloadItems.Where(Function(myitem) myitem.GroupDescriptionIdentifier.Equals(_item)).FirstOrDefault.IsExpanded = False
        Next

    End Sub

    Public ReadOnly Property ShowHelpCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf ShowHelp)
        End Get
    End Property

    Private Async Sub ShowHelp()

        Await MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.ShowMessageAsync(Me, "SFDL.NET 3", "Version: 3.0.0.0 TP3")

    End Sub


#End Region

#Region "ListView ContextMenu Commands and Properites"

    Public ReadOnly Property OpenParentFolderCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf OpenParentFolder)
        End Get
    End Property

    Private Sub OpenParentFolder(ByVal parameter As Object)

        If Not IsNothing(parameter) Then

            Dim _item As DownloadItem = TryCast(parameter, DownloadItem)
            Dim _folder_path As String

            _folder_path = IO.Path.GetDirectoryName(_item.LocalFile)

            Debug.WriteLine(_folder_path)

            If Not String.IsNullOrWhiteSpace(_folder_path) And IO.Directory.Exists(_folder_path) Then
                System.Diagnostics.Process.Start("explorer.exe", String.Format("{0}{1}{2}", Chr(34), _folder_path, Chr(34)))
            End If

        End If

    End Sub


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

        Dim _mytask As New AppTask("SFDL Container wird entfernt/geschlossen....")

        If Not IsNothing(parameter) Then

            Dim _container_session As ContainerSession = Nothing
            Dim _tmp_list As New List(Of DownloadItem)

            AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent
            ActiveTasks.Add(_mytask)

            If parameter.GetType.Equals(GetType(String)) Then

                If Not String.IsNullOrWhiteSpace(parameter) And parameter.ToString.Contains(";") Then

                    Dim _container_sessionid As Guid = Guid.Parse(parameter.ToString.Split(";")(1))

                    _container_session = ContainerSessions.Where(Function(mysession) mysession.ID.Equals(_container_sessionid)).FirstOrDefault

                End If

            End If

            If parameter.GetType.Equals(GetType(DownloadItem)) Then

                Dim _dlitem As DownloadItem = TryCast(parameter, DownloadItem)

                _container_session = ContainerSessions.Where(Function(mysession) mysession.ID.Equals(_dlitem.ParentContainerID)).FirstOrDefault

            End If


            If _container_session.SessionState = ContainerSessionState.DownloadRunning Then
                _mytask.SetTaskStatus(TaskStatus.Faulted, "Kann Session nicht schließen da diese aktiv ist (Download)")
            Else

                _tmp_list = DownloadItems.Where(Function(myitem) _container_session.ID.Equals(myitem.ParentContainerID)).ToList

                For Each _item In _tmp_list
                    DownloadItems.Remove(_item)
                Next

                _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format("SFDL Container '{0}' geschlossen", IO.Path.GetFileName(_container_session.ContainerFileName)))

                ContainerSessions.Remove(_container_session)

            End If

        End If


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

                                            ActiveTasks.Remove(e)
                                            DoneTasks.Add(e)

                                        End Sub)

    End Sub


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

    Private _max_download_speed As String
    Public Property MaxDownloadSpeed As String
        Set(value As String)

            If Not String.IsNullOrWhiteSpace(value) Then

                If IsNumeric(value) Then

                    If value >= Integer.MinValue And value <= Integer.MaxValue Then

                        _max_download_speed = value
                    Else
                        _max_download_speed = 0
                    End If

                End If

            Else
                _max_download_speed = 0
            End If

            RaisePropertyChanged("MaxDownloadSpeed")

        End Set
        Get
            Return _max_download_speed
        End Get
    End Property

#Region "DragnDrop"

    Public ReadOnly Property PreviewDropCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf HandlePreviewDrop)
        End Get
    End Property

    Private _PreviewDropCommand As ICommand
    Private Sub HandlePreviewDrop(inObject As Object)

        Dim ido As IDataObject = TryCast(inObject, IDataObject)

        If ido Is Nothing Then
            Return
        End If

        For Each _lol In ido.GetFormats()

            Debug.WriteLine(_lol.ToString)

        Next


        For Each _file In ido.GetData("FileNameW")

            If IO.Path.GetExtension(_file) = ".sfdl" Then
                OpenSFDLFile(_file)
            End If

        Next


    End Sub
#End Region


End Class
