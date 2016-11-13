Imports SFDL.Container

Public Class UnRARChain

    Public Property MasterUnRarChainFile As DownloadItem
    Public Property ChainMemberFiles As New List(Of DownloadItem)
    Public Property ReadyForInstantVideo As Boolean = False
    Public Property UnRARDone As Boolean = False
    Public Property UnRARRunning As Boolean = False

End Class
