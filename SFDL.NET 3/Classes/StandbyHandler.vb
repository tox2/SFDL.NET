Imports SFDL.NET3.NativeMethods

Public Class StandyHandler
    Public Function PreventStandby() As NativeMethods.EXECUTION_STATE

        Try
            Return NativeMethods.SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED Or EXECUTION_STATE.ES_CONTINUOUS Or EXECUTION_STATE.ES_DISPLAY_REQUIRED)
        Catch ex As Exception
            Return NativeMethods.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS)
        End Try

    End Function

End Class