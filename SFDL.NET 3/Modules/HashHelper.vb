Imports System.IO
Imports System.Security.Cryptography

Module HashHelper

    Public Function MD5StringHash(ByVal strString As String) As String
        Dim MD5 As New MD5CryptoServiceProvider
        Dim Data As Byte()
        Dim Result As Byte()
        Dim Res As String = ""
        Dim Tmp As String = ""

        Data = Text.Encoding.ASCII.GetBytes(strString)
        Result = MD5.ComputeHash(Data)
        For i As Integer = 0 To Result.Length - 1
            Tmp = Hex(Result(i))
            If Len(Tmp) = 1 Then Tmp = "0" & Tmp
            Res += Tmp
        Next
        Return Res
    End Function

    Public Function MD5FileHash(ByVal sFile As String) As String

        Dim MD5 As New MD5CryptoServiceProvider
        Dim Hash As Byte()
        Dim Result As String = ""
        Dim Tmp As String = ""

        Try

            Using FN As New FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.Read, 8192)

                MD5.ComputeHash(FN)

                Hash = MD5.Hash
                For i As Integer = 0 To Hash.Length - 1
                    Tmp = Hex(Hash(i))
                    If Len(Tmp) = 1 Then Tmp = "0" & Tmp
                    Result += Tmp
                Next

            End Using

        Catch ex As Exception
            Debug.WriteLine("[MD5] {0}", ex.Message)
        End Try

        Return Result

    End Function

    Public Function CRC32FileHash(ByVal sFileName As String) As String

        Dim crc32 As New CRC32Managed
        Dim _rt As String = String.Empty
        Dim hash As UInt32

        Try

            Using stream As New FileStream(sFileName, FileMode.Open, FileAccess.Read, FileShare.Read)

                crc32.ComputeHash(stream)

                hash = crc32.CRC32Hash

                _rt = String.Format("{0:X}", hash)

            End Using

        Catch ex As Exception
            Debug.WriteLine("[CRC32] {0}", ex.Message)
        End Try

        Return _rt

    End Function


    Public Function SHA1FileHash(ByVal sFile As String) As String

        Dim SHA1 As New SHA1CryptoServiceProvider
        Dim Hash As Byte()
        Dim Result As String = ""
        Dim Tmp As String = ""

        Try

            Using FN As New FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.Read, 8192)

                SHA1.ComputeHash(FN)

                Hash = SHA1.Hash

                For i As Integer = 0 To Hash.Length - 1
                    Tmp = Hex(Hash(i))
                    If Len(Tmp) = 1 Then Tmp = "0" & Tmp
                    Result += Tmp
                Next

            End Using

        Catch ex As Exception
            Debug.WriteLine("[SHA1] {0}", ex.Message)
        End Try

        Return Result

    End Function

End Module
