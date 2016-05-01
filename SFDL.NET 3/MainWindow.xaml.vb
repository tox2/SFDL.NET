
Public Class MainWindow
    Public Sub New()

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()

        ' Fügen Sie Initialisierungen nach dem InitializeComponent()-Aufruf hinzu.

    End Sub

    Private Sub cmd_Settings_Click(sender As Object, e As RoutedEventArgs) Handles cmd_Settings.Click

        Dim _settings_dialog As New SettingsWindow

        _settings_dialog.ShowDialog()

    End Sub
End Class
