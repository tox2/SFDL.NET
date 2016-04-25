Public Class SpeedreportSettings

    Public Property SpeedreportView As SpeedreportVisibility = SpeedreportVisibility.ShowGUI
    Public Property SpeedreportUsername As String = String.Empty
    Public Property SpeedreportConnection As String = String.Empty
    Public Property SpeedreportComment As String = String.Empty
    Public Property SpeedreportTemplate As String = String.Empty

End Class

Public Enum SpeedreportVisibility
    ShowGUI
    Write2File
    Hide
End Enum
