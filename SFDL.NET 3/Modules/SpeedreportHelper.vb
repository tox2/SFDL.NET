Module SpeedreportHelper

    Private Function CalculateSizeAsMB(ByVal _item_list As List(Of DownloadItem)) As Double

        Dim _full_session_size As Double

        _full_session_size = _item_list.Aggregate(_full_session_size, Function(current, _file) current + _file.SizeDownloaded)

        _full_session_size = (_full_session_size / 1024) / 1024

        Return _full_session_size

    End Function

    Private Function CalculateSpeed(ByVal _starttime As Date, ByVal _stoptime As Date, ByVal _item_list As List(Of DownloadItem)) As Double

        Dim _full_session_size As Double
        Dim _time_elapsed As Double
        Dim _rt As Double

        _full_session_size = _item_list.Aggregate(_full_session_size, Function(current, _file) current + _file.SizeDownloaded)

        _full_session_size = _full_session_size / 1024

        _time_elapsed = DateDiff(DateInterval.Second, _starttime, _stoptime)

        Debug.WriteLine(String.Format("{0} KB in {1} Sekunden heruntergeladen", _full_session_size, _time_elapsed))

        _rt = _full_session_size / _time_elapsed

        Return _rt

    End Function

    Function GenerateSpeedreport(ByVal session As ContainerSession, ByVal _speedreportSettings As SpeedreportSettings) As String

        Dim _rt_speedreport As String = String.Empty
        Dim _speed As Double = 0
        Dim _size As Double = 0

        CalculateSizeAsMB(session.DownloadItems.Where(Function(_item) Not _item.SizeDownloaded = 0))
        CalculateSpeed(session.DownloadStartedTime, session.DownloadStoppedTime, session.DownloadItems.Where(Function(_item) Not _item.SizeDownloaded = 0))

        _rt_speedreport = _speedreportSettings.SpeedreportTemplate

        _rt_speedreport = _rt_speedreport.Replace("%%USER%%", _speedreportSettings.SpeedreportUsername)
        _rt_speedreport = _rt_speedreport.Replace("%%CONNECTION%%", _speedreportSettings.SpeedreportConnection)
        _rt_speedreport = _rt_speedreport.Replace("%%COMMENT%%", _speedreportSettings.SpeedreportComment)
        _rt_speedreport = _rt_speedreport.Replace("%%SPEED%%", Math.Round(_speed, 2) & " KB/s")
        _rt_speedreport = _rt_speedreport.Replace("%%SFDL_DESC%%", session.ContainerFile.Description)
        _rt_speedreport = _rt_speedreport.Replace("%%SFDL_UPPER%%", session.ContainerFile.Uploader)
        _rt_speedreport = _rt_speedreport.Replace("%%TIME%%", TimeSpan.FromSeconds(DateDiff(DateInterval.Second, session.DownloadStartedTime, session.DownloadStoppedTime)).ToString("HH:mm:ss"))
        _rt_speedreport = _rt_speedreport.Replace("%%SFDL_SIZE%%", Math.Round(_size, 2) & " MB")

        Return _rt_speedreport

    End Function

End Module
