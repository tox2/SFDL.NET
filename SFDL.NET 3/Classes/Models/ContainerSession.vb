﻿Public Class ContainerSession
    Public Sub New(ByVal _container As Container.Container)
        Me.ContainerSessionID = Guid.NewGuid
        Me.ContainerFile = _container
        Me.SessionState = ContainerSessionState.Queued
    End Sub

    Public Property ContainerSessionID As Guid
    Public Property ContainerFile As SFDL.Container.Container
    Public Property ContainerFileName As String
    Public Property ContainerFilePath As String
    Public Property SessionState As ContainerSessionState
    Public Property DownloadStartedTime As Date
    Public Property DownloadStoppedTime As Date
    Public Property ActiveThreads As Integer
    Public Property UnRarChains As New List(Of UnRARChain)
    Public Property DownloadItems As New List(Of DownloadItem)

End Class