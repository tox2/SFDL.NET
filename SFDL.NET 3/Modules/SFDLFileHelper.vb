﻿Module SFDLFileHelper

    Private _log As NLog.Logger = NLog.LogManager.GetLogger("SFDLFileHelper")

    Sub CheckAndFixPackageName(ByRef _container As ContainerSession)

        Dim _count As Integer = 1

        For Each _package In _container.ContainerFile.Packages

            If String.IsNullOrWhiteSpace(_package.Name) Then
                _package.Name = String.Format("Package{0}", _count)
            End If

        Next

    End Sub

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

        'SFDL v2 COntainer
        If _version = 0 Then

            Try

                For Each _element As Xml.XmlNode In _xml.GetElementsByTagName("SFDLFileVersion")
                    _version = Integer.Parse(_element.InnerText.ToString)
                Next

            Catch ex As Exception
                _version = 0
            End Try

        End If

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
        _fingerprint = _fingerprint & _container_session.DisplayName
        _fingerprint = _fingerprint & _container_session.ContainerFile.Uploader
        _fingerprint = _fingerprint & _container_session.ContainerFileName

        If Not _container_session.ContainerFile.Packages.Count = 0 Then
            _fingerprint = _fingerprint & _container_session.ContainerFile.Packages(0).Name
        End If

        If Not _container_session.ContainerFile.Packages.Where(Function(mypackage) mypackage.BulkFolderMode = False).Count = 0 Then

            If Not _container_session.ContainerFile.Packages(0).FileList.Count = 0 Then
                _fingerprint = _fingerprint & _container_session.ContainerFile.Packages(0).FileList(0).FileName

                If Not _container_session.ContainerFile.Packages(0).FileList(0).FullPath.Length > 248 Then
                    _fingerprint = _fingerprint & IO.Path.GetDirectoryName(_container_session.ContainerFile.Packages(0).FileList(0).FullPath)
                Else
                    _fingerprint = _fingerprint & System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(_container_session.ContainerFile.Packages(0).FileList(0).FullPath))
                End If


            End If

        Else

            If Not _container_session.ContainerFile.Packages.Where(Function(mypackage) mypackage.BulkFolderMode = True).Count = 0 Then

                If Not _container_session.ContainerFile.Packages(0).BulkFolderList.Count = 0 Then
                    _fingerprint = _fingerprint & _container_session.ContainerFile.Packages(0).BulkFolderList(0).BulkFolderPath
                    _fingerprint = _fingerprint & _container_session.ContainerFile.Packages(0).BulkFolderList(0).PackageName
                End If

            End If

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
                _bulkfolder.PackageName = _decrypt_helper.DecryptString(_bulkfolder.PackageName, _password)

            Next

        Next

    End Sub

    Sub GenerateContainerSessionDownloadItems(ByVal _containersession As ContainerSession, ByVal _mark_files As Boolean)

        Dim _tmp_list As New List(Of DownloadItem)

        For Each _package In _containersession.ContainerFile.Packages

            For Each _file In _package.FileList
                'ToDo: Prüfen ob eintrage plausibel sind

                Dim _dl_item As New DownloadItem()

                _dl_item.Init(_file)

                With _dl_item

                    .PackageName = _package.Name
                    .ParentContainerID = _containersession.ID
                    .LocalFile = GetDownloadFilePath(Application.Current.Resources("Settings"), _containersession, _dl_item)

                    If _mark_files = False Then
                        .isSelected = True
                    Else
                        .isSelected = False
                    End If

                End With

                If Not _dl_item.FileSize = 0 And IO.File.Exists(_dl_item.LocalFile) Then

                    'Check if File is already completly downloaded
                    Dim _fileinfo As New IO.FileInfo(_dl_item.LocalFile)

                    If _fileinfo.Length.Equals(_dl_item.FileSize) Then
                        _dl_item.isSelected = False
                        _dl_item.DownloadStatus = DownloadItem.Status.AlreadyDownloaded
                        _dl_item.SizeDownloaded = _dl_item.FileSize
                    End If

                End If

                _tmp_list.Add(_dl_item)


            Next

        Next

        _containersession.DownloadItems.AddRange(_tmp_list)

    End Sub

    Function GetBulkFileList(ByRef _container_session As ContainerSession) As Boolean

        Dim _ftp As ArxOne.Ftp.FtpClient
        Dim _rt As Boolean = True

        Try

            SetupFTPClient(_ftp, _container_session.ContainerFile.Connection)

            For Each _package In _container_session.ContainerFile.Packages.Where(Function(mypackage) mypackage.BulkFolderMode = True)

                For Each _bulk_folder In _package.BulkFolderList
                    _package.FileList = GetRecursiveListing(_bulk_folder.BulkFolderPath, _ftp, _package.Name)
                Next

            Next

        Catch ex As Exception
            _rt = False
            _log.Error(ex, ex.Message)
        End Try

        _ftp.Dispose()

        Return _rt

    End Function

    Function GetRecursiveListing(ByVal _bulk_folder As String, ByVal _ftp As ArxOne.Ftp.FtpClient, ByVal _packagename As String) As List(Of SFDL.Container.FileItem)

        Dim _ftp_path As New ArxOne.Ftp.FtpPath(_bulk_folder)
        Dim _rt_list As New List(Of SFDL.Container.FileItem)
        Dim _mylog As NLog.Logger = NLog.LogManager.GetLogger("BulkRecursiveListing")
        Dim _ftp_unix_platform As New ArxOne.Ftp.Platform.UnixFtpPlatform

        Try


            For Each _item In ArxOne.Ftp.FtpClientUtility.List(_ftp, _ftp_path)

                Try

                    Dim _entry As ArxOne.Ftp.FtpEntry

                    _entry = FTPHelper.TryParseLine(_item, _bulk_folder)

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
                                _file_item.DirectoryPath = _bulk_folder
                                _file_item.HashType = Container.HashType.None

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

    Private Function CleanDownloadPathInput(strIn As String) As String
        ' Replace invalid characters with empty strings.

        Dim _rt As String = String.Empty
        Dim _org_filename As String = String.Empty

        Try

            If Not String.IsNullOrWhiteSpace(strIn) Then

                _org_filename = IO.Path.GetFileName(strIn)

                _rt = strIn.Replace(_org_filename, "")

                _rt = IO.Path.Combine(_rt, Text.RegularExpressions.Regex.Replace(_org_filename, "[^\w\.@-]", ""))

            End If

        Catch e As TimeoutException
            Return String.Empty
        End Try

        Return _rt

    End Function

    Function GetDownloadFilePath(ByVal _settings As Settings, ByVal _container_session As ContainerSession, ByVal _item As DownloadItem) As String

        Dim _download_dir As String = String.Empty
        Dim _tmp_last_sub_dir As String = String.Empty
        Dim _dowload_local_filename As String = String.Empty

        Try

            _download_dir = IO.Path.Combine(_settings.DownloadDirectory, _container_session.DisplayName)

            If _settings.CreatePackageSubfolder Then
                _download_dir = IO.Path.Combine(_download_dir, _item.PackageName)
            End If

            'If Not IO.Directory.Exists(_download_dir) Then
            '    IO.Directory.CreateDirectory(_download_dir)
            'End If

            _dowload_local_filename = IO.Path.Combine(_download_dir, _item.FileName)

        Catch ex As Exception
            _log.Error(ex, ex.Message)
        End Try

        _dowload_local_filename = CleanDownloadPathInput(_dowload_local_filename)

        Return _dowload_local_filename

    End Function

End Module
