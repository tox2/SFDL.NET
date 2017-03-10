Imports System.Globalization

<ValueConversion(GetType(Long), GetType(String))>
Class FileSizeConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Dim units As String() = {"B", "KB", "MB", "GB", "TB", "PB",
            "EB", "ZB", "YB"}
        Dim size As Double = CLng(value)
        Dim unit As Integer = 0

        While size >= 1024
            size /= 1024
            unit += 1
        End While

        Return [String].Format("{0:0.#} {1}", size, units(unit))
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class