Imports System.Collections.Specialized
Imports SFDL.NET3

Public Class MainViewModel
    Inherits ViewModelBase

    Private _curr_selected_item As DownloadItem = Nothing
    Private _settings As New Settings
    Private _container_info_shown As Boolean = False

    Public Sub New()
        _settings = Application.Current.Resources("Settings")
        CreateView()
    End Sub

#Region "Private Subs"

    Private Sub CreateView()

        Dim view As CollectionView = DirectCast(CollectionViewSource.GetDefaultView(DownloadItems), CollectionView)

        Dim groupDescription As New PropertyGroupDescription("PackageName")

        view.GroupDescriptions.Add(groupDescription)

        'Dim groupDescription2 As New PropertyGroupDescription("ParentContainerID")

        'view.GroupDescriptions.Add(groupDescription2)

    End Sub

    Private Sub OpenSFDLFile(ByVal _sfdl_container_path As String)

        Dim _mytask As New Task(String.Format("SFDL Datei {0} wird geöffnet...", _sfdl_container_path))
        Dim _mycontainer As New Container.Container
        Dim _mycontainer_session As ContainerSession
        Dim _decrypt_password As String
        Dim _decrypt As New SFDL.Container.Decrypt


        AddHandler _mytask.TaskDone, AddressOf TaskDoneEvent

        ActiveTasks.Add(_mytask)

        System.Threading.Tasks.Task.Run(Sub()

                                            Try

                                                Dim _bulk_result As Boolean

                                                If GetContainerVersion(_sfdl_container_path) = 0 Or GetContainerVersion(_sfdl_container_path) > 10 Then
                                                    Throw New Exception("Diese SFDL Datei ist mit dieser Programmversion nicht kompatibel!")
                                                End If

                                                _mycontainer = XMLHelper.XMLDeSerialize(_mycontainer, _sfdl_container_path)

                                                If _mycontainer.Encrypted = True Then

                                                    Try
Decrypt:
                                                        _decrypt_password = MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.ShowInputAsync(Me, "SFDL entschlüsseln", "Bitte gib ein Passwort ein um den SFDL Container zu entschlüsseln").Result

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
                                                    _bulk_result = GetBulkFileList(_mycontainer_session)
                                                End If

                                                GenerateContainerSessionDownloadItems(_mycontainer_session, _settings.NotMarkAllContainerFiles)

                                                If _bulk_result = False And _mycontainer_session.DownloadItems.Count = 0 Then
                                                    Throw New Exception("Öffnen fehlgeschlagen - Bulk Package konnte nicht gelesen werden")
                                                End If

                                                'ToDo: Parse/Generate UnRAR Chains

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

                                        End Sub)



    End Sub

    Private Sub PreDownloadCheck()

        'ToDo: Check if Download is Running?

        'ToDo: Check if any Item is marked

        'ToDo: Check if Download Directory Exists


    End Sub

    Private Async Sub StartDownload()

        Try

            Dim _thread_count_pool As Integer = _settings.MaxDownloadThreads
            Dim _itemdownloadlist As New List(Of DownloadItem)
            Dim _tasklist As New List(Of System.Threading.Tasks.Task)
            Dim _log As NLog.Logger = NLog.LogManager.GetLogger("StartDownload")
            Dim _download_helper As New DownloadHelper

            'Check if rdy for Download
            PreDownloadCheck()

            While Not ContainerSessions.Where(Function(mysession) mysession.SessionState = ContainerSessionState.Queued Or mysession.SessionState = ContainerSessionState.DownloadRunning).Count = 0

                'Query Download Items
                For Each _session In ContainerSessions.Where(Function(mysession) mysession.SessionState = ContainerSessionState.Queued Or mysession.SessionState = ContainerSessionState.DownloadRunning)

                    Dim _thread_count As Integer = 0

                    If Not _session.ContainerFile.MaxDownloadThreads > _thread_count_pool Then
                        _thread_count = _session.ContainerFile.MaxDownloadThreads
                        _thread_count_pool -= _thread_count
                    Else
                        If Not _thread_count_pool = 0 Then
                            _thread_count = _thread_count_pool
                        Else
                            _log.Info("Alle verfügbaren Threads sind vergeben!")
                        End If
                    End If

                    If Not _thread_count = 0 Then

                        _itemdownloadlist.AddRange(DownloadItems.Where(Function(myitem) myitem.ParentContainerID.Equals(_session.ID) And myitem.DownloadStatus = DownloadItem.Status.Queued).Take(_thread_count))

                        _tasklist.Add(System.Threading.Tasks.Task.Run(Sub()
                                                                          _download_helper.DownloadContainerItems(_thread_count, _itemdownloadlist, _session.ContainerFile.Connection)
                                                                      End Sub))
                    End If

                Next


                'Warten bis dieser Download Chunk Fertig ist

                Await Threading.Tasks.Task.WhenAll(_tasklist)

                _thread_count_pool = _settings.MaxDownloadThreads 'Reset

                'ToDO: Prüfen welche Container Sessions nun ggf. fertig heruntergeladen sind.
                For Each _session In ContainerSessions.Where(Function(mysession) mysession.SessionState = ContainerSessionState.Queued Or mysession.SessionState = ContainerSessionState.DownloadRunning)

                    If DownloadItems.Where(Function(myitem) myitem.DownloadStatus = DownloadItem.Status.Queued).Count = 0 Then 'Alle Items sind heruntergeladen
                        _session.SessionState = ContainerSessionState.DownloadComplete
                        'ToDo: generate Speedreport
                        'ToDo: Unrar Items
                    End If

                Next

            End While

            Debug.WriteLine("Alle Downloads abgeschlossen!!")

        Catch ex As Exception

        End Try


    End Sub

#End Region

#Region "Button States"

    Private _button_downloadstart_enabled As Boolean = True

    Public Property ButtonDownloadStartEnabled As Boolean
        Set(value As Boolean)
            _button_downloadstart_enabled = value
            RaisePropertyChanged("ButtonDownloadStartEnabled")
        End Set
        Get
            Return _button_downloadstart_enabled
        End Get
    End Property

    Private _button_downloadstop_enabled As Boolean = False

    Public Property ButtonDownloadStopEnabled As Boolean
        Set(value As Boolean)
            _button_downloadstop_enabled = value
            RaisePropertyChanged("ButtonDownloadStopEnabled")
        End Set
        Get
            Return _button_downloadstop_enabled
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

    Public ReadOnly Property ShowContainerInfoCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf ShowContainerInfo)
        End Get
    End Property

    Private Sub ShowContainerInfo()
        Me.ContainerInfoOpen = True
    End Sub


#End Region

#Region "ListView ContextMenu Commands and Properites"

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

    Private Sub CloseSFDLContainer()

        If Not IsNothing(SelectedDownloadItem) Then

            Dim _container_sessionid As Guid

            _container_sessionid = SelectedDownloadItem.ParentContainerID

            Dim _tmp_list As New List(Of DownloadItem)

            _tmp_list = DownloadItems.Where(Function(myitem) SelectedDownloadItem.ParentContainerID.Equals(myitem.ParentContainerID)).ToList

            For Each _item In _tmp_list
                DownloadItems.Remove(_item)
            Next

            ContainerSessions.Remove(ContainerSessions.Where(Function(mysession) mysession.ID.Equals(_container_sessionid))(0))

        End If

    End Sub

#End Region

#Region "Tasks"

    Private _active_tasks As New ObjectModel.ObservableCollection(Of Task)
    Public Property ActiveTasks As ObjectModel.ObservableCollection(Of Task)
        Set(value As ObjectModel.ObservableCollection(Of Task))
            _active_tasks = value
            RaisePropertyChanged("ActiveTasks")
        End Set
        Get
            Return _active_tasks
        End Get
    End Property

    Private Sub TaskDoneEvent(e As Task)

        'Wait 5 Seconds
        System.Threading.Thread.Sleep(5000)
        DispatchService.DispatchService.Invoke(Sub()
                                                   ActiveTasks.Remove(e)
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

    Public Property ContainerInfoOpen As Boolean
        Set(value As Boolean)
            _container_info_shown = value
            RaisePropertyChanged("ContainerInfoOpen")
        End Set
        Get
            Return _container_info_shown
        End Get
    End Property


End Class
