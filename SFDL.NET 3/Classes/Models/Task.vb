Public Class Task

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
                RaiseEvent TaskDone(Me)

        End Select

    End Sub

    Public Event TaskDone(ByVal e As Task)

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
