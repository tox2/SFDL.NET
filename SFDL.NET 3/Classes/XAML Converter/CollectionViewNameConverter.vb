Imports System.Globalization

Public Class CollectionViewNameConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert

        Dim _my_string As String = CType(value, String)

        If IsNothing(_my_string) Then
            Return Binding.DoNothing
        End If

        If String.IsNullOrWhiteSpace(_my_string) Then
            Return Binding.DoNothing
        End If

        If _my_string.Contains(";") Then

            Dim _container As ContainerSession = Nothing

            _container = MainViewModel.ThisInstance.ContainerSessions.Where(Function(mysession) mysession.ID.ToString.Equals(_my_string.Split(";")(1).ToString)).FirstOrDefault

            If Not IsNothing(_container) Then
                Return String.Format("{0}", _container.DisplayName)
            Else
                Return _my_string.Split(";")(0).ToString
            End If

        Else
            Return Binding.DoNothing
        End If

    End Function
    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack

        Return Binding.DoNothing

    End Function

End Class
