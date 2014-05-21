Public Class Serializer

    Public Shared Function toXML(ByVal aMessage As kmehrmessage) As String
        Using tmpStringWriter As New System.IO.StringWriter
            myMessageSerializer.Serialize(tmpStringWriter, aMessage)
            tmpStringWriter.Close()

            Return tmpStringWriter.ToString
        End Using
    End Function

    Public Shared Function fromXML(ByVal aMessage As String) As kmehrmessage
        Using tmpStringReader As New System.IO.StringReader(aMessage)
            Dim tmpMessage As kmehrmessage = DirectCast(myMessageSerializer.Deserialize(tmpStringReader), kmehrmessage)
            tmpStringReader.Close()

            Return tmpMessage
        End Using
    End Function

    Private Shared myMessageSerializer As New Xml.Serialization.XmlSerializer(GetType(KMEHR.kmehrmessage))
End Class
