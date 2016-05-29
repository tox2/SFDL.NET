
Module DownloadHelper

    Sub DownloadContainerItems(maxConcurrency As Integer, items As List(Of DownloadItem))

        Dim _tasks = New List(Of System.Threading.Tasks.Task)

        For Each _item As DownloadItem In items

            Dim _dl_task As System.Threading.Tasks.Task

            _dl_task = System.Threading.Tasks.Task.Run(Sub()
                                                           'Ftp Download
                                                           DownloadItem(_item)
                                                       End Sub)
            _tasks.Add(_dl_task)

        Next

        System.Threading.Tasks.Task.WhenAll(_tasks)

    End Sub

    Private Sub DownloadItem(ByVal _item As DownloadItem)

        _item.DownloadSpeed = "lol"
        _item.DownloadStatus = NET3.DownloadItem.Status.Completed



    End Sub

End Module
