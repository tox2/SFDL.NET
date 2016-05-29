Imports System.ComponentModel

Public Class DownloadItem
    Inherits SFDL.Container.FileItem
    Implements IDisposable
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Public Sub RaisePropertyChanged(ByVal propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Private _download_speed As String

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
    Public Property DownloadSpeed As String
        Set(value As String)
            _download_speed = value
            RaisePropertyChanged("DownloadSpeed")
        End Set
        Get
            Return _download_speed
        End Get
    End Property
    Public Property ParentContainerID As Guid
    Public Property DownloadStatusImage As String = "Resources/Icons/appbar.clock.png"

    Public Enum Status
        Queued
        Running
        Stopped
        Completed
    End Enum

#Region "IDisposable Support"
    Private disposedValue As Boolean ' Dient zur Erkennung redundanter Aufrufe.

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: verwalteten Zustand (verwaltete Objekte) entsorgen.
            End If

            ' TODO: nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalize() weiter unten überschreiben.
            ' TODO: große Felder auf Null setzen.
        End If
        disposedValue = True
    End Sub

    ' TODO: Finalize() nur überschreiben, wenn Dispose(disposing As Boolean) weiter oben Code zur Bereinigung nicht verwalteter Ressourcen enthält.
    'Protected Overrides Sub Finalize()
    '    ' Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(disposing As Boolean) weiter oben ein.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' Dieser Code wird von Visual Basic hinzugefügt, um das Dispose-Muster richtig zu implementieren.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(disposing As Boolean) weiter oben ein.
        Dispose(True)
        ' TODO: Auskommentierung der folgenden Zeile aufheben, wenn Finalize() oben überschrieben wird.
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class
