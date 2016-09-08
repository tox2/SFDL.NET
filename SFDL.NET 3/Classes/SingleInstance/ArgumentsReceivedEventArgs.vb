

''Code from http://web.archive.org/web/20080506103924/http://www.flawlesscode.com/post/2008/02/Enforcing-single-instance-with-argument-passing.aspx

''' <summary>
''' Holds a list of arguments given to an application at startup.
''' </summary>
Public Class ArgumentsReceivedEventArgs
    Inherits EventArgs
    Public Property Args() As [String]()
        Get
            Return m_Args
        End Get
        Set
            m_Args = Value
        End Set
    End Property
    Private m_Args As [String]()
End Class
