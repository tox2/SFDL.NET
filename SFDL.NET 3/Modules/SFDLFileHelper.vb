Module SFDLFileHelper

    Private _log As NLog.Logger = NLog.LogManager.GetLogger("SFDLFileHelper")

    Function GetContainerVersion(ByVal _sfdl_file_path As String) As Integer

        Dim _version As Integer = 0
        Dim _xml As New Xml.XmlDocument

        _xml.LoadXml(My.Computer.FileSystem.ReadAllText(_sfdl_file_path, Text.Encoding.Default))

        Try

            For Each _element As Xml.XmlNode In _xml.GetElementsByTagName("ContainerVersion")
                _version = Integer.Parse(_element.InnerText.ToString)
            Next

        Catch ex As Exception
            _version = 0
        End Try

        _log.Info("SFDL File Version: {0}", _version)

        Return _version

    End Function

    Sub GenerateContainerFingerprint(ByRef _container_session As ContainerSession)

        Dim _fingerprint As String = String.Empty

        _fingerprint = _container_session.ContainerFile.MaxDownloadThreads.ToString
        _fingerprint = _fingerprint & _container_session.ContainerFile.Connection.Host
        _fingerprint = _fingerprint & _container_session.ContainerFile.Connection.Port
        _fingerprint = _fingerprint & _container_session.ContainerFile.Connection.Username
        _fingerprint = _fingerprint & _container_session.ContainerFile.Packages.Count

        If Not _container_session.ContainerFile.Packages.Count = 0 Then
            _fingerprint = _fingerprint & _container_session.ContainerFile.Packages(0).Name
        End If

        _container_session.Fingerprint = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(_fingerprint))

    End Sub

    Sub DecryptSFDLContainer(ByRef _container As Container.Container, ByVal _password As String)

        Dim _decrypt_helper As New SFDL.Container.Decrypt

        With _container

            .Description = _decrypt_helper.DecryptString(.Description, _password)
            .Uploader = _decrypt_helper.DecryptString(.Uploader, _password)
            .Encrypted = False

        End With

        With _container.Connection

            .Host = _decrypt_helper.DecryptString(.Host, _password)
            .Password = _decrypt_helper.DecryptString(.Password, _password)
            .Username = _decrypt_helper.DecryptString(.Username, _password)

        End With

        For Each _package In _container.Packages

            _package.Name = _decrypt_helper.DecryptString(_package.Name, _password)

            For Each _item In _package.FileList

                With _item

                    .DirectoryPath = _decrypt_helper.DecryptString(.DirectoryPath, _password)
                    .DirectoryRoot = _decrypt_helper.DecryptString(.DirectoryRoot, _password)
                    .FileName = _decrypt_helper.DecryptString(.FileName, _password)
                    .FullPath = _decrypt_helper.DecryptString(.FullPath, _password)
                    .PackageName = _decrypt_helper.DecryptString(.PackageName, _password)

                End With

            Next

            For Each _bulkfolder In _package.BulkFolderList

                _bulkfolder.BulkFolderPath = _decrypt_helper.DecryptString(_bulkfolder.BulkFolderPath, _password)
                _bulkfolder.Name = _decrypt_helper.DecryptString(_bulkfolder.Name, _password)

            Next

        Next

    End Sub

    Sub GenerateContainerSessionDownloadItems(ByVal _containersession As ContainerSession, ByVal _mark_files As Boolean)

        Dim _tmp_list As New List(Of DownloadItem)

        For Each _package In _containersession.ContainerFile.Packages
            For Each _file In _package.FileList
                'ToDo: Prüfen ob eintrage plausibel sind

                Using _dl_item As New DownloadItem(_file)

                    _dl_item.PackageName = _package.Name
                    _dl_item.ParentContainerID = _containersession.ID

                    If _mark_files = False Then
                        _dl_item.isSelected = True
                    Else
                        _dl_item.isSelected = False
                    End If

                    _tmp_list.Add(_dl_item)

                End Using

            Next
        Next

        _containersession.DownloadItems.AddRange(_tmp_list)

    End Sub

    Function GetBulkFileList(ByRef _container_session As ContainerSession) As Boolean

        Dim _ftp As ArxOne.Ftp.FtpClient
        Dim _creds As Net.NetworkCredential
        Dim _ftp_client_param As New ArxOne.Ftp.FtpClientParameters
        Dim _rt As Boolean = True

        Try

            With _container_session.ContainerFile.Connection

                If .AuthRequired = True Then
                    _creds = New Net.NetworkCredential(.Username, .Password)
                Else
                    _creds = New Net.NetworkCredential("anonymous", "Password")
                End If

                With _ftp_client_param
                    .ActiveTransferHost = Net.IPAddress.Parse(_container_session.ContainerFile.Connection.Host)
                    .AnonymousPassword = "sfdl@anon.net"
                    .Passive = True
                    .SslProtocols = Security.Authentication.SslProtocols.None
                    .ChannelProtection = ArxOne.Ftp.FtpProtection.Ftp
                    .DefaultEncoding = System.Text.Encoding.UTF8
                End With

                _ftp = New ArxOne.Ftp.FtpClient(New Uri(String.Format("ftp://{0}:{1}", .Host, .Port)), _creds, _ftp_client_param)

                _ftp.ServerType.ToString()

            End With

            For Each _package In _container_session.ContainerFile.Packages.Where(Function(mypackage) mypackage.BulkFolderMode = True)

                For Each _bulk_folder In _package.BulkFolderList
                    _package.FileList = GetRecursiveListing(_bulk_folder.BulkFolderPath, _ftp, _package.Name)
                Next

            Next

        Catch ex As Exception
            _rt = False
            _log.Error(ex, ex.Message)
        End Try

        Return _rt

    End Function

    Function GetRecursiveListing(ByVal _bulk_folder As String, ByVal _ftp As ArxOne.Ftp.FtpClient, ByVal _packagename As String) As List(Of SFDL.Container.FileItem)

        Dim _ftp_path As New ArxOne.Ftp.FtpPath(_bulk_folder)
        Dim _rt_list As New List(Of SFDL.Container.FileItem)
        Dim _mylog As NLog.Logger = NLog.LogManager.GetLogger("BulkRecursiveListing")

        Try

            For Each _item In ArxOne.Ftp.FtpClientUtility.List(_ftp, _ftp_path)

                Try

                    Dim _entry As ArxOne.Ftp.FtpEntry

                    _entry = _ftp.Platform.Parse(_item, _bulk_folder)

                    If Not IsNothing(_entry) Then

                        If _entry.Type = ArxOne.Ftp.FtpEntryType.Directory And Not (_entry.Name.ToString.Equals(".") Or _entry.Name.ToString.Equals("..")) Then
                            _rt_list.AddRange(GetRecursiveListing(_entry.Path.ToString, _ftp, _packagename))
                        Else

                            If _entry.Type = ArxOne.Ftp.FtpEntryType.File Then

                                Dim _file_item As New SFDL.Container.FileItem

                                _file_item.FullPath = _entry.Path.ToString
                                _file_item.FileName = _entry.Path.GetFileName
                                _file_item.FileSize = _entry.Size
                                _file_item.PackageName = _packagename

                                _rt_list.Add(_file_item)

                            End If

                        End If

                    End If

                Catch ex As Exception
                    _mylog.Error(ex, ex.Message)
                End Try

            Next

        Catch ex As Exception
            _mylog.Error(ex, ex.Message)
        End Try

        Return _rt_list

    End Function

End Module
