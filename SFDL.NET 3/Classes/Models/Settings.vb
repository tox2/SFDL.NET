Public Class Settings

    Public Property DeleteSFDLAfterOpen As Boolean = False
    Public Property Language As String = "de-DE"
    Public Property DownloadDirectory As String = String.Empty
    Public Property ExistingFileHandling As ExistingFileHandling = ExistingFileHandling.ResumeFile
    Public Property PreventStandby As Boolean = True
    Public Property CreatePackageSubfolder As Boolean = False
    Public Property Send2Tray As Boolean = False
    Public Property ClicknLoad As Boolean = True
    Public Property MaxDownloadThreads As Integer = 3
    Public Property MaxRetry As Integer = 3
    Public Property RetryWaitTime As Integer = 3
    Public Property NotMarkAllContainerFiles As Boolean = False
    Public Property SearchUpdates As Boolean = True
    Public Property InstantVideo As Boolean = False
    Public Property UnRARSettings As New UnRARSettings
    Public Property SpeedReportSettings As New SpeedreportSettings
    Public Property RemoteControlSettings As New RemoteControlSettings

    Public Shared Function InitNewSettings() As Settings

        Dim _rt As New Settings

        With _rt

            .ClicknLoad = True
            .CreatePackageSubfolder = False
            .DeleteSFDLAfterOpen = False
            .DownloadDirectory = IO.Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "Downloads")
            .ExistingFileHandling = ExistingFileHandling.ResumeFile
            .InstantVideo = False
            .Language = "de"
            .MaxDownloadThreads = 3
            .MaxRetry = 3
            .PreventStandby = True
            .Send2Tray = False
            .UnRARSettings = New UnRARSettings
            .SpeedReportSettings = New SpeedreportSettings

        End With


        With _rt.SpeedReportSettings

            Dim _template As New Text.StringBuilder

            _template.AppendLine("SFDL: %%SFDL_DESC%%")
            _template.AppendLine("Upper: %%SFDL_UPPER%%")
            _template.AppendLine("")
            _template.AppendLine("%%SFDL_SIZE%% in %%DLTIME%% heruntergeladen @ %%SPEED%% (Im Durchschnitt)")
            _template.AppendLine("")
            _template.AppendLine("Kommentar: %%COMMENT%%")


            .SpeedreportTemplate = _template.ToString

        End With

        Return _rt

    End Function

End Class


Public Enum ExistingFileHandling
    ResumeFile
    OverwriteFile
End Enum
