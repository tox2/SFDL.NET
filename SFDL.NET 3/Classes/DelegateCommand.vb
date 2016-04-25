Public Class DelegateCommand
    Implements ICommand
    Private ReadOnly _action As Action

    Public Sub New(action As Action)
        _action = action
    End Sub
    Private Function CanExecute(parameter As Object) As Boolean Implements ICommand.CanExecute
        Return True
    End Function

    Private Sub Execute(parameter As Object) Implements ICommand.Execute
        _action()
    End Sub

    Public Event CanExecuteChanged As EventHandler
    Private Event ICommand_CanExecuteChanged As EventHandler Implements ICommand.CanExecuteChanged
End Class
