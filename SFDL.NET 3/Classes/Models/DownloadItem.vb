Imports System.ComponentModel

Public Class DownloadItem
    Inherits SFDL.Container.FileItem
    Implements IDisposable
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Public Sub RaisePropertyChanged(ByVal propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Private _download_speed As String = String.Empty
    Private _selected As Boolean = False
    Private _download_progress As Integer = 0
    Private _parent_container_id As Guid
    Private _status_image As String = "Resources/Icons/appbar.sign.parking.png"
    Private _status As Status = Status.None
    Private _status_string As String = String.Empty
    Private _id As Guid

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

        _id = New Guid

    End Sub

    Public Property isSelected As Boolean
        Set(value As Boolean)
            _selected = value
            RaisePropertyChanged("isSelected")

            If value = True Then
                Me.DownloadStatus = Status.Queued
            Else
                Me.DownloadStatus = Status.None
            End If

        End Set
        Get
            Return _selected
        End Get
    End Property
    Public Property DownloadProgress As Integer
        Set(value As Integer)
            _download_progress = value
            RaisePropertyChanged("DownloadProgress")
        End Set
        Get
            Return _download_progress
        End Get
    End Property

    Public ReadOnly Property DownloadStatusString
        Get

            Select Case Me.DownloadStatus

                Case Status.None

                    Return String.Empty

                Case Status.Queued

                    Return My.Resources.Strings.DownloadStatus_Queued

                Case Status.Running

                    Return My.Resources.Strings.DownloadStatus_Running

                Case Status.Stopped

                    Return My.Resources.Strings.DownloadStatus_Stopped

                Case Status.Completed

                    Return My.Resources.Strings.DownloadStatus_Completed

                Case Status.Completed_HashInvalid

                    Return My.Resources.Strings.DownloadStatus_Completed_HashInvalid

                Case Status.Completed_HashValid

                    Return My.Resources.Strings.DownloadStatus_Completed_HashValid

                Case Status.Failed

                    Return My.Resources.Strings.DownloadStatus_Failed

                Case Status.Failed_FileNameTooLong

                    Return My.Resources.Strings.DownloadStatus_Failed_FileNameTooLong

                Case Status.Failed_NotEnoughDiskSpace

                    Return My.Resources.Strings.DownloadStatus_Failed_NotEnoughDiskSpace

                Case Status.Failed_ServerFull

                    Return My.Resources.Strings.DownloadStatus_Failed_ServerFull

                Case Else

                    Return My.Resources.Strings.DownloadStatus_Failed

            End Select

        End Get
    End Property

    Public Property DownloadStatus As Status
        Set(value As Status)
            _status = value

            RaisePropertyChanged("DownloadStatus")
            RaisePropertyChanged("DownloadStatusString")

            Select Case value

                Case Status.None

                    Me.DownloadStatusImage = "Resources/Icons/appbar.sign.parking.png"

                Case Status.Completed

                    Me.DownloadStatusImage = "Resources/Icons/appbar.check.png"

                Case Status.Queued

                    Me.DownloadStatusImage = "Resources/Icons/appbar.clock.png"

                Case Status.Running

                    Me.DownloadStatusImage = "Resources/Icons/appbar.cabinet.in.png"

                Case Status.Stopped

                    Me.DownloadStatusImage = "Resources/Icons/appbar.control.stop.png"

                Case Status.Completed_HashValid

                    Me.DownloadStatusImage = "Resources/Icons/appbar.check.png"

                Case Status.Completed_HashInvalid

                    Me.DownloadStatusImage = "Resources/Icons/appbar.alert.png"

                Case Status.Failed

                    Me.DownloadStatusImage = "Resources/Icons/appbar.stop.png"

                Case Status.Failed_FileNameTooLong

                    Me.DownloadStatusImage = "Resources/Icons/appbar.dimension.line.width.png"

                Case Status.Failed_NotEnoughDiskSpace

                    Me.DownloadStatusImage = "Resources/Icons/appbar.stop.png"

                Case Status.Failed_ServerFull

                    Me.DownloadStatusImage = "Resources/Icons/appbar.cup.full.png"


            End Select

        End Set
        Get
            Return _status
        End Get
    End Property
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
        Set(value As Guid)
            _parent_container_id = value
            RaisePropertyChanged("ParentContainerID")
        End Set
        Get
            Return _parent_container_id
        End Get
    End Property
    Public Property DownloadStatusImage As String
        Set(value As String)
            _status_image = value
            RaisePropertyChanged("DownloadStatusImage")
        End Set
        Get
            Return _status_image
        End Get
    End Property

    Public Property LocalFile As String = String.Empty

    Public ReadOnly Property ID As Guid
        Get
            Return _id
        End Get
    End Property

    Public Property SizeDownloaded As Long = 0
    Public Property RetryPossible As Boolean = False
    Public Property RetryCount As Integer = 0


    Public Enum Status
        None
        Queued
        Running
        Stopped
        Retry
        Failed
        Failed_FileNameTooLong
        Failed_NotEnoughDiskSpace
        Failed_ServerFull
        Failed_ServerDown
        Failed_ConnectionError
        Failed_AuthError
        Failed_FileNotFound
        Failed_DirectoryNotFound
        Failed_InternalServerError
        Completed
        Completed_HashValid
        Completed_HashInvalid
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
