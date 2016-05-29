
Module DownloadHelper

    Async Sub DownloadContainerItems(maxConcurrency As Integer, items As List(Of DownloadItem))

        Using sem = New System.Threading.SemaphoreSlim(maxConcurrency)

            Dim _tasks = New List(Of System.Threading.Tasks.Task)

            For Each _item As DownloadItem In items

                Await sem.WaitAsync()

                Dim _dl_task As System.Threading.Tasks.Task


                _dl_task = System.Threading.Tasks.Task.Run(Sub()

                                                               'Ftp Download
                                                               DownloadItem(_item)

                                                           End Sub).ContinueWith(Sub()
                                                                                     sem.Release()
                                                                                 End Sub)

                _tasks.Add(_dl_task)




            Next

            Await System.Threading.Tasks.Task.WhenAll(_tasks)

        End Using

    End Sub

    Private Sub DownloadItem(ByVal _item As DownloadItem)



        _item.DownloadSpeed = "lol"



    End Sub

End Module
