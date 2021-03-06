﻿<Serializable>
Public Class ContainerSession

    Private _lock_instant_video_streams As New Object

    Public Sub InitCollectionSync()

        _lock_instant_video_streams = New Object

        Me.InstantVideoStreams = New ObjectModel.ObservableCollection(Of InstantVideoStream)

        BindingOperations.EnableCollectionSynchronization(InstantVideoStreams, _lock_instant_video_streams)

    End Sub

    Public Sub Init(ByVal _container As Container.Container)

        Me.ID = Guid.NewGuid
        Me.ContainerFile = _container
        Me.SessionState = ContainerSessionState.Queued
        Me.Priority = 0
        Me.InstantVideoStreams = New ObjectModel.ObservableCollection(Of InstantVideoStream)

        BindingOperations.EnableCollectionSynchronization(InstantVideoStreams, _lock_instant_video_streams)

    End Sub

    Public Property ID As Guid
    Public Property ContainerFile As SFDL.Container.Container
    Public Property DisplayName As String = String.Empty
    Public Property ContainerFileName As String = String.Empty
    Public Property ContainerFilePath As String = String.Empty
    Public Property SessionState As ContainerSessionState = ContainerSessionState.Queued
    Public Property DownloadStartedTime As Date = Date.MinValue
    Public Property DownloadStoppedTime As Date = Date.MinValue
    Public Property UnRarChains As New List(Of UnRARChain)
    Public Property DownloadItems As New List(Of DownloadItem)
    Public Property Priority As Integer = 0 '0 is Default -> All Container Sessions are equal
    Public Property Fingerprint As String = String.Empty
    Public Property SynLock As New Object
    <Xml.Serialization.XmlIgnore>
    Public Property WIG As Amib.Threading.IWorkItemsGroup = Nothing
    Public Property SingleSessionMode As Boolean = False
    Public Property InstantVideoStreams As ObjectModel.ObservableCollection(Of InstantVideoStream)
    Public Property LocalDownloadRoot As String = String.Empty

End Class
