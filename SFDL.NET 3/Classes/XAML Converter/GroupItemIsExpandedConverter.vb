Imports System.Globalization

Public Class GroupItemIsExpandedConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert

        If IsNothing(value) = False AndAlso value.GetType().FullName.Equals("MS.Internal.Data.CollectionViewGroupInternal") Then

            Dim _session_name As String = String.Empty
            Dim _container As ContainerSession = Nothing

            _session_name = TryCast(value.Name, String)

            _container = MainViewModel.ThisInstance.ContainerSessions.Where(Function(mysession) mysession.ID.ToString.Equals(_session_name)).FirstOrDefault

            Return _container.DownloadItems(0).IsExpanded

        Else
            Return Binding.DoNothing
        End If

    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Return Binding.DoNothing
    End Function
End Class
