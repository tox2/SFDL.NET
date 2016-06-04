
Public Class MainWindow
    Public Sub New()

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()
        Me.DataContext = New MainViewModel
        ' Fügen Sie Initialisierungen nach dem InitializeComponent()-Aufruf hinzu.
        LogHelper.GenerateLogConfig()
        Application.Current.Resources.Add("DownloadStopped", False)

    End Sub

    Private Sub ComB_Container_Info_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComB_Container_Info.SelectionChanged

        Dim _sel_object As ContainerSession
        Dim _country_code As String = String.Empty

        Try

            _sel_object = ComB_Container_Info.SelectedItem

            System.Threading.Tasks.Task.Run(Sub()

                                                _country_code = WhoisHelper.Resolve(_sel_object.ContainerFile.Connection.Host)

                                            End Sub).ContinueWith(Sub()

                                                                      DispatchService.DispatchService.Invoke(Sub()

                                                                                                                 txt_containerinfo_upper.Content = _sel_object.ContainerFile.Uploader

                                                                                                                 If Not String.IsNullOrWhiteSpace(_country_code) Then
                                                                                                                     txt_containerinfo_serverlocation.Content = _country_code
                                                                                                                     img_containerinfo_serverlocation.Source = New BitmapImage(New Uri("http://n1.dlcache.com/flags/" & _country_code.ToLower & ".gif"))
                                                                                                                 Else
                                                                                                                     txt_containerinfo_serverlocation.Content = "N/A"
                                                                                                                     img_containerinfo_serverlocation.Source = Nothing
                                                                                                                 End If

                                                                                                             End Sub)

                                                                  End Sub)


        Catch ex As Exception

        End Try


    End Sub
End Class
