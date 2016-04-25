Imports System.Globalization

Public Class ExistingFileHandlingConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert

        Dim _my_parameter As String = CType(parameter, String)

        Select Case _my_parameter

            Case "ResumeFile"

                If CType(value, ExistingFileHandling) = ExistingFileHandling.ResumeFile Then
                    Return True
                Else
                    Return False
                End If

            Case "OverwriteFile"
                If CType(value, ExistingFileHandling) = ExistingFileHandling.OverwriteFile Then
                    Return True
                Else
                    Return False
                End If

            Case Else
                Return Binding.DoNothing

        End Select

    End Function
    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack

        Dim _my_parameter As String = CType(parameter, String)

        Select Case _my_parameter

            Case "ResumeFile"

                If CType(value, Boolean) = True Then
                    Return ExistingFileHandling.ResumeFile
                Else
                    Return Binding.DoNothing
                End If

            Case "OverwriteFile"

                If CType(value, Boolean) = True Then
                    Return ExistingFileHandling.OverwriteFile
                Else
                    Return Binding.DoNothing
                End If

            Case Else
                Return Binding.DoNothing

        End Select

    End Function
End Class
