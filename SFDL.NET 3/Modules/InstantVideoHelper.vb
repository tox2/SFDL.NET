Imports Microsoft.Win32
Imports NLog

Module InstantVideoHelper

    Friend Function IsReadyForInstantVideo(ByVal _chain As UnRARChain) As Boolean

        Dim _rt As Boolean = True
        Dim _log As Logger = LogManager.GetLogger("IsReadyForInstantVideo")

        Try

            Select Case _chain.MasterUnRarChainFile.DownloadStatus

                Case DownloadItem.Status.Completed
                        'ok

                Case DownloadItem.Status.Completed_HashInvalid
                        'ok

                Case DownloadItem.Status.Completed_HashValid
                        'ok

                Case DownloadItem.Status.AlreadyDownloaded
                    'ok

                Case Else

                    _rt = False


            End Select

            For Each _chainmember In _chain.ChainMemberFiles.Where(Function(_my_item) _my_item.RequiredForInstantVideo = True)

                _log.Debug("{0} is needed for InstantVideo!", _chainmember.FileName)

                Select Case _chainmember.DownloadStatus

                    Case DownloadItem.Status.Completed
                        'ok

                    Case DownloadItem.Status.Completed_HashInvalid
                        'ok

                    Case DownloadItem.Status.Completed_HashValid
                        'ok

                    Case DownloadItem.Status.AlreadyDownloaded
                        'ok

                    Case Else

                        _rt = False

                End Select


            Next

            _chain.ReadyForInstantVideo = _rt

            _log.Debug("{0} UnRArChain Reday For InstantVideo: {1}", _chain.MasterUnRarChainFile.FileName, _chain.ReadyForInstantVideo)

        Catch ex As Exception
            _log.Error(ex, ex.Message)
            _rt = False
        End Try

        Return _rt

    End Function
    Private Function isx64() As Boolean

        If IntPtr.Size = 8 Then
            Return True
        Else
            Return False
        End If

    End Function

    Friend Function CheckIfVLCInstalled() As Boolean

        Dim _regkey As RegistryKey
        Dim _rt As Boolean = False

        Try

            If isx64() Then
                _regkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\VideoLAN\VLC", False)
            Else
                _regkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\VideoLAN\VLC", False)
            End If

            _rt = True

        Catch ex As Exception
            _rt = False
        End Try

        Return _rt


    End Function

    Friend Function GetVLCExecutable() As String

        Dim _regkey As RegistryKey
        Dim _rt As String = String.Empty

        Try

            If isx64() Then
                _regkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\VideoLAN\VLC", False)
            Else
                _regkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\VideoLAN\VLC", False)
            End If

            _rt = _regkey.GetValue(Nothing).ToString

        Catch ex As Exception
        End Try

        Return _rt

    End Function

End Module
