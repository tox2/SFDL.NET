
Imports System.ComponentModel
Imports MahApps.Metro.Controls.Dialogs

Public Class MainWindow

    Public Sub New()

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()
        Me.DataContext = New MainViewModel



    End Sub

    Private Async Sub MainWindow_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered

        For Each _arg In Environment.GetCommandLineArgs

            If Not String.IsNullOrWhiteSpace(_arg) And IO.Path.GetExtension(_arg).ToLower = ".sfdl" Then
                MainViewModel.ThisInstance.OpenSFDLFile(_arg)
            End If

        Next

        If IO.File.Exists(IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "unrar.exe")) = False Then
            Await ShowMessageAsync(My.Resources.Strings.VariousStrings_Warning, My.Resources.Strings.VariousStrings_UnRARExecutableMissingException)
        End If

        ComB_Container_Info.DataContext = MainViewModel.ThisInstance

        If My.Settings.UserWindowState = WindowState.Normal Then

            If Not My.Settings.UserWindowHeight = 0 Then
                Me.Height = My.Settings.UserWindowHeight
            End If

            If Not My.Settings.UserWindowWitdh = 0 Then
                Me.Width = My.Settings.UserWindowWitdh
            End If

            If Not My.Settings.UserWindowTop = 0 Then
                Me.Top = My.Settings.UserWindowTop
            End If

            If Not My.Settings.UserWindowLeft = 0 Then
                Me.Left = My.Settings.UserWindowLeft
            End If

        Else
            Me.WindowState = WindowState.Maximized
        End If

    End Sub

    Private Sub ComB_Container_Info_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComB_Container_Info.SelectionChanged

        Dim _sel_object As ContainerSession
        Dim _country_code As String = String.Empty

        Try

            _sel_object = ComB_Container_Info.SelectedItem

            If Not IsNothing(_sel_object) Then

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

            Else

                txt_containerinfo_serverlocation.Content = Nothing
                img_containerinfo_serverlocation.Source = Nothing
                txt_containerinfo_upper.Content = String.Empty

            End If


        Catch ex As Exception

        End Try


    End Sub



    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing

        If MainViewModel.ThisInstance.WindowState = WindowState.Normal Or MainViewModel.ThisInstance.WindowState = WindowState.Maximized Then

            My.Settings.UserWindowState = MainViewModel.ThisInstance.WindowState

            My.Settings.UserWindowHeight = Me.Height
            My.Settings.UserWindowWitdh = Me.Width

            My.Settings.UserWindowTop = Me.Top
            My.Settings.UserWindowLeft = Me.Left

            My.Settings.Save()

        End If

    End Sub

    Private Sub MenuItem_Click(sender As Object, e As RoutedEventArgs)

        MessageBox.Show(MainViewModel.ThisInstance.DownloadItems(0).isExpanded)

    End Sub

    Private Sub OnGridKeyUp(sender As Object, e As KeyEventArgs)

        If Not IsNothing(ListView_DownloadItems.SelectedItems) AndAlso e.Key = Key.Space Then

            For Each _item As DownloadItem In ListView_DownloadItems.SelectedItems

                If _item.isSelected = True Then
                    _item.isSelected = False
                Else
                    _item.isSelected = True
                End If

            Next

            e.Handled = True

        End If


    End Sub
End Class
