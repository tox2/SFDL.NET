Module WhoisHelper

    Private _log As NLog.Logger = NLog.LogManager.GetLogger("WhoisHelper")
    ''' <summary>
    ''' Funktion zum umsetzten der IP-Addresse in einen Ländercode
    ''' </summary>
    Public Function Resolve(ByVal _ip As String) As String

        Dim XMLReader As New Xml.XmlDocument
        Dim xmlKD As Xml.XmlElement
        Dim _country_code As String

        _log.Info(String.Format("Queryring WohIs für IP {0}", _ip))

        _country_code = ""

        XMLReader.Load("http://xml.utrace.de/?query=" & _ip)

        xmlKD = CType(XMLReader.DocumentElement.ChildNodes(0), Xml.XmlElement)

        For Each _node As Xml.XmlNode In xmlKD.ChildNodes

            If _node.Name = "countrycode" Then

                _country_code = _node.ChildNodes(0).Value

            End If

        Next

        _log.Info(String.Format("CounterCode determined : {0}", _country_code))

        Return _country_code

    End Function

    Public Function DownloadFlagImage(ByVal _countrycode As String) As System.Drawing.Image

        Dim _flag As System.Drawing.Image

        _flag = DownloadImage("http://n1.dlcache.com/flags/" & _countrycode.ToLower & ".gif")

        Return _flag

    End Function

    ''' <summary>
    ''' Function to download Image from website
    ''' </summary>
    ''' <param name="_URL">URL address to download image</param>
    ''' <return>Image</return>
    Private Function DownloadImage(ByVal _URL As String) As System.Drawing.Image

        Dim _tmpImage As System.Drawing.Image = Nothing

        Try
            ' Open a connection
            Dim _HttpWebRequest As System.Net.HttpWebRequest = CType(System.Net.HttpWebRequest.Create(_URL), System.Net.HttpWebRequest)

            _HttpWebRequest.AllowWriteStreamBuffering = True

            ' You can also specify additional header values like the user agent or the referer: (Optional)
            _HttpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1)"

            ' set timeout for 20 seconds (Optional)
            _HttpWebRequest.Timeout = 20000

            ' Request response:
            Dim _WebResponse As System.Net.WebResponse = _HttpWebRequest.GetResponse()

            ' Open data stream:
            Dim _WebStream As System.IO.Stream = _WebResponse.GetResponseStream()

            ' convert webstream to image
            _tmpImage = System.Drawing.Image.FromStream(_WebStream)

            ' Cleanup
            _WebResponse.Close()
        Catch _Exception As Exception
            ' Error
            Console.WriteLine("Exception caught in process: {0}", _Exception.ToString())
            Return Nothing
        End Try

        Return _tmpImage

    End Function

End Module
