Imports System.Globalization

Public Class LangauageConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert

        Dim _my_parameter As String = CType(parameter, String)

        Select Case _my_parameter

            Case "de"

                If CType(value, String) = "de" Then
                    Return True
                Else
                    Return False
                End If

            Case "en"
                If CType(value, String) = "en" Then
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

            Case "de"

                If CType(value, Boolean) = True Then
                    Return "de"
                Else
                    Return Binding.DoNothing
                End If

            Case "en"

                If CType(value, Boolean) = True Then
                    Return "en"
                Else
                    Return Binding.DoNothing
                End If

            Case Else
                Return Binding.DoNothing

        End Select

    End Function
End Class

