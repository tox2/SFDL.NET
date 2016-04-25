Imports System.Globalization

<ValueConversion(GetType(Boolean), GetType(Boolean))>
Public Class InvertBooleanConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Dim original As Boolean = CBool(value)
        Return Not original
    End Function
    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Dim original As Boolean = CBool(value)
        Return Not original
    End Function
End Class