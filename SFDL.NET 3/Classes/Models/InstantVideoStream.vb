Imports System.ComponentModel

Public Class InstantVideoStream
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Public Sub RaisePropertyChanged(ByVal propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Private _isSelected As Boolean = False

    Public Property IsSelected As Boolean
        Set(value As Boolean)
            _isSelected = value
            RaisePropertyChanged("IsSelected")
        End Set
        Get
            Return _isSelected
        End Get
    End Property

    Public Property DisplayName As String
    Public Property ParentSessionID As Guid
    Public Property File As String

End Class
