Imports Microsoft.Win32

Public Class URIHandlerAssociation

    Public Function RegisterSFDLURIHandler(ByVal _path As String) As Boolean

        Dim _reg_key As RegistryKey
        Dim _key_found As Boolean = False

        Try

            For Each _key In My.Computer.Registry.ClassesRoot.GetSubKeyNames
                If _key.ToString.Equals("sfdl") Then
                    _key_found = True
                End If
            Next

            If _key_found = True Then
                My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("sfdl")
            End If

            _reg_key = My.Computer.Registry.ClassesRoot.CreateSubKey("sfdl")

            _reg_key.SetValue("", "URL:SFDL.NET Protocol Handler")

            _reg_key.SetValue("URL Protocol", "", RegistryValueKind.String)

            _reg_key = My.Computer.Registry.ClassesRoot.CreateSubKey("sfdl\DefaultIcon")

            _reg_key.SetValue("", Chr(34) & _path & Chr(34))

            My.Computer.Registry.ClassesRoot.CreateSubKey("sfdl\shell")
            My.Computer.Registry.ClassesRoot.CreateSubKey("sfdl\shell\open")

            _reg_key = My.Computer.Registry.ClassesRoot.CreateSubKey("sfdl\shell\open\command")

            _reg_key.SetValue("", Chr(34) & _path & Chr(34) & " " & Chr(34) & "%1" & Chr(34), RegistryValueKind.String)

        Catch ex As Exception
            Debug.WriteLine("URI Handler konnte nicht registriert werden." & vbNewLine & ex.Message)
            Return False
        End Try

        Return True

    End Function

End Class