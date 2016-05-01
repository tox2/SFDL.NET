﻿Imports MahApps.Metro.Controls.Dialogs
Public Class SettingsViewModel
    Inherits ViewModelBase

    Private _settings As New Settings
    Private _selected_unrar_password As String = String.Empty

    Public Sub New()
        _settings = Application.Current.Resources("Settings")
    End Sub

#Region "General/Basic Settings Properties"

    Public Property PreventStandby As Boolean
        Set(value As Boolean)
            _settings.PreventStandby = value
            RaisePropertyChanged("PreventStandby")
        End Set
        Get
            Return _settings.PreventStandby
        End Get
    End Property

    Public Property Send2Tray As Boolean
        Set(value As Boolean)
            _settings.Send2Tray = value
            RaisePropertyChanged("Send2Tray")
        End Set
        Get
            Return _settings.Send2Tray
        End Get
    End Property
    Public Property ClicknLoad As Boolean
        Set(value As Boolean)
            _settings.ClicknLoad = value
            RaisePropertyChanged("ClicknLoad")
        End Set
        Get
            Return _settings.ClicknLoad
        End Get
    End Property
    Public Property SearchUpdates As Boolean
        Set(value As Boolean)
            _settings.SearchUpdates = value
            RaisePropertyChanged("SearchUpdates")
        End Set
        Get
            Return _settings.SearchUpdates
        End Get
    End Property
    Public Property Language As String
        Set(value As String)
            _settings.Language = value
            RaisePropertyChanged("Language")
        End Set
        Get
            Return _settings.Language
        End Get
    End Property
    Public Property InstantVideo As Boolean
        Set(value As Boolean)
            _settings.InstantVideo = value
            RaisePropertyChanged("InstantVideo")
        End Set
        Get
            Return _settings.InstantVideo
        End Get
    End Property
    Public Property DownloadDirectory As String
        Set(value As String)
            _settings.DownloadDirectory = value
            RaisePropertyChanged("DownloadDirectory")
        End Set
        Get
            Return _settings.DownloadDirectory
        End Get
    End Property
    Public Property CreateDownloadDir As Boolean
        Set(value As Boolean)
            _settings.CreateDownloadDir = value
            _settings.CreatePackageSubfolder = Not value
            RaisePropertyChanged("CreateDownloadDir")
        End Set
        Get
            Return _settings.CreateDownloadDir
        End Get
    End Property
    Public Property CreateDownloadDirUseFilename As Boolean
        Set(value As Boolean)
            _settings.CreateDownloadDirUseFilename = value
            RaisePropertyChanged("CreateDownloadDirUseFilename")
        End Set
        Get
            Return _settings.CreateDownloadDirUseFilename
        End Get
    End Property
    Public Property CreateDownloadDirUseDescription As Boolean
        Set(value As Boolean)
            _settings.CreateDownloadDirUseDescription = value
            RaisePropertyChanged("CreateDownloadDirUseDescription")
        End Set
        Get
            Return _settings.CreateDownloadDirUseDescription
        End Get
    End Property
    Public Property ExistingFileHandling As ExistingFileHandling
        Set(value As ExistingFileHandling)
            _settings.ExistingFileHandling = value
            RaisePropertyChanged("ExistingFileHandling")
        End Set
        Get
            Return _settings.ExistingFileHandling
        End Get
    End Property
    Public Property MaxDownloadThreads As Integer
        Set(value As Integer)
            _settings.MaxDownloadThreads = value
            RaisePropertyChanged("MaxDownloadThreads")
        End Set
        Get
            Return _settings.MaxDownloadThreads
        End Get
    End Property
    Public Property MaxRetry As Integer
        Set(value As Integer)
            _settings.MaxRetry = value
            RaisePropertyChanged("MaxRetry")
        End Set
        Get
            Return _settings.MaxRetry
        End Get
    End Property
    Public Property RetryWaitTime As Integer
        Set(value As Integer)
            _settings.RetryWaitTime = value
            RaisePropertyChanged("RetryWaitTime")
        End Set
        Get
            Return _settings.RetryWaitTime
        End Get
    End Property
    Public Property MarkAllContainerFiles As Boolean
        Set(value As Boolean)
            _settings.MarkAllContainerFiles = value
            RaisePropertyChanged("MarkAllContainerFiles")
        End Set
        Get
            Return _settings.MarkAllContainerFiles
        End Get
    End Property
    Public Property DeleteSFDLAfterOpen As Boolean
        Set(value As Boolean)
            _settings.DeleteSFDLAfterOpen = value
            RaisePropertyChanged("DeleteSFDLAfterOpen")
        End Set
        Get
            Return _settings.DeleteSFDLAfterOpen
        End Get
    End Property

#End Region

#Region "UnRAR Settings Properties"

    Public Property UnRARAfterDownload As Boolean
        Set(value As Boolean)
            _settings.UnRARSettings.UnRARAfterDownload = value
            RaisePropertyChanged("UnRARAfterDownload")
        End Set
        Get
            Return _settings.UnRARSettings.UnRARAfterDownload
        End Get
    End Property

    Public Property DeleteAfterUnRAR As Boolean
        Set(value As Boolean)
            _settings.UnRARSettings.DeleteAfterUnRAR = value
            RaisePropertyChanged("UnRARAfterDownload")
        End Set
        Get
            Return _settings.UnRARSettings.DeleteAfterUnRAR
        End Get
    End Property

    Public Property UseUnRARPasswordList As Boolean
        Set(value As Boolean)
            _settings.UnRARSettings.UseUnRARPasswordList = value
            RaisePropertyChanged("UseUnRARPasswordList")
        End Set
        Get
            Return _settings.UnRARSettings.UseUnRARPasswordList
        End Get
    End Property

    Public Property UnRARPasswordList As ObjectModel.ObservableCollection(Of String)
        Set(value As ObjectModel.ObservableCollection(Of String))
            _settings.UnRARSettings.UnRARPasswordList = value
            RaisePropertyChanged("UnRARPasswordList")
        End Set
        Get
            Return _settings.UnRARSettings.UnRARPasswordList
        End Get
    End Property

    Public Property SelectedUnRARPassword As String
        Set(value As String)
            _selected_unrar_password = value
        End Set
        Get
            Return _selected_unrar_password
        End Get
    End Property


#End Region

#Region "Speedreport Settings"

    Public Property SpeedreportVisibility As SpeedreportVisibility
        Set(value As SpeedreportVisibility)
            _settings.SpeedReportSettings.SpeedreportView = value
        End Set
        Get
            Return _settings.SpeedReportSettings.SpeedreportView
        End Get
    End Property

    Public Property SpeedreportUsername As String
        Set(value As String)
            _settings.SpeedReportSettings.SpeedreportUsername = value
            RaisePropertyChanged("SpeedreportUsername")
        End Set
        Get
            Return _settings.SpeedReportSettings.SpeedreportUsername
        End Get
    End Property

    Public Property SpeedreportConnection As String
        Set(value As String)
            _settings.SpeedReportSettings.SpeedreportConnection = value
            RaisePropertyChanged("SpeedreportConnection")
        End Set
        Get
            Return _settings.SpeedReportSettings.SpeedreportConnection
        End Get
    End Property

    Public Property SpeedreportComment As String
        Set(value As String)
            _settings.SpeedReportSettings.SpeedreportComment = value
            RaisePropertyChanged("SpeedreportComment")
        End Set
        Get
            Return _settings.SpeedReportSettings.SpeedreportComment
        End Get
    End Property
    Public Property SpeedreportTemplate As String
        Set(value As String)
            _settings.SpeedReportSettings.SpeedreportTemplate = value
            RaisePropertyChanged("SpeedreportTemplate")
        End Set
        Get
            Return _settings.SpeedReportSettings.SpeedreportTemplate
        End Get
    End Property


#End Region

#Region "Commands"

    Private Sub SaveSettings()

        Application.Current.Resources("Settings") = _settings

        XMLHelper.XMLSerialize(_settings, IO.Path.Combine(Environment.GetEnvironmentVariable("appdata"), "SFDL.NET 3\settings.xml"))

    End Sub

    Public ReadOnly Property SaveSettingsCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf SaveSettings)
        End Get
    End Property
    Private Sub SelectDownloadFolder()

        Dim _sdf_dialog As New Forms.FolderBrowserDialog

        With _sdf_dialog
            .ShowNewFolderButton = True
        End With

        _sdf_dialog.ShowDialog()

        Me.DownloadDirectory = _sdf_dialog.SelectedPath

    End Sub
    Public ReadOnly Property SelectDownloadFolderCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf SelectDownloadFolder)
        End Get
    End Property

    Private Async Sub AddUnRARPassword()

        Dim _new_password As String = String.Empty

        _new_password = Await DialogCoordinator.Instance.ShowInputAsync(Me, "Passwort hinzufügen", "Bitte gebe das Kennwort ein welche du hinzufügen möchtest")

        If Not _settings.UnRARSettings.UnRARPasswordList.Contains(_new_password) Then
            Me.UnRARPasswordList.Add(_new_password)
        End If

    End Sub

    Public ReadOnly Property AddUnRARPasswordCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf AddUnRARPassword)
        End Get
    End Property

    Private Async Sub RemoveUnRARPassword()

        If Not String.IsNullOrWhiteSpace(_selected_unrar_password) Then

            Dim _style As MessageDialogStyle = MessageDialogStyle.AffirmativeAndNegative

            If Await DialogCoordinator.Instance.ShowMessageAsync(Me, "Passwort entfernen", "Willst du das ausgewählte Passwort entfernen?", _style) = MessageDialogResult.Affirmative Then
                Me.UnRARPasswordList.Remove(_selected_unrar_password)
            End If

        End If

    End Sub

    Public ReadOnly Property RemoveUnRARPasswordCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf RemoveUnRARPassword)
        End Get
    End Property

    Private Sub InsertSpeedReportVariable(ByVal parameter As Object)

        Dim _param As String = CType(parameter, String)

        Select Case _param

            Case "username"

                Me.SpeedreportTemplate = Me.SpeedreportTemplate & Space(1) & "%%USERNAME%%" & Space(1)

            Case "connection"

                Me.SpeedreportTemplate = Me.SpeedreportTemplate & Space(1) & "%%CONNECTION%%" & Space(1)

            Case "comment"

                Me.SpeedreportTemplate = Me.SpeedreportTemplate & Space(1) & "%%COMMENT%%" & Space(1)

            Case "downloadspeed"

                Me.SpeedreportTemplate = Me.SpeedreportTemplate & Space(1) & "%%SPEED%%" & Space(1)

            Case "sfdl_description"

                Me.SpeedreportTemplate = Me.SpeedreportTemplate & Space(1) & "%%SFDL_DESC%%" & Space(1)

            Case "sfdl_upper"

                Me.SpeedreportTemplate = Me.SpeedreportTemplate & Space(1) & "%%SFDL_UPPER%%" & Space(1)

            Case "sfdl_downloadsize"

                Me.SpeedreportTemplate = Me.SpeedreportTemplate & Space(1) & "%%TIME%%" & Space(1)

        End Select

    End Sub


    Public ReadOnly Property InsertSpeedReportVariableCommand As ICommand
        Get
            Return New DelegateCommand(AddressOf InsertSpeedReportVariable)
        End Get
    End Property

#End Region

End Class