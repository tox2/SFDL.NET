Public Class SettingsViewModel
    Inherits ViewModelBase

    Dim _settings As Settings

    Public Sub New()
        _settings = Application.Current.Resources("Settings")
    End Sub

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

    Private Sub SaveSettings()

        Application.Current.Resources("Settings") = _settings

        XMLHelper.XMLSerialize(_settings, IO.Path.Combine(Environment.GetEnvironmentVariable("appdata"), "SFDL.NET 3\settings.xml"))

    End Sub

    Private Sub SelectDownloadFolder()

        Dim _sdf_dialog As New Forms.FolderBrowserDialog

        With _sdf_dialog
            .ShowNewFolderButton = True
        End With

        _sdf_dialog.ShowDialog()

    End Sub

    Public ReadOnly Property SaveSettingsCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf SaveSettings)
        End Get
    End Property

    Public ReadOnly Property SelectDownloadFolderCommand() As ICommand
        Get
            Return New DelegateCommand(AddressOf SelectDownloadFolder)
        End Get
    End Property

End Class
