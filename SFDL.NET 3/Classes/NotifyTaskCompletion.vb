Imports System.ComponentModel
Imports System.Threading.Tasks

''Taken from https://msdn.microsoft.com/en-us/magazine/dn605875.aspx | THX!
Public NotInheritable Class NotifyTaskCompletion(Of TResult)
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Public Sub RaisePropertyChanged(ByVal propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Public Sub New(_task As Task(Of TResult))

        Task = _task

        If Not Task.IsCompleted Then
#Disable Warning BC42358 ' Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
            WatchTaskAsync(Task)
#Enable Warning BC42358 ' Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
        End If

    End Sub

    Private Async Function WatchTaskAsync(task As Task) As Task
        Try
            Await task
        Catch
        End Try

        RaisePropertyChanged("Status")
        RaisePropertyChanged("IsCompleted")
        RaisePropertyChanged("IsNotCompleted")

        If task.IsCanceled Then
            RaisePropertyChanged("IsCanceled")
        ElseIf task.IsFaulted Then
            RaisePropertyChanged("IsFaulted")
            RaisePropertyChanged("Exception")
            RaisePropertyChanged("InnerException")
            RaisePropertyChanged("ErrorMessage")
        Else
            RaisePropertyChanged("IsSuccessfullyCompleted")
            RaisePropertyChanged("Result")
        End If


    End Function


    Private m_Task As Task(Of TResult)

    Public Property Task() As Task(Of TResult)
        Get
            Return m_Task
        End Get
        Private Set
            m_Task = Value
        End Set
    End Property

    Public ReadOnly Property Result() As TResult
        Get
            Return If((Task.Status = TaskStatus.RanToCompletion), Task.Result, Nothing)
        End Get
    End Property
    Public ReadOnly Property Status() As TaskStatus
        Get
            Return Task.Status
        End Get
    End Property
    Public ReadOnly Property IsCompleted() As Boolean
        Get
            Return Task.IsCompleted
        End Get
    End Property
    Public ReadOnly Property IsNotCompleted() As Boolean
        Get
            Return Not Task.IsCompleted
        End Get
    End Property
    Public ReadOnly Property IsSuccessfullyCompleted() As Boolean
        Get
            Return Task.Status = TaskStatus.RanToCompletion
        End Get
    End Property
    Public ReadOnly Property IsCanceled() As Boolean
        Get
            Return Task.IsCanceled
        End Get
    End Property
    Public ReadOnly Property IsFaulted() As Boolean
        Get
            Return Task.IsFaulted
        End Get
    End Property
    Public ReadOnly Property Exception() As AggregateException
        Get
            Return Task.Exception
        End Get
    End Property
    Public ReadOnly Property InnerException() As Exception
        Get
            Return If((Exception Is Nothing), Nothing, Exception.InnerException)
        End Get
    End Property
    Public ReadOnly Property ErrorMessage() As String
        Get
            Return If((InnerException Is Nothing), Nothing, InnerException.Message)
        End Get
    End Property
End Class