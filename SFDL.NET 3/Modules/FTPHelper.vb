Imports System.Text
Imports ArxOne.Ftp

Module FTPHelper

    Private _log As NLog.Logger = NLog.LogManager.GetLogger("FTPClient")

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

                If _connection_info.DataConnectionType = Container.FTPDataConnectionType.Passive Then
                    .Passive = True
                Else
                    .Passive = False
                End If

                .SslProtocols = _connection_info.SSLProtocol

                Select Case _connection_info.CharacterEncoding

                    Case Container.CharacterEncoding.ASCII

                        .DefaultEncoding = System.Text.Encoding.ASCII

                    Case Container.CharacterEncoding.Standard

                        .DefaultEncoding = System.Text.Encoding.Default

                    Case Container.CharacterEncoding.UTF7

                        .DefaultEncoding = System.Text.Encoding.UTF7

                    Case Container.CharacterEncoding.UTF8

                        .DefaultEncoding = System.Text.Encoding.UTF8

                End Select

                .ConnectTimeout = TimeSpan.FromSeconds(_connection_info.ConnectTimeout)
                .SessionTimeout = TimeSpan.FromSeconds(_connection_info.CommandTimeout)
                .ReadWriteTimeout = TimeSpan.FromSeconds(_connection_info.CommandTimeout)

            End With

            _ftp_client = New ArxOne.Ftp.FtpClient(New Uri(String.Format("ftp://{0}:{1}", .Host, .Port)), _creds, _ftp_client_param)

            _ftp_client.SendSingleCommand("NOOP")

            If _ftp_client.ServerFeatures.HasFeature("UTF8") And _connection_info.CharacterEncoding = Container.CharacterEncoding.UTF8 Then
                _ftp_client.SendSingleCommand("OPTS", "UTF8 ON")
            End If

        End With

        AddHandler _ftp_client.Reply, AddressOf _log_ftp
        AddHandler _ftp_client.Request, AddressOf _log_ftp
        AddHandler _ftp_client.IOError, AddressOf _log_ftp


    End Sub

    Private Sub _log_ftp(sender As Object, e As ArxOne.Ftp.ProtocolMessageEventArgs)

        Dim _log_line As New StringBuilder

        If Not IsNothing(e.RequestCommand) Then
            _log_line.AppendLine(e.RequestCommand)
        End If

        If Not IsNothing(e.RequestParameters) Then
            For Each _line In e.RequestParameters
                _log_line.AppendLine(_line)
            Next
        End If

        If Not IsNothing(e.Reply) Then

            _log_line.AppendLine(e.Reply.Code)

            For Each _line In e.Reply.Lines
                _log_line.AppendLine(_line)
            Next

        End If

        _log.Info(_log_line)

    End Sub

    Function TryParseLine(ByVal _item As String, _parent_folder As String)

        Dim _ftp_unix_platform As New ArxOne.Ftp.Platform.UnixFtpPlatform
        Dim _ftp_windows_platform As New ArxOne.Ftp.Platform.WindowsFtpPlatform
        Dim _ftp_filezilla_platform As New ArxOne.Ftp.Platform.WindowsFileZillaFtpPlatform
        Dim _rt As ArxOne.Ftp.FtpEntry = Nothing

        _rt = _ftp_unix_platform.Parse(_item, _parent_folder)

        If IsNothing(_rt) Then
            _rt = _ftp_windows_platform.Parse(_item, _parent_folder)
        End If

        If IsNothing(_rt) Then
            _rt = _ftp_filezilla_platform.Parse(_item, _parent_folder)
        End If

        Return _rt

    End Function

End Module
