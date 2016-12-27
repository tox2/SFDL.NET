﻿
Imports System.ComponentModel
Imports MahApps.Metro
Imports MahApps.Metro.Controls.Dialogs
Imports NLog

Public Class MainWindow

    Dim _force_exit As Boolean = False

    Public Sub New()

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()
        Me.DataContext = New MainViewModel



    End Sub

    Private Sub LoadTheme()

        If Not String.IsNullOrWhiteSpace(CType(Application.Current.Resources("Settings"), Settings).AppAccent) And Not String.IsNullOrWhiteSpace(CType(Application.Current.Resources("Settings"), Settings).AppTheme) Then
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(CType(Application.Current.Resources("Settings"), Settings).AppAccent), ThemeManager.GetAppTheme(CType(Application.Current.Resources("Settings"), Settings).AppTheme))
        End If

    End Sub

    Private Async Sub MainWindow_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered

        Dim _settings As Settings = Application.Current.Resources("Settings")
        Dim _log As Logger = LogManager.GetLogger("ContentRendered")
        Dim _cnl_helper As New ClicknLoad
        Dim _new_update As Boolean = False

        For Each _arg In Environment.GetCommandLineArgs

            If Not String.IsNullOrWhiteSpace(_arg) Then

                If IO.Path.GetExtension(_arg).ToLower = ".sfdl" Then
                    MainViewModel.ThisInstance.OpenSFDLFile(_arg)
                End If

                If _arg.ToLower.StartsWith("sfdl://") And _settings.ClicknLoad = True Then

                    Dim _local_tmp_file As String = String.Empty

                    _local_tmp_file = Await _cnl_helper.ProcessClicknLoad(_arg.Replace("sfdl://", ""))

                    If Not String.IsNullOrWhiteSpace(_local_tmp_file) AndAlso IO.File.Exists(_local_tmp_file) Then

                        MainViewModel.ThisInstance.OpenSFDLFile(_local_tmp_file)

                    End If

                End If

            End If

        Next

        ComB_Container_Info.DataContext = MainViewModel.ThisInstance
        InstantVideoContainerList.DataContext = MainViewModel.ThisInstance

        If My.Settings.UserWindowState = WindowState.Normal Then

            If Not My.Settings.UserWindowHeight = 0 Then
                Me.Height = My.Settings.UserWindowHeight
            End If

            If Not My.Settings.UserWindowWitdh = 0 Then
                Me.Width = My.Settings.UserWindowWitdh
            End If

        Else
            Me.WindowState = WindowState.Maximized
        End If

        LoadTheme()

        If IO.File.Exists(IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "bin", "unrar.exe")) = False Or IO.File.Exists(IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "bin", "TOAC.exe")) = False Then
            Await ShowMessageAsync(My.Resources.Strings.VariousStrings_Warning, My.Resources.Strings.VariousStrings_UnRARExecutableMissingException)
        End If


#Region "Check and Update InstallState and File Registration"


        If isAdministrator() = True And CheckInstallState() = False Then

            Try 'Versuchen die Dateiendung .sfdl und den URI Handler zu registrieren

                Dim _file_assoiciation As New FileAssociation

                _file_assoiciation.Extension = "sfdl"
                _file_assoiciation.ContentType = "application/sfdl.net"
                _file_assoiciation.FullName = "SFDL.NET Files"
                _file_assoiciation.IconIndex = 0
                _file_assoiciation.IconPath = IO.Path.Combine(IO.Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory), "Icon.ico")
                _file_assoiciation.ProperName = "SFDL.NET File"
                _file_assoiciation.AddCommand("open", System.Reflection.Assembly.GetExecutingAssembly().Location & " " & Chr(34) & "%1" & Chr(34))

                _file_assoiciation.Create()

                _log.Info("SFDL Extension registriert!")

                If _settings.ClicknLoad Then

                    Dim _uri_handler_association As New URIHandlerAssociation

                    _uri_handler_association.RegisterSFDLURIHandler(System.Reflection.Assembly.GetExecutingAssembly().Location)

                    _log.Info("SFDL URI Handler registriert!")

                End If

                UpdateInstallState()

            Catch ex As Exception
                _log.Error(ex, ex.Message)
            End Try

        End If


        If CheckInstallState() = False Then

            Dim _result As MessageDialogResult
            Dim _dialog_settings As New MetroDialogSettings

            _dialog_settings.AffirmativeButtonText = "Ja, bitte"
            _dialog_settings.NegativeButtonText = "Nein, danke"

            _result = Await ShowMessageAsync(My.Resources.Strings.VariousStrings_Warning, My.Resources.Strings.InstallPathChangedPrompt, MessageDialogStyle.AffirmativeAndNegative, _dialog_settings)

            If _result = MessageDialogResult.Affirmative Then
                RunAsAdmin()
            End If

        End If

#End Region

        If _settings.SearchUpdates = True Then

            _new_update = Await IsNewUpdateAvailible()

        If _new_update = True Then
                Await ShowMessageAsync("Update Check", "Es ist ein neues Update verfügbar!" & vbNewLine & "Die neuste Version kannst du auf http://www.sfdl.net herunterladen.")
            End If

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

            My.Settings.Save()

        End If

    End Sub

    Private Sub MenuItem_Click(sender As Object, e As RoutedEventArgs)

        MessageBox.Show(MainViewModel.ThisInstance.DownloadItems(0).IsExpanded)

    End Sub

    Private Sub OnGridKeyUp(sender As Object, e As KeyEventArgs)

        If Not IsNothing(ListView_DownloadItems.SelectedItems) AndAlso e.Key = Key.Enter Then

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

    Private Async Sub SFDL_MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles SFDL_MainWindow.Closing

        Dim _somthing_running As Boolean = False

        If _force_exit = False Then

            For Each _container_session In MainViewModel.ThisInstance.ContainerSessions

                If _container_session.SessionState = ContainerSessionState.DownloadRunning Or _container_session.UnRarChains.Where(Function(mychain) mychain.UnRARRunning = True).Count >= 1 Then
                    _somthing_running = True
                End If

            Next

            If _somthing_running = True Then

                Dim _result As MessageDialogResult
                Dim _dialog_settings As New MetroDialogSettings

                _dialog_settings.AffirmativeButtonText = "Ja"
                _dialog_settings.NegativeButtonText = "Nein"

                e.Cancel = True

                _result = Await ShowMessageAsync("SFDL.NET beenden", "Es läuft aktuell noch ein Download oder UnRAR - Möchtest du die Anwendung trotzdem beenden?", MessageDialogStyle.AffirmativeAndNegative, _dialog_settings)

                If _result = MessageDialogResult.Affirmative Then
                    _force_exit = True
                    Application.Current.Shutdown()
                End If

            Else
                MainViewModel.ThisInstance.SaveSessions()
            End If
        Else
            MainViewModel.ThisInstance.SaveSessions()
        End If

    End Sub

    Private Sub cmd_play_instant_video_Click(sender As Object, e As RoutedEventArgs) Handles cmd_play_instant_video.Click

        Dim _sel_object As ContainerSession

        Try

            _sel_object = InstantVideoContainerList.SelectedItem

            If Not IsNothing(_sel_object) Then

                For Each _chain In _sel_object.UnRarChains

                    Dim _vlc_args As String = String.Empty

                    _vlc_args = String.Format("{0} --no-qt-error-dialogs", Chr(34) & _chain.MasterUnRarChainFile.LocalFile & Chr(34))

                    System.Diagnostics.Process.Start(Chr(34) & GetVLCExecutable() & Chr(34), _vlc_args)

                Next

            End If

        Catch ex As Exception

        End Try

    End Sub
End Class
