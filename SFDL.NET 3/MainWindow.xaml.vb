
Public Class MainWindow
    Public Sub New()

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()
        Me.DataContext = New MainViewModel
        ' Fügen Sie Initialisierungen nach dem InitializeComponent()-Aufruf hinzu.
        LogHelper.GenerateLogConfig()

    End Sub
End Class
