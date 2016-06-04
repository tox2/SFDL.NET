Imports System.Net

Module Module1

    Sub Main()

        Dim _download_path As String
        Dim _filesize As Long = 0

#Region "Variables for Download Speed Calculation"

        'IO Stream Variablen
        Const Length As Integer = 256
        Dim buffer As [Byte]()
        Dim bytesRead As Integer = 0
        Dim bytestotalread As Integer = 0
        Dim _starttime As DateTime = DateTime.Now

        Dim _percent_downloaded As Integer = 0
        Dim _current As Long = 0
        Dim _ctime As TimeSpan
        Dim elapsed As TimeSpan
        Dim bytesPerSec As Integer = 0

#End Region

        Try

            Dim _param As New ArxOne.Ftp.FtpClientParameters

            Dim _ftp_client As ArxOne.Ftp.FtpClient

            _ftp_client = New ArxOne.Ftp.FtpClient(ArxOne.Ftp.FtpProtection.Ftp, "ftp.otenet.gr", 21, New NetworkCredential("speedtest", "speedtest"))

            _download_path = IO.Path.Combine("E:\Download\", "testfile001.db")

            _filesize = ArxOne.Ftp.FtpClientUtility.ListEntries(_ftp_client, "/").Where(Function(myitem) myitem.Name.Equals("test100Mb.db"))(0).Size

            Using _ftp_read_stream = ArxOne.Ftp.FtpClientUtility.Retr(_ftp_client, New ArxOne.Ftp.FtpPath("/test100Mb.db"), ArxOne.Ftp.FtpTransferMode.Binary)

                Console.WriteLine("Starting Download")

                buffer = New Byte(8192) {}
                bytesRead = _ftp_read_stream.Read(buffer, 0, buffer.Length)

                Using _local_write_stream As New IO.FileStream(_download_path, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.None, 8192, False)

                    While bytesRead > 0

                        Dim _tmp_percent_downloaded As Double = 0
                        Dim _new_perc As Integer = 0
                        Dim _download_speed As String = String.Empty

                        _local_write_stream.Write(buffer, 0, bytesRead)

                        bytesRead = _ftp_read_stream.Read(buffer, 0, Length)
                        bytestotalread += bytesRead

                        elapsed = DateTime.Now.Subtract(_starttime)

#Region "Download Speed / Progress Calculation - If this Part is Commented Out the Download works"

                        _tmp_percent_downloaded = CDbl(_local_write_stream.Position) / CDbl(_filesize)
                        _new_perc = CInt(_tmp_percent_downloaded * 100)

                        bytesPerSec = CInt(If(elapsed.TotalSeconds < 1, bytestotalread, bytestotalread / elapsed.TotalSeconds))


                        If _new_perc <> _percent_downloaded Then 'Nicht jedesmal Updaten

                            Dim _tmp_speed As Double

                            _percent_downloaded = _new_perc

                            _current = bytestotalread
                            _ctime = DateTime.Now.Subtract(_starttime)

                            _tmp_speed = Math.Round(bytesPerSec / 1024, 2)

                            If _tmp_speed >= 1024 Then
                                _download_speed = Math.Round(_tmp_speed / 1024, 2) & " MB/s"
                            Else
                                _download_speed = _tmp_speed & " KB/s"
                            End If

                            Console.WriteLine("Download Speed: " & _download_speed)
                            Console.WriteLine("Completed: " & _percent_downloaded)

                        End If
#End Region

                    End While

                End Using

            End Using

        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try

        Console.WriteLine("Download Complete! - Press any Key to exit")

        Console.Read()

    End Sub

End Module
