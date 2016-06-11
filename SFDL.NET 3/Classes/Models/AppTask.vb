Imports System.ComponentModel

Public Class AppTask
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private _taskdisplaytext As String = String.Empty
    Private _taskstatusimage As String = String.Empty
    Private _taskstatus As TaskStatus
    Private _taskid As Guid
    Public Sub New(ByVal _displaytext As String)

        _taskid = Guid.NewGuid
        _taskdisplaytext = _displaytext
        _taskstatus = TaskStatus.Running
        _taskstatusimage = "Resources/Icons/appbar.control.fastforward.variant.png"

    End Sub

    Public Sub RaisePropertyChanged(ByVal propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Public Sub SetTaskStatus(ByVal _status As TaskStatus, ByVal _displaytext As String)

        _taskdisplaytext = _displaytext

        Select Case _status

            Case TaskStatus.Faulted

                _taskstatusimage = "Resources/Icons/appbar.stop.png"

            Case TaskStatus.Canceled

                _taskstatusimage = "Resources/Icons/appbar.stop.png"

            Case TaskStatus.Running

                _taskstatusimage = "Resources/Icons/appbar.control.fastforward.variant.png"

            Case TaskStatus.RanToCompletion

                _taskstatusimage = "Resources/Icons/appbar.check.png"

        End Select

        RaisePropertyChanged("TaskStatusImage")
        RaisePropertyChanged("TaskDisplayText")
        RaisePropertyChanged("TaskStatus")

        If Not _status = TaskStatus.Running Then
            RaiseEvent TaskDone(Me)
        End If

    End Sub

    Public Event TaskDone(ByVal e As AppTask)

    Public ReadOnly Property TaskStatusImage As String
        Get
            Return _taskstatusimage
        End Get
    End Property
    Public ReadOnly Property TaskID As Guid
        Get
            Return _taskid
        End Get
    End Property
    Public ReadOnly Property TaskDisplayText As String
        Get
            Return _taskdisplaytext
        End Get
    End Property

    Public ReadOnly Property TaskStatus As TaskStatus
        Get
            Return _taskstatus
        End Get
    End Property

End Class
