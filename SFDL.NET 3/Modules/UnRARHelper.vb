Module UnRARHelper

    Dim _log As NLog.Logger = NLog.LogManager.GetLogger("UnRARHelper")

    Function isUnRarChainComplete(ByVal _chain As UnRARChain) As Boolean

        Dim _rt As Boolean = True

        Try


            Select Case _chain.MasterUnRarChainFile.DownloadStatus

                Case DownloadItem.Status.Completed
                    'ok

                Case DownloadItem.Status.Completed_HashInvalid
                    'ok

                Case DownloadItem.Status.Completed_HashValid
                    'ok

                Case Else

                    _rt = False

            End Select


            For Each _chainmember As DownloadItem In _chain.ChainMemberFiles

                Select Case _chainmember.DownloadStatus

                    Case DownloadItem.Status.Completed
                    'ok

                    Case DownloadItem.Status.Completed_HashInvalid
                    'ok

                    Case DownloadItem.Status.Completed_HashValid
                        'ok
                    Case Else
                        _rt = False

                End Select

            Next


        Catch ex As Exception
            _log.Error(ex, ex.Message)
            _rt = False
        End Try

        Return _rt


    End Function

End Module
