Imports Microsoft.Win32
Imports NLog

Module InstantVideoHelper

    Friend Function IsReadyForInstantVideo(ByVal _chain As UnRARChain) As Boolean

        Dim _rt As Boolean = True
        Dim _log As Logger = LogManager.GetLogger("IsReadyForInstantVideo")

        Try


            If Not IO.File.Exists(_chain.MasterUnRarChainFile.LocalFile) Then
                _rt = False
            Else
                If Not New IO.FileInfo(_chain.MasterUnRarChainFile.LocalFile).Length.Equals(_chain.MasterUnRarChainFile.FileSize) Then
                    _rt = False
                End If
            End If

            For Each _chainmember In _chain.ChainMemberFiles.Where(Function(_my_item) _my_item.RequiredForInstantVideo = True)

                _log.Debug("{0} is needed for InstantVideo!", _chainmember.FileName)

                If Not IO.File.Exists(_chainmember.LocalFile) Then
                    _rt = False
                Else
                    If Not New IO.FileInfo(_chainmember.LocalFile).Length.Equals(_chainmember.FileSize) Then
                        _rt = False
                    End If
                End If

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
