Module FTPHelper

    Sub SetupFTPClient(ByRef _ftp_client As ArxOne.Ftp.FtpClient, ByVal _connection_info As SFDL.Container.Connection)

        Dim _creds As Net.NetworkCredential
        Dim _ftp_client_param As New ArxOne.Ftp.FtpClientParameters

        With _connection_info

            If .AuthRequired = True Then
                _creds = New Net.NetworkCredential(.Username, .Password)
            Else
                _creds = New Net.NetworkCredential("anonymous", "Password")
            End If

            With _ftp_client_param
                .ActiveTransferHost = Net.IPAddress.Parse(_connection_info.Host)
                .AnonymousPassword = "sfdl@anon.net"
                .Passive = True
                .SslProtocols = Security.Authentication.SslProtocols.None
                .ChannelProtection = ArxOne.Ftp.FtpProtection.Ftp
                .DefaultEncoding = System.Text.Encoding.UTF8
            End With

            _ftp_client = New ArxOne.Ftp.FtpClient(New Uri(String.Format("ftp://{0}:{1}", .Host, .Port)), _creds, _ftp_client_param)

        End With

    End Sub

End Module
