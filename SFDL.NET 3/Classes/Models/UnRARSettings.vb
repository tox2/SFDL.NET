Public Class UnRARSettings

    Public Property UnRARAfterDownload As Boolean = True
    Public Property DeleteAfterUnRAR As Boolean = False
    Public Property UseUnRARPasswordList As Boolean = True
    Public Property UnRARPasswordList As New ObjectModel.ObservableCollection(Of String)

End Class
