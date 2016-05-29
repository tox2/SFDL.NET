
Class DownloadHelper

    Private _log As NLog.Logger = NLog.LogManager.GetLogger("DownloadHelper")
    Private _ftp_client_list As New Dictionary(Of Guid, ArxOne.Ftp.FtpClient)
    Private _obj_ftp_client_list_lock As New Object

    Sub DownloadContainerItems(maxConcurrency As Integer, items As List(Of DownloadItem), ByVal _connection_info As SFDL.Container.Connection)

        Dim _tasks = New List(Of System.Threading.Tasks.Task)

        For Each _item As DownloadItem In items

            Dim _dl_task As System.Threading.Tasks.Task
            Dim _ftp_client As ArxOne.Ftp.FtpClient

            SyncLock _obj_ftp_client_list_lock

                'Check if any FTP Client Exits for this Parent Container Session
                If Not _ftp_client_list.ContainsKey(_item.ParentContainerID) Then
                    SetupFTPClient(_ftp_client, _connection_info)
                    _ftp_client_list.Add(_item.ParentContainerID, _ftp_client)
                Else
                    _ftp_client = _ftp_client_list(_item.ParentContainerID)
                End If

            End SyncLock

            _dl_task = System.Threading.Tasks.Task.Run(Sub()
                                                           'Ftp Download
                                                           DownloadItem(_item, _ftp_client.Session)
                                                       End Sub)
            _tasks.Add(_dl_task)

        Next

        System.Threading.Tasks.Task.WhenAll(_tasks)

    End Sub

    Private Sub DownloadItem(ByVal _item As DownloadItem, ByVal _ftp_session As ArxOne.Ftp.FtpSession)




    End Sub

End Class
