Public Class Settings

    Public Property DeleteSFDLAfterOpen As Boolean = False
    Public Property Language As String = "de-DE"
    Public Property DownloadDirectory As String = String.Empty
    Public Property ExistingFileHandling As ExistingFileHandling = ExistingFileHandling.ResumeFile
    Public Property PreventStandby As Boolean = True
    Public Property CreateDownloadDirUseDescription As Boolean
    Public Property CreateDownloadDirUseFilename As Boolean = True
    Public Property CreateDownloadDir As Boolean = True
    Public Property CreatePackageSubfolder As Boolean = False
    Public Property Send2Tray As Boolean = False
    Public Property ClicknLoad As Boolean = True
    Public Property MaxDownloadThreads As Integer = 3
    Public Property MaxRetry As Integer = 3
    Public Property RetryWaitTime As Integer = 3
    Public Property MarkAllContainerFiles As Boolean = False
    Public Property SearchUpdates As Boolean = True
    Public Property InstantVideo As Boolean = False
    Public Property UnRARSettings As New UnRARSettings
    Public Property SpeedReportSettings As New SpeedreportSettings
    Public Property RemoteControlSettings As New RemoteControlSettings

End Class

Public Enum ExistingFileHandling
    ResumeFile
    OverwriteFile
End Enum
