﻿Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Windows.Forms
Imports Amib.Threading
Imports ArxOne.Ftp
Imports MahApps.Metro.Controls.Dialogs
Imports NLog
Imports SFDL.Container
Imports SFDL.Container.Legacy
Imports OpenFileDialog = Microsoft.Win32.OpenFileDialog

Public Class MainViewModel
    Inherits ViewModelBase

    Private _settings As New Settings

    Private _stp As New SmartThreadPool
    Private _lock_active_tasks As New Object
    Private _lock_done_tasks As New Object
    Private _lock_download_items As New Object
    Private _lock_container_sessions As New Object
    Private _eta_thread As IWorkItemResult(Of Boolean)
    Private _download_helper As New DownloadHelper

    Public Sub UpdateSettings()

        _settings = CType(Application.Current.Resources("Settings"), Settings)

    End Sub

    Public Sub New()

        _instance = Me
        _settings = CType(Application.Current.Resources("Settings"), Settings)
        Application.Current.Resources("DownloadStopped") = True

        'Init ThreadSafe Observ Collections

        BindingOperations.EnableCollectionSynchronization(ActiveTasks, _lock_active_tasks)
        BindingOperations.EnableCollectionSynchronization(DoneTasks, _lock_done_tasks)
        BindingOperations.EnableCollectionSynchronization(DownloadItems, _lock_download_items)
        BindingOperations.EnableCollectionSynchronization(ContainerSessions, _lock_container_sessions)

        CreateView()

        LoadSavedSessions()

        NewUpdateAvailableVisibility = New NotifyTaskCompletion(Of Visibility)(IsNewUpdateAvailible(_settings))


    End Sub

#Region "Public Subs"

    Public Sub SaveSessions()

        Dim _path As String = Path.Combine(Environment.GetEnvironmentVariable("appdata"), "SFDL.NET 3", "Sessions")
        Dim _log As Logger = LogManager.GetLogger("SaveSessions")

        If Directory.Exists(_path) = False Then
            Directory.CreateDirectory(_path)
        End If

        For Each _file In Directory.GetFiles(_path, "*.session")
            File.Delete(_file)
        Next

        For Each _session In ContainerSessions

            Try
                _log.Info(String.Format("Saving Session {0}", _session.ID.ToString()))
                XMLSerialize(_session, Path.Combine(_path, _session.ID.ToString & ".session"))

            Catch ex As Exception
                _log.Error(ex, ex.Message)
            End Try

        Next

    End Sub

#End Region

#Region "Private Subs"

    Private Sub LoadSavedSessions()

        Dim _path As String = Path.Combine(Environment.GetEnvironmentVariable("appdata"), "SFDL.NET 3", "Sessions")
        Dim _log As Logger = LogManager.GetLogger("LoadSavedSessions")


        If Directory.Exists(_path) Then

            For Each _file In Directory.GetFiles(_path, "*.session")

                Dim _new_session As New ContainerSession

                Try

                    _log.Info(String.Format("Loading Sessions {0}", _file))

                    _new_session = CType(XMLDeSerialize(_new_session, _file), ContainerSession)
                    _new_session.WIG = Nothing
                    _new_session.DownloadStartedTime = Date.MinValue
                    _new_session.DownloadStoppedTime = Date.MinValue
                    _new_session.SessionState = ContainerSessionState.Queued
                    _new_session.SingleSessionMode = False
                    _new_session.SynLock = New Object

                    For Each _chain In _new_session.UnRarChains
                        _chain.UnRARRunning = False
                    Next

                    _new_session.InitCollectionSync()

                    'GenerateContainerSessionChains(_new_session)

                    For Each _item In _new_session.DownloadItems

                        'Update DownloadPath cause it could have changed
                        _item.LocalFile = GetDownloadFilePath(CType(Application.Current.Resources("Settings"), Settings), _new_session, _item)
                        '_item.DownloadStatus = DownloadItem.Status.None
                        _item.DownloadProgress = 0
                        _item.DownloadSpeed = String.Empty
                        _item.SingleSessionMode = False
                        _item.RetryCount = 0
                        _item.RetryPossible = False
                        _item.LocalFileSize = 0
                        _item.SizeDownloaded = 0

                        DownloadItems.Add(_item)

                    Next

                    _new_session.LocalDownloadRoot = GetSessionLocalDownloadRoot(_new_session, _settings)

                    ContainerSessions.Add(_new_session)

                    File.Delete(_file)

                Catch ex As Exception
                    _log.Error(ex, ex.Message)
                End Try

            Next

        End If



    End Sub

    Private Sub CreateView()

        Dim view As CollectionView = DirectCast(CollectionViewSource.GetDefaultView(DownloadItems), CollectionView)

        Dim groupDescription As PropertyGroupDescription

        groupDescription = New PropertyGroupDescription("GroupDescriptionIdentifier")
        view.GroupDescriptions.Add(groupDescription)

        groupDescription = New PropertyGroupDescription("PackageName")
        view.GroupDescriptions.Add(groupDescription)

        If _settings.InstantVideo = True Then
            view.SortDescriptions.Add(New SortDescription("RequiredForInstantVideo", ListSortDirection.Descending))
        End If

    End Sub

    Friend Async Sub OpenSFDLFile(ByVal _sfdl_container_path As String)

        Dim _mytask As New AppTask("")
        Dim _mycontainer As New Container.Container
        Dim _mylegacycontainer As New SFDLFile
        Dim _mycontainer_session As ContainerSession
        Dim _decrypt_password As String
        Dim _decrypt As New Decrypt

        AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent

        ActiveTasks.Add(_mytask)

        If Me.WindowState = WindowState.Minimized Then
            Me.WindowState = WindowState.Normal
        End If

        WindowInstance.Activate()
        WindowInstance.Focus()


        _mytask.SetTaskStatus(TaskStatus.Running, String.Format(My.Resources.Strings.OpenSFDL_AppTask_Start_Message, _sfdl_container_path))

        Try

            Dim _bulk_result As Boolean = False

            Select Case GetContainerVersion(_sfdl_container_path)

                Case 0 'Invalid
                    Throw New Exception(String.Format(My.Resources.Strings.OpenSFDL_Exception_SFDLFileNotCompatible, Path.GetFileName(_sfdl_container_path)))

                Case <= 5 'SFDL v1 - not supported anymore
                    Throw New Exception(String.Format(My.Resources.Strings.OpenSFDL_Exception_SFDLFileNotCompatible, Path.GetFileName(_sfdl_container_path)))

                Case <= 9 'SFDL v2  - try to convert
                    _mylegacycontainer = CType(XMLDeSerialize(_mylegacycontainer, _sfdl_container_path), SFDLFile)
                    Converter.ConvertSFDLv2ToSFDLv3(_mylegacycontainer, _mycontainer)

                Case > 10 'Invalid
                    Throw New Exception(String.Format(My.Resources.Strings.OpenSFDL_Exception_SFDLFileNotCompatible, Path.GetFileName(_sfdl_container_path)))

                Case Else 'Valid v3 Container

                    _mycontainer = CType(XMLDeSerialize(_mycontainer, _sfdl_container_path), Container.Container)

            End Select


            If _mycontainer.Encrypted = True Then

                Try
Decrypt:
                    _decrypt_password = Await DialogCoordinator.Instance.ShowInputAsync(Me, My.Resources.Strings.OpenSFDL_Prompt_Decrypt_Title, String.Format(My.Resources.Strings.OpenSFDL_Prompt_Decrypt_Message, Path.GetFileName(_sfdl_container_path)))

                    If String.IsNullOrWhiteSpace(_decrypt_password) Then
                        Throw New Exception(String.Format(My.Resources.Strings.OpenSFDL_Decrypt_Aborted_Message, Path.GetFileName(_sfdl_container_path)))
                    End If

                    _decrypt.DecryptString(_mycontainer.Connection.Host, _decrypt_password)

                Catch ex As FalsePasswordException
                    GoTo Decrypt
                End Try

                DecryptSFDLContainer(_mycontainer, _decrypt_password)

            End If

            _mycontainer_session = New ContainerSession()
            _mycontainer_session.Init(_mycontainer)
            _mycontainer_session.ContainerFileName = Path.GetFileNameWithoutExtension(_sfdl_container_path)
            _mycontainer_session.ContainerFilePath = _sfdl_container_path

            If String.IsNullOrWhiteSpace(_mycontainer.Description) Then
                _mycontainer_session.DisplayName = _mycontainer_session.ContainerFileName
            Else
                _mycontainer_session.DisplayName = _mycontainer.Description
            End If

            CheckAndFixPackageName(_mycontainer_session)

            GenerateContainerFingerprint(_mycontainer_session)

            If Not ContainerSessions.Where(Function(mycon) mycon.Fingerprint.Equals(_mycontainer_session.Fingerprint)).Count = 0 Then
                Throw New Exception(String.Format(My.Resources.Strings.OpenSFDL_Exception_SFDLContainerAlreadyOpen, Path.GetFileName(_sfdl_container_path)))
            End If

            If Not _mycontainer_session.ContainerFile.Packages.Where(Function(mypackage) mypackage.BulkFolderMode = True).Count = 0 Then
                _bulk_result = Await Task.Run(Function() GetBulkFileList(_mycontainer_session))
            Else
                _bulk_result = True
            End If

            GenerateContainerSessionDownloadItems(_mycontainer_session, _settings.NotMarkAllContainerFiles, _settings.DownloadItemBlacklist.ToList)


            If _bulk_result = False Or _mycontainer_session.DownloadItems.Count = 0 Then
                Throw New Exception(String.Format(My.Resources.Strings.OpenSFDL_Exception_FTPDown, Path.GetFileName(_sfdl_container_path)))
            End If

            GenerateContainerSessionChains(_mycontainer_session)

            For Each _item In _mycontainer_session.DownloadItems
                DownloadItems.Add(_item)
            Next

            _mycontainer_session.LocalDownloadRoot = GetSessionLocalDownloadRoot(_mycontainer_session, _settings)

            ContainerSessions.Add(_mycontainer_session)


            If _bulk_result = False And Not _mycontainer_session.DownloadItems.Count = 0 Then
                _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format(My.Resources.Strings.OpenSFDL_AppTask_Faulted_Message, Path.GetFileName(_sfdl_container_path)))
            Else

                If _settings.DeleteSFDLAfterOpen = True Then
                    File.Delete(_sfdl_container_path)
                    _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format(My.Resources.Strings.OpenSFDL_AppTask_Completed_1_Message, Path.GetFileName(_sfdl_container_path)))
                Else
                    _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format(My.Resources.Strings.OpenSFDL_AppTask_Completed_2_Message, Path.GetFileName(_sfdl_container_path)))
                End If

            End If

        Catch ex As Exception
            _mytask.SetTaskStatus(TaskStatus.Faulted, ex.Message)
        End Try


    End Sub

    Private Function PreDownloadCheck(ByVal _task As AppTask) As Boolean

        Dim _rt As Boolean = True

        Try

            If DownloadItems.Where(Function(myitem) myitem.isSelected = True).Count = 0 Then
                Throw New Exception(My.Resources.Strings.PreDownloadCheck_Exception_NothingSelected)
            End If

            If String.IsNullOrWhiteSpace(_settings.DownloadDirectory) Then
                Throw New Exception(My.Resources.Strings.PreDownloadCheck_Exception_NoDownloadDirectory)
            End If

            If Directory.Exists(_settings.DownloadDirectory) = False Then
                Throw New Exception(My.Resources.Strings.PreDownloadCheck_Exception_DownloadDirectoryNotExists)
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

        Dim _log As Logger = LogManager.GetLogger("CalculateETA")

        While SmartThreadPool.IsWorkItemCanceled = False

            Dim _total_speed As Double = 0
            Dim _total_size As Double = 0
            Dim _total_size_downloaded As Double = 0
            Dim _size_remaining As Double = 0
            Dim _time_remaining As Double = 0
            Dim _percent_done As Integer = 0

            Try

                If WindowState = WindowState.Maximized Or WindowState = WindowState.Normal Then 'Nur berechnen wenn Window Sichtbar

                    System.Threading.Tasks.Parallel.ForEach(Of DownloadItem)(DownloadItems, Sub(_item As DownloadItem)

                                                                                                    If _item.DownloadStatus = DownloadItem.Status.Running Then

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

                                                                                                    End If

                                                                                                End Sub)


                    _total_size = DownloadItems.Where(Function(myitem) Not myitem.DownloadStatus = DownloadItem.Status.None).Select(Function(_item) _item.FileSize).Sum
                    _total_size_downloaded = DownloadItems.Where(Function(myitem) Not myitem.DownloadStatus = DownloadItem.Status.None).Select(Function(_item) _item.SizeDownloaded).Sum

                    _percent_done = CInt((_total_size_downloaded / _total_size) * 100)

                        _size_remaining = _total_size - _total_size_downloaded

                    If _size_remaining > 0 And _total_speed > 0 Then

                        _time_remaining = Math.Round(Double.Parse(CStr(((_size_remaining / 1024) / _total_speed) / 60)), 2)

                        If _mytask.TaskStatus = TaskStatus.Running Then

                            Application.Current.Dispatcher.BeginInvoke(New Action(Sub()
                                                                                      WindowInstance.TaskbarItemInfo.ProgressValue = _percent_done / 100.0
                                                                                  End Sub))

                            If _total_speed >= 1024 Then
                                _mytask.SetTaskStatus(TaskStatus.Running, String.Format(My.Resources.Strings.ETA_AppTask_Status_1_Message, Math.Round(_total_speed / 1024, 2), ConvertDecimal2Time(_time_remaining), _percent_done))
                            Else
                                _mytask.SetTaskStatus(TaskStatus.Running, String.Format(My.Resources.Strings.ETA_AppTask_Status_4_Message, Math.Round(_total_speed, 2), ConvertDecimal2Time(_time_remaining), _percent_done))
                            End If

                        Else
                            _mytask.SetTaskStatus(TaskStatus.Running, String.Format(My.Resources.Strings.ETA_AppTask_Status_2_Message, CInt((_total_size_downloaded / _total_size) * 100)))
                        End If

                    Else
                        '_log.Debug("Nothing to Calculate")
                    End If


                Else
                    _log.Debug("Skipping Calculate - Window is Minimized")
                End If

            Catch ex As Exception
                _log.Error(ex, ex.Message)
            Finally
                Thread.Sleep(500)
            End Try

        End While

        _log.Debug("ETA While exited!")

        DispatchService.DispatchService.Invoke(Sub()
                                                   WindowInstance.TaskbarItemInfo.ProgressState = Shell.TaskbarItemProgressState.None
                                               End Sub)

        PostDownload()

        Return True

    End Function

    Sub PostDownload()

        Dim _mytasklist As New List(Of AppTask)

        Try

            _mytasklist = ActiveTasks.Where(Function(mytask) mytask.TaskName = "ETATask").ToList

            For Each _mytask As AppTask In _mytasklist

                If CBool(Application.Current.Resources("DownloadStopped")) = False Then
                    _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format(My.Resources.Strings.PostDownload_AppTask_Completed_2_Message, Now.ToString))
                Else
                    _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format(My.Resources.Strings.PostDownload_AppTask_Completed_1_Message, Now.ToString))
                End If

            Next

            _download_helper.Dispose()
            RemoveHandler _download_helper.ServerFull, AddressOf ServerFullEvent

        Catch ex As Exception

        End Try

        Application.Current.Resources("DownloadStopped") = True
        ButtonDownloadStartStop = True

        Tasks.Task.Run(Sub()
                           ExcutePostDownloadActions()
                       End Sub)

    End Sub

    Private Sub QueryDownloadItems()

        Dim _log As Logger = LogManager.GetLogger("QueryDownloadItems")

#Region "Update STP with Current Settings"

        _stp.MaxThreads = _settings.MaxDownloadThreads + 1 '+1 for ETA Thread

#End Region

        For Each _session In ContainerSessions.Where(Function(mysession) mysession.SessionState = ContainerSessionState.Queued Or mysession.SessionState = ContainerSessionState.DownloadRunning)

            Dim _wig As IWorkItemsGroup
            Dim _wig_start As New WIGStartInfo
            Dim _ssm_flag As Boolean = False
            Dim DLItemQuery As IEnumerable(Of DownloadItem)
            Dim _args As New DownloadContainerItemsArgs

            SyncLock _session.SynLock

                If IsNothing(_session.WIG) Then

                    _wig_start.CallToPostExecute = CallToPostExecute.Always
                    _wig_start.PostExecuteWorkItemCallback = AddressOf DownloadCompleteCallback

                    _wig = _stp.CreateWorkItemsGroup(_session.ContainerFile.MaxDownloadThreads, _wig_start)

                    _session.WIG = _wig

                End If

                If _session.SingleSessionMode = True Or _stp.MaxThreads - 1 = 1 Then '-1 for ETA Thread
                    _session.WIG.Concurrency = 1
                    _args.SingleSessionMode = True
                End If


                DLItemQuery = (From myitem In DownloadItems Where myitem.ParentContainerID.Equals(_session.ID) And (myitem.isSelected = True And IsNothing(myitem.IWorkItemResult) = True) Or myitem.DownloadStatus = DownloadItem.Status.Retry)

                For Each _dlitem In DLItemQuery

                    Dim _prio As WorkItemPriority = WorkItemPriority.Normal

                    If _session.DownloadStartedTime = Date.MinValue And _session.SessionState = ContainerSessionState.Queued Then
                        _session.DownloadStartedTime = Now
                        _session.SessionState = ContainerSessionState.DownloadRunning
                    End If

                    _dlitem.SizeDownloaded = 0

                    _dlitem.LocalFileSize = 0

                    _dlitem.RetryPossible = False

                    With _args
                        .ConnectionInfo = _session.ContainerFile.Connection
                        .DownloadDirectory = _settings.DownloadDirectory
                    End With

                    _log.Debug(String.Format("Spooling Item {0}", _dlitem.FileName))

                    If _settings.InstantVideo = True And _dlitem.RequiredForInstantVideo = True Then
                        _prio = WorkItemPriority.AboveNormal
                    End If

                    If _dlitem.DownloadStatus = DownloadItem.Status.Retry Then
                        _prio = WorkItemPriority.Highest
                        _args.RetryMode = True
                    End If

                    _dlitem.DownloadStatus = DownloadItem.Status.Queued

                    _dlitem.IWorkItemResult = _session.WIG.QueueWorkItem(New Func(Of DownloadItem, DownloadContainerItemsArgs, DownloadItem)(AddressOf _download_helper.DownloadContainerItem), _dlitem, _args, _prio)

                Next


            End SyncLock

        Next

    End Sub

    Private Sub StartDownload()

        Dim _log As Logger = LogManager.GetLogger("StartDownload")
        Dim _mytask As New AppTask(My.Resources.Strings.ETA_AppTask_Start, "ETATask")
        Dim _error As Boolean = False

        Try

            _stp = New SmartThreadPool
            _stp.MaxThreads = _settings.MaxDownloadThreads + 1 '+1 for ETA Thread

            AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent

            ActiveTasks.Add(_mytask)

#Region "Cleanup"


            _download_helper = New DownloadHelper
            AddHandler _download_helper.ServerFull, AddressOf ServerFullEvent

            Application.Current.Resources("DownloadStopped") = False
            ButtonInstantVideoEnabled = False

            System.Threading.Tasks.Parallel.ForEach(ContainerSessions, Sub(_session)

                                                                           If _session.DownloadItems.Where(Function(myitem) myitem.isSelected = True).Count > 0 Then
                                                                               _session.SessionState = ContainerSessionState.Queued
                                                                           Else
                                                                               _session.SessionState = ContainerSessionState.None
                                                                           End If
                                                                           _session.SingleSessionMode = False
                                                                           _session.WIG = Nothing
                                                                           _session.SynLock = New Object
                                                                           _session.DownloadStartedTime = Date.MinValue
                                                                           _session.DownloadStoppedTime = Date.MinValue
                                                                           _session.InstantVideoStreams.Clear()

                                                                           For Each _chain In _session.UnRarChains
                                                                               _chain.UnRARRunning = False
                                                                           Next

                                                                       End Sub)

            System.Threading.Tasks.Parallel.ForEach(DownloadItems, Sub(_dlitem)

                                                                       If _dlitem.isSelected Then

                                                                           _dlitem.IWorkItemResult = Nothing
                                                                           _dlitem.DownloadProgress = 0
                                                                           _dlitem.DownloadSpeed = String.Empty
                                                                           _dlitem.SingleSessionMode = False
                                                                           _dlitem.RetryCount = 0
                                                                           _dlitem.RetryPossible = False
                                                                           _dlitem.SizeDownloaded = 0
                                                                           _dlitem.LocalFileSize = 0
                                                                           _dlitem.LocalFile = GetDownloadFilePath(Application.Current.Resources("Settings"), ContainerSessions.Where(Function(mysession) mysession.ID.Equals(_dlitem.ParentContainerID)).First, _dlitem)

                                                                       Else
                                                                           _dlitem.DownloadStatus = DownloadItem.Status.None
                                                                           _dlitem.SizeDownloaded = 0
                                                                           _dlitem.LocalFileSize = 0
                                                                           _dlitem.DownloadSpeed = String.Empty
                                                                       End If

                                                                   End Sub)


#End Region

            If PreDownloadCheck(_mytask) = False Then
                Throw New Exception("PreDownload Check has failed!")
            End If

            Application.Current.Resources("DownloadStopped") = False

            ButtonInstantVideoEnabled = False

            ButtonDownloadStartStop = False

            _mytask.SetTaskStatus(TaskStatus.Running, My.Resources.Strings.ETA_AppTask_Status_3_Message)

            DispatchService.DispatchService.Invoke(Sub()
                                                       WindowInstance.TaskbarItemInfo.ProgressState = Shell.TaskbarItemProgressState.Normal
                                                   End Sub)

            _eta_thread = _stp.QueueWorkItem(New Func(Of AppTask, Boolean)(AddressOf CalculateETA), _mytask)

            QueryDownloadItems()

        Catch ex As Exception
            _log.Error(ex, ex.Message)
            _error = True
        End Try

    End Sub

    Private Sub DownloadCompleteCallback(wir As IWorkItemResult, Optional _overrride_item As DownloadItem = Nothing)

        Dim _log As Logger = LogManager.GetLogger("DownloadCompleteCallback")
        Dim _unrar_task As AppTask
        Dim _mysession As ContainerSession
        Dim _item As DownloadItem

        Try

            If Not IsNothing(_overrride_item) Then
                _item = _overrride_item
            Else
                _item = CType(wir.Result, DownloadItem)
            End If

            If IsNothing(_item) Then
                Throw New Exception("Item Is Null")
            End If

            _mysession = ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID))


            Task.Run(Sub()

                         If Not _mysession.UnRarChains.Count = 0 And _settings.UnRARSettings.UnRARAfterDownload = True Then

                             For Each _chain In _mysession.UnRarChains

                                 If (isUnRarChainComplete(_chain) = True And _chain.UnRARDone = False) And _chain.UnRARRunning = False Then

                                     _chain.UnRARRunning = True

                                     _log.Debug("Chain {0} is complete", _chain.MasterUnRarChainFile.FileName.ToString)

                                     If _settings.UnRARSettings.UnRARAfterDownload = True And _chain.UnRARDone = False Then

                                         _unrar_task = New AppTask(String.Format(My.Resources.Strings.UnRAR_AppTask_Start_Message, Path.GetFileName(_chain.MasterUnRarChainFile.LocalFile)))

                                         AddHandler _unrar_task.TaskDone, AddressOf TaskDoneEvent

                                         ActiveTasks.Add(_unrar_task)

                                         UnRAR(_chain, _unrar_task, _settings.UnRARSettings)

                                         _chain.UnRARDone = True

                                         'If UnRAR(_chain, _unrar_task, _settings.UnRARSettings) = True Then
                                         '    _chain.UnRARDone = True
                                         'Else
                                         '    _chain.UnRARDone = False
                                         'End If

                                         _chain.UnRARRunning = False

                                     Else
                                         _log.Info("UnRARChain is not yet complete or it is already unpacked / processed ")
                                     End If

                                 Else
                                     If _settings.InstantVideo = True AndAlso IsReadyForInstantVideo(_chain) = True Then

                                         If _mysession.InstantVideoStreams.Where(Function(mystream) mystream.File.Equals(_chain.MasterUnRarChainFile.LocalFile)).Count = 0 Then

                                             Dim _instantvideo_stream As New InstantVideoStream

                                             _instantvideo_stream.DisplayName = _chain.MasterUnRarChainFile.FileName
                                             _instantvideo_stream.ParentSessionID = _mysession.ID
                                             _instantvideo_stream.File = _chain.MasterUnRarChainFile.LocalFile

                                             _mysession.InstantVideoStreams.Add(_instantvideo_stream)

                                         End If

                                         ButtonInstantVideoEnabled = True

                                     End If
                                 End If


                             Next

                         Else
                             _log.Info("This container has no UnRARChain!")
                         End If

                     End Sub)


            Task.Run(Sub()

#Region "Check If Download Or Any Session Is Complete"

                         SyncLock _mysession.SynLock

                             If _mysession.SessionState = ContainerSessionState.Queued Or _mysession.SessionState = ContainerSessionState.DownloadRunning Then

                                 Dim DLItemQuery As New List(Of DownloadItem)

                                 DLItemQuery.AddRange(_mysession.DownloadItems.Where(Function(myitem) (myitem.DownloadStatus = DownloadItem.Status.Queued)))
                                 DLItemQuery.AddRange(_mysession.DownloadItems.Where(Function(myitem) (myitem.DownloadStatus = DownloadItem.Status.Running)))
                                 DLItemQuery.AddRange(_mysession.DownloadItems.Where(Function(myitem) (myitem.DownloadStatus = DownloadItem.Status.Retry)))
                                 DLItemQuery.AddRange(_mysession.DownloadItems.Where(Function(myitem) (myitem.DownloadStatus = DownloadItem.Status.RetryWait)))
                                 DLItemQuery.AddRange(_mysession.DownloadItems.Where(Function(myitem) (myitem.isSelected = True)))

                                 If DLItemQuery.Count = 0 Or Application.Current.Resources("DownloadStopped") = True Then

                                     _mysession.SessionState = ContainerSessionState.DownloadComplete
                                         _mysession.DownloadStoppedTime = Now
#Region "Speedreport"
                                         If _settings.SpeedReportSettings.SpeedreportEnabled = True Then

                                             Dim _speedreport As String = String.Empty
                                             Dim _sr_filepath As String = String.Empty
                                         Dim _sr_task As New AppTask(My.Resources.Strings.Speedreport_AppTask_Start_Message)

                                         Try

                                                 AddHandler _sr_task.TaskDone, AddressOf TaskDoneEvent

                                                 ActiveTasks.Add(_sr_task)

                                                 _sr_filepath = _mysession.LocalDownloadRoot

                                                 _sr_filepath = Path.Combine(_sr_filepath, "speedreport.txt")

                                                 _speedreport = GenerateSpeedreport(_mysession, _settings.SpeedReportSettings)

                                                 If _speedreport.Equals("nodata") And System.IO.File.Exists(_sr_filepath) Then
                                                     Throw New NoSpeedreportDataException
                                                 End If

                                                 If String.IsNullOrWhiteSpace(_speedreport) Then
                                                 Throw New Exception("speedreport failed")
                                             End If

                                                 My.Computer.FileSystem.WriteAllText(_sr_filepath, _speedreport, False, Encoding.Default)

                                             _sr_task.SetTaskStatus(TaskStatus.RanToCompletion, String.Format(My.Resources.Strings.Speedreport_AppTask_Completed_Message, GenerateSimpleSpeedreport(_mysession)))

                                         Catch ex As NoSpeedreportDataException
                                                 _sr_task.SetTaskStatus(TaskStatus.RanToCompletion, String.Format("Speedreport | {0}", GenerateSimpleSpeedreport(_mysession)))

                                             Catch ex As Exception
                                             _sr_task.SetTaskStatus(TaskStatus.Faulted, My.Resources.Strings.Speedreport_Exception_SpeedreportFailed)
                                         End Try

                                         End If

#End Region
                                     End If


                                 End If

                         End SyncLock
#End Region
                     End Sub).ContinueWith(Sub()


                                               If ContainerSessions.Where(Function(mysession) mysession.SessionState = ContainerSessionState.Queued Or mysession.SessionState = ContainerSessionState.DownloadRunning).Count = 0 Or Application.Current.Resources("DownloadStopped") = True Then

                                                   'Alle DL Fertig
                                                   _log.Info("All downloads cancled/stopped")
                                                   _eta_thread.Cancel()

                                               Else
                                                   QueryDownloadItems()
                                               End If

                                           End Sub)




        Catch ex As Exception
            _log.Error(ex, ex.Message)
        End Try
    End Sub

    Private Async Sub ExcutePostDownloadActions()

        Dim _wait As Boolean = True

        Dim _m_container_sessions As New List(Of ContainerSession)

        _m_container_sessions = ContainerSessions.ToList

        Await Task.Run(Sub()

                           While _wait = True

                               For Each _session In _m_container_sessions

                                   If Not _session.SessionState = ContainerSessionState.DownloadRunning And _session.UnRarChains.Where(Function(mychain) mychain.UnRARRunning = True).Count = 0 Then
                                       _wait = False
                                   Else
                                       _wait = True
                                   End If

                               Next

                           End While

                       End Sub)


        If CheckedPostDownloadShutdownComputer = True Then

            Dim _shutdown_cmd As String = "shutdown -s -t 30"

            Process.Start("cmd", String.Format("/c {0}", _shutdown_cmd))

            DispatchService.DispatchService.Invoke(Sub()
                                                       Application.Current.Shutdown()
                                                   End Sub)

        Else
            If CheckedPostDownloadExitApp = True Then

                DispatchService.DispatchService.Invoke(Sub()
                                                           Application.Current.Shutdown()
                                                       End Sub)

            End If
        End If


    End Sub


    Private Sub ServerFullEvent(_item As DownloadItem)

        Task.Run(Sub()

                     Dim _log As Logger = LogManager.GetLogger("ItemDownloadCompleteEvent")

                     SyncLock ContainerSessions.First(Function(mysession) mysession.ID.Equals(_item.ParentContainerID)).SynLock

                         ContainerSessions.Where(Function(mycontainer) mycontainer.ID.Equals(_item.ParentContainerID))(0).SingleSessionMode = True

                         For Each _item In ContainerSessions.Where(Function(mycontainer) mycontainer.ID.Equals(_item.ParentContainerID)).FirstOrDefault().DownloadItems
                             _item.SingleSessionMode = True
                         Next

                     End SyncLock

                 End Sub)

    End Sub

    Private Async Sub StopDownload()

        Await Task.Run(Sub()

                           For Each _session In ContainerSessions

                               If Not IsNothing(_session.WIG) Then
                                   _session.WIG.Cancel()
                               End If

                           Next

                           _eta_thread.Cancel()

                           Application.Current.Resources("DownloadStopped") = True

                       End Sub)


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
                CheckedPostDownloadExitApp = True
            Else
                CheckedPostDownloadExitApp = False
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

        Dim _mytask As New AppTask(My.Resources.Strings.RemoveAllCompletedDownloads_AppTask_Start_Message)
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
            _mytask.SetTaskStatus(TaskStatus.RanToCompletion, My.Resources.Strings.RemoveAllCompletedDownloads_AppTask_Completed_Message)
        Else
            _mytask.SetTaskStatus(TaskStatus.Faulted, My.Resources.Strings.RemoveAllCompletedDownloads_AppTask_Faulted_Message)
        End If



    End Sub

    Public ReadOnly Property RemoveAllDownloadsCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf RemoveAllDownloads)
        End Get
    End Property

    Private Sub RemoveAllDownloads()

        Dim _mytask As New AppTask(My.Resources.Strings.RemoveAllCompletedDownloads_AppTask_Start_Message)

        AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent

        ActiveTasks.Add(_mytask)

        If CBool(Application.Current.Resources("DownloadStopped")) = False Then
            _mytask.SetTaskStatus(TaskStatus.Canceled, My.Resources.Strings.RemoveAllCompletedDownloads_AppTask_Faulted_Message)
        Else

            DownloadItems.Clear()
            ContainerSessions.Clear()

            _mytask.SetTaskStatus(TaskStatus.RanToCompletion, My.Resources.Strings.RemoveAllCompletedDownloads_AppTask_Completed_Message)

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
        Application.Current.MainWindow.Close()
    End Sub

    Public ReadOnly Property OpenSFDLCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf OpenSFDL)
        End Get
    End Property

    Private Sub OpenSFDL()

        Dim _ofd As New OpenFileDialog()

        With _ofd

            .Multiselect = True
            .Title = My.Resources.Strings.OpenFileDialog_Title
            .Filter = "SFDL Files (*.sfdl)|*.sfdl"

        End With

        If CType(_ofd.ShowDialog, Global.System.Windows.Forms.DialogResult) = DialogResult.Cancel Then Return

        For Each _file In _ofd.FileNames
            OpenSFDLFile(_file)
        Next

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

    Public ReadOnly Property MarkAllItemsCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf MarkAllItems)
        End Get
    End Property

    Private Sub MarkAllItems()
        DownloadItems.Select(Function(myitem)
                                 If myitem.isSelected = False Then
                                     myitem.isSelected = True
                                 End If
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
                                 If myitem.isSelected = True Then
                                     myitem.isSelected = False
                                 End If
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

            For Each _dlitem In DownloadItems.Where(Function(myitem) myitem.GroupDescriptionIdentifier.Equals(_item))
                _dlitem.IsExpanded = True
            Next

        Next

    End Sub

    Public ReadOnly Property CollapseAllPackagesCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf CollapseAllPackages)
        End Get
    End Property

    Private Sub CollapseAllPackages()

        For Each _item In DownloadItems.Select(Function(myitem) myitem.GroupDescriptionIdentifier).Distinct
            For Each _dlitem In DownloadItems.Where(Function(myitem) myitem.GroupDescriptionIdentifier.Equals(_item))
                _dlitem.IsExpanded = False
            Next
        Next

    End Sub

    Public ReadOnly Property ShowHelpCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf ShowHelp)
        End Get
    End Property

    Private Async Sub ShowHelp()

        Await DialogCoordinator.Instance.ShowMessageAsync(Me, "SFDL.NET 3", String.Format("Version: {0}", My.Application.Info.Version.ToString) & vbNewLine & vbNewLine & "Thanks to who supported and helped me with this Project" & vbNewLine & "Special thanks goes to tox2 for his great help")

    End Sub


    Public ReadOnly Property InstantVideoCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf InstantVideo)
        End Get
    End Property

    Private Async Sub InstantVideo()

        If CheckIfVLCInstalled() = False Then
            Await DialogCoordinator.Instance.ShowMessageAsync(Me, "InstantVideo", My.Resources.Strings.InstantVideo_Message_VLCNotInstalled_Message)
        Else

            InstantVideoOpen = True

            Dim _mystream As InstantVideoStream = Nothing
            Dim _error As Boolean = False

            Try

                _mystream = ContainerSessions.SelectMany(Of InstantVideoStream)(Function(mysession) mysession.InstantVideoStreams).Where(Function(insteam) insteam.IsSelected = True).FirstOrDefault

                If Not IsNothing(_mystream) Then

                    Dim _vlc_args As String = String.Empty

                    Dim _app_task As New AppTask(String.Format("Starte InstantVideo für Archiv {0}", _mystream.File))

                    _vlc_args = String.Format("{0} --no-qt-error-dialogs", Chr(34) & _mystream.File & Chr(34))

                    System.Diagnostics.Process.Start(Chr(34) & GetVLCExecutable() & Chr(34), _vlc_args)

                End If

            Catch ex As Exception
                _error = True
            End Try

            If _error = True Then
                Await DialogCoordinator.Instance.ShowMessageAsync(Me, "InstantVideo Fehler", "Das ausgewählte InstatVideo konnte nicht gestartet werden!")
            End If


        End If

    End Sub

#End Region

#Region "ListView ContextMenu Commands And Properites"

    Public ReadOnly Property MarkAllItemsInPackageCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf MarkAllItemsInPackage)
        End Get
    End Property

    Private Sub MarkAllItemsInPackage(ByVal parameter As Object)

        If Not IsNothing(parameter) Then

            Dim _item As DownloadItem = TryCast(parameter, DownloadItem)

            For Each _item In DownloadItems.Where(Function(myitem) myitem.PackageName.Equals(_item.PackageName) AndAlso myitem.isSelected = False)

                _item.isSelected = True

            Next

        End If

    End Sub

    Public ReadOnly Property UnMarkAllItemsInPackageCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf UnMarkAllItemsInPackage)
        End Get
    End Property

    Private Sub UnMarkAllItemsInPackage(ByVal parameter As Object)

        If Not IsNothing(parameter) Then

            Dim _item As DownloadItem = TryCast(parameter, DownloadItem)

            For Each _item In DownloadItems.Where(Function(myitem) myitem.PackageName.Equals(_item.PackageName) AndAlso myitem.isSelected = True)

                _item.isSelected = False

            Next

        End If

    End Sub


    Public ReadOnly Property OpenParentFolderCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf OpenParentFolder)
        End Get
    End Property

    Private Sub OpenParentFolder(ByVal parameter As Object)

        Dim _log As Logger = LogManager.GetLogger("OpenParentFolder")

        Try

            If Not IsNothing(parameter) Then

                Dim _item As DownloadItem = TryCast(parameter, DownloadItem)
                Dim _folder_path As String

                _folder_path = Path.GetDirectoryName(_item.LocalFile)

                Debug.WriteLine(_folder_path)

                If Not String.IsNullOrWhiteSpace(_folder_path) And Directory.Exists(_folder_path) Then
                    Process.Start("explorer.exe", String.Format("{0}{1}{2}", Chr(34), _folder_path, Chr(34)))
                End If

            End If

        Catch ex As Exception
            _log.Error(ex, ex.Message)
        End Try

    End Sub


    Public ReadOnly Property CloseSFDLContainerCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf CloseSFDLContainer)
        End Get
    End Property

    Private Sub CloseSFDLContainer(ByVal parameter As Object)

        Dim _mytask As New AppTask(My.Resources.Strings.CloseSFDLContainer_AppTask_Start_Message)

        If Not IsNothing(parameter) Then

            Dim _container_session As ContainerSession = Nothing
            Dim _tmp_list As New List(Of DownloadItem)

            AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent
            ActiveTasks.Add(_mytask)

            If parameter.GetType Is GetType(String) Then

                If Not String.IsNullOrWhiteSpace(CType(parameter, String)) Then

                    Dim _container_sessionid As Guid = Guid.Parse(CType(parameter, String))

                    _container_session = ContainerSessions.Where(Function(mysession) mysession.ID.Equals(_container_sessionid)).FirstOrDefault

                End If

            End If

            If parameter.GetType Is GetType(DownloadItem) Then

                Dim _dlitem As DownloadItem = TryCast(parameter, DownloadItem)

                _container_session = ContainerSessions.Where(Function(mysession) mysession.ID.Equals(_dlitem.ParentContainerID)).FirstOrDefault

            End If


            If _container_session.SessionState = ContainerSessionState.DownloadRunning Or _container_session.UnRarChains.Where(Function(mychain) mychain.UnRARRunning = True).Count >= 1 Then
                _mytask.SetTaskStatus(TaskStatus.Faulted, My.Resources.Strings.CloseSFDLContainer_AppTask_Faulted_Message)
            Else

                _tmp_list = DownloadItems.Where(Function(myitem) _container_session.ID.Equals(myitem.ParentContainerID)).ToList

                For Each _item In _tmp_list
                    DownloadItems.Remove(_item)
                Next

                _mytask.SetTaskStatus(TaskStatus.RanToCompletion, String.Format(My.Resources.Strings.CloseSFDLContainer_AppTask_Completed_Message, Path.GetFileName(_container_session.ContainerFileName)))

                ContainerSessions.Remove(_container_session)

            End If

        End If


    End Sub

#End Region

#Region "Allgemeine Properties"

    Private _max_download_speed As String
    Public Property MaxDownloadSpeed As String

        Set(value As String)

            If Equals(value, _max_download_speed) Then
                Return
            End If

            If IsNumeric(value) Or String.IsNullOrEmpty(value) Then
                _max_download_speed = value
            Else
                Return
            End If

            RaisePropertyChanged("MaxDownloadSpeed")

        End Set
        Get
            Return _max_download_speed
        End Get
    End Property

    Public Property WindowInstance As Window

    Private _window_state As WindowState = WindowState.Normal
    Public Property WindowState As WindowState
        Set(value As WindowState)
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


#End Region

#Region "Tasks"

    Public ReadOnly Property CopyDoneTaskTextCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf CopyDoneTaskText)
        End Get
    End Property

    Private Sub CopyDoneTaskText(ByVal parameter As Object)

        If Not IsNothing(parameter) Then

            Dim _item As AppTask = TryCast(parameter, AppTask)

            If Not IsNothing(_item) Then

                My.Computer.Clipboard.Clear()
                My.Computer.Clipboard.SetText(_item.TaskDisplayText)

            End If

        End If

    End Sub

    Private _active_tasks As New ObservableCollection(Of AppTask)
    Public Property ActiveTasks As ObservableCollection(Of AppTask)
        Set(value As ObservableCollection(Of AppTask))
            _active_tasks = value
            RaisePropertyChanged("ActiveTasks")
        End Set
        Get
            Return _active_tasks
        End Get
    End Property

    Private _done_tasks As New ObservableCollection(Of AppTask)
    Public Property DoneTasks As ObservableCollection(Of AppTask)
        Set(value As ObservableCollection(Of AppTask))
            _done_tasks = value
            RaisePropertyChanged("DoneTasks")
        End Set
        Get
            Return _done_tasks
        End Get
    End Property

    Private Sub TaskDoneEvent(e As AppTask)

        Task.Run(Sub()

                     'Wait 5 Seconds
                     Thread.Sleep(5000)

                     ActiveTasks.Remove(e)
                     DoneTasks.Add(e)

                 End Sub)

    End Sub


#End Region

#Region "InstantVideo"


    Private _instant_video_shown As Boolean = False

    Public Property InstantVideoOpen As Boolean
        Set(value As Boolean)
            _instant_video_shown = value
            RaisePropertyChanged("InstantVideoOpen")
            RaisePropertyChanged("InstantVideoContainerSessions")
        End Set
        Get
            Return _instant_video_shown
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

#Region "Container Sessions"

    Private _container_sessions As New ObservableCollection(Of ContainerSession)

    Public Property ContainerSessions As ObservableCollection(Of ContainerSession)
        Set(value As ObservableCollection(Of ContainerSession))
            _container_sessions = value
            RaisePropertyChanged("ContainerSessions")
        End Set
        Get
            Return _container_sessions
        End Get
    End Property

    Private _download_items As New ObservableCollection(Of DownloadItem)

    Public Property DownloadItems As ObservableCollection(Of DownloadItem)
        Set(value As ObservableCollection(Of DownloadItem))
            _download_items = value
            RaisePropertyChanged("DownloadItems")
        End Set
        Get
            Return _download_items
        End Get
    End Property

#End Region

#Region "DragnDrop"

    Public ReadOnly Property PreviewDropCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf HandlePreviewDrop)
        End Get
    End Property


    Private _PreviewDropCommand As ICommand
    Private Sub HandlePreviewDrop(inObject As Object)

        Dim _log As Logger = LogManager.GetLogger("HandlePreviewDrop")

        Dim ido As System.Windows.IDataObject = TryCast(inObject, System.Windows.IDataObject)

        If ido Is Nothing Then
            Return
        End If

        Dim fileDrop = ido.GetData(System.Windows.DataFormats.FileDrop, True)
        Dim filesOrDirectories = TryCast(fileDrop, [String]())

        If filesOrDirectories IsNot Nothing AndAlso filesOrDirectories.Length > 0 Then

            For Each fullPath As String In filesOrDirectories
                If Directory.Exists(fullPath) Then
                    _log.Debug("{0} is a directory", fullPath)

                ElseIf File.Exists(fullPath) Then
                    _log.Debug("{0} is a file", fullPath)

                    If Path.GetExtension(fullPath).ToLower = ".sfdl" Then
                        OpenSFDLFile(fullPath)
                    End If

                Else
                    _log.Debug("{0} is not a file and not a directory", fullPath)
                End If
            Next
        End If


    End Sub

#End Region

#Region "NewUpdate"


    Private _newupdate_available_visibility As NotifyTaskCompletion(Of Visibility)

    Public Property NewUpdateAvailableVisibility As NotifyTaskCompletion(Of Visibility)
        Set(value As NotifyTaskCompletion(Of Visibility))
            _newupdate_available_visibility = value
            RaisePropertyChanged("NewUpdateAvailableVisibility")
        End Set
        Get
            Return _newupdate_available_visibility
        End Get
    End Property

    Public ReadOnly Property OpenNewUpdateWebsiteCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf OpenNewUpdateWebsite)
        End Get
    End Property

    Sub OpenNewUpdateWebsite()

        Try
            Process.Start("https://github.com/n0ix/SFDL.NET/releases")
        Catch ex As Exception
            Debug.WriteLine("Failed to open Browser!")
        End Try

    End Sub

#End Region

#Region "ContainerInfo Commands and Properties"

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

    Public ReadOnly Property ShowContainerInfoCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf ShowContainerInfo)
        End Get
    End Property

    Private Sub ShowContainerInfo()
        ContainerInfoOpen = True
    End Sub

    Private _containerInfo_selectedItem As ContainerSession

    Public Property ContainerInfoSelectedItem As ContainerSession

        Set(value As ContainerSession)

            Dim _change As Boolean = False

            If IsNothing(_containerInfo_selectedItem) Then
                _containerInfo_selectedItem = value
                _change = True
            Else
                If Not IsNothing(value) AndAlso Not _containerInfo_selectedItem.ID.Equals(value.ID) Then
                    _containerInfo_selectedItem = value
                    _change = True
                Else
                    _containerInfo_selectedItem = value
                    ContainerInfoServerWhois = Nothing
                    ContainerInfoTotalSize = Nothing
                End If
            End If



            RaisePropertyChanged("ContainerInfoSelectedItem")

            If _change = True Then
                ContainerInfoServerWhois = New NotifyTaskCompletion(Of WhoIsResult)(WhoisHelper.Resolve(_containerInfo_selectedItem.ContainerFile.Connection.Host))
                ContainerInfoTotalSize = New NotifyTaskCompletion(Of Double)(SFDLFileHelper.GetContainerTotalSize(_containerInfo_selectedItem))
            End If

        End Set
        Get
            Return _containerInfo_selectedItem
        End Get
    End Property

    Private _containerinfo_serverwhois As NotifyTaskCompletion(Of WhoIsResult)

    Public Property ContainerInfoServerWhois As NotifyTaskCompletion(Of WhoIsResult)
        Set(value As NotifyTaskCompletion(Of WhoIsResult))
            _containerinfo_serverwhois = value
            RaisePropertyChanged("ContainerInfoServerWhois")
        End Set
        Get
            Return _containerinfo_serverwhois
        End Get
    End Property

    Private _containerinfo_totalsize As NotifyTaskCompletion(Of Double)

    Public Property ContainerInfoTotalSize As NotifyTaskCompletion(Of Double)
        Set(value As NotifyTaskCompletion(Of Double))
            _containerinfo_totalsize = value
            RaisePropertyChanged("ContainerInfoTotalSize")
        End Set
        Get
            Return _containerinfo_totalsize
        End Get
    End Property

#End Region



End Class
