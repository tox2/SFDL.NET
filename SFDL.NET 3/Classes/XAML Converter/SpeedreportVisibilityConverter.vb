Imports System.Globalization

Public Class SpeedreportVisibilityConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert

        Dim _my_parameter As String = CType(parameter, String)

        Select Case _my_parameter

            Case "ShowSpeedreport"

                If CType(value, SpeedreportVisibility) = SpeedreportVisibility.ShowGUI Then
                    Return True
                Else
                    Return False
                End If

            Case "Speedreport2File"
                If CType(value, SpeedreportVisibility) = SpeedreportVisibility.Write2File Then
                    Return True
                Else
                    Return False
                End If

            Case "HideSpeedreport"
                If CType(value, SpeedreportVisibility) = SpeedreportVisibility.Hide Then
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

            Case "ShowSpeedreport"

                If CType(value, Boolean) = True Then
                    Return SpeedreportVisibility.ShowGUI
                Else
                    Return Binding.DoNothing
                End If

            Case "Speedreport2File"

                If CType(value, Boolean) = True Then
                    Return SpeedreportVisibility.Write2File
                Else
                    Return Binding.DoNothing
                End If

            Case "HideSpeedreport"

                If CType(value, Boolean) = True Then
                    Return SpeedreportVisibility.Hide
                Else
                    Return Binding.DoNothing
                End If

            Case Else
                Return Binding.DoNothing

        End Select


    End Function
End Class
