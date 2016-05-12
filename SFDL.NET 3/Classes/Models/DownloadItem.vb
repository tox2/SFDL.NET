Public Class DownloadItem
    Inherits SFDL.Container.FileItem

    Public Sub New(ByVal _fileitem As SFDL.Container.FileItem)

        Me.DirectoryPath = _fileitem.DirectoryPath
        Me.DirectoryRoot = _fileitem.DirectoryRoot
        Me.DownloadProgress = 0
        Me.DownloadSpeed = String.Empty
        Me.DownloadStatus = Status.Queued
        Me.FileName = _fileitem.FileName
        Me.FileHash = _fileitem.FileHash
        Me.FileSize = _fileitem.FileSize
        Me.FullPath = _fileitem.FullPath
        Me.HashType = _fileitem.HashType
        Me.PackageName = _fileitem.PackageName
        Me.isSelected = True

    End Sub

    Public Property isSelected As Boolean = False
    Public Property DownloadProgress As Integer = 0
    Public Property DownloadStatus As Status = Status.Queued
    Public Property DownloadSpeed As String = String.Empty

    Public Enum Status
        Queued
        Running
        Stopped
        Completed
    End Enum

End Class
