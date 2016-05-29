Public Class ContainerSession
    Public Sub New(ByVal _container As Container.Container)
        Me.ID = Guid.NewGuid
        Me.ContainerFile = _container
        Me.SessionState = ContainerSessionState.Queued
        Me.Priority = 0
    End Sub

    Public Property ID As Guid
    Public Property ContainerFile As SFDL.Container.Container
    Public Property DisplayName As String = String.Empty
    Public Property ContainerFileName As String = String.Empty
    Public Property ContainerFilePath As String = String.Empty
    Public Property SessionState As ContainerSessionState = ContainerSessionState.Queued
    Public Property DownloadStartedTime As Date
    Public Property DownloadStoppedTime As Date
    Public Property ActiveThreads As Integer = 0
    Public Property UnRarChains As New List(Of UnRARChain)
    Public Property DownloadItems As New List(Of DownloadItem)
    Public Property Priority As Integer = 0 '0 is Default -> All Container Sessions are equal
    Public Property Fingerprint As String = String.Empty

End Class
