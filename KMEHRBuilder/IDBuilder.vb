Friend Class IDBuilder

    Public Shared Function IDKMEHR(ByVal aCreationDate As Date) As IDKMEHR
        Dim tmpIDKMEHR As New IDKMEHR

        tmpIDKMEHR.S = IDKMEHRschemes.IDKMEHR
        tmpIDKMEHR.SV = Builder.SV
        tmpIDKMEHR.Value = aCreationDate.ToString("yyyyMMddHHmmssfff")

        Return tmpIDKMEHR
    End Function

    Public Shared Function IDKMEHR(ByVal aID As Long) As IDKMEHR
        Dim tmpIDKMEHR As New IDKMEHR

        tmpIDKMEHR.S = IDKMEHRschemes.IDKMEHR
        tmpIDKMEHR.SV = Builder.SV
        tmpIDKMEHR.Value = aID.ToString

        Return tmpIDKMEHR
    End Function

    Public Shared Function IDKMEHR(ByVal aSystem As String, ByVal aSystemVersion As String, ByVal aID As String) As IDKMEHR
        Dim tmpIDKMEHR As New IDKMEHR

        tmpIDKMEHR.S = IDKMEHRschemes.LOCAL
        tmpIDKMEHR.SL = aSystem
        tmpIDKMEHR.SV = aSystemVersion
        tmpIDKMEHR.Value = aID

        Return tmpIDKMEHR
    End Function

    Public Shared Function IDHCPARTY(ByVal aHCParty As String) As IDHCPARTY
        Dim tmpIDHCPARTY As New IDHCPARTY

        tmpIDHCPARTY.S = IDHCPARTYschemes.IDHCPARTY
        tmpIDHCPARTY.SV = Builder.SV
        tmpIDHCPARTY.Value = aHCParty

        Return tmpIDHCPARTY
    End Function

    Public Shared Function IDHCPARTY(ByVal aSystem As String, ByVal aSystemVersion As String, ByVal aHCParty As String) As IDHCPARTY
        Dim tmpIDHCPARTY As New IDHCPARTY

        tmpIDHCPARTY.S = IDHCPARTYschemes.LOCAL
        tmpIDHCPARTY.SL = aSystem
        tmpIDHCPARTY.SV = aSystemVersion
        tmpIDHCPARTY.Value = aHCParty

        Return tmpIDHCPARTY
    End Function

    Public Shared Function IDPATIENT(ByVal aID As String) As IDPATIENT
        Dim tmpIDPATIENT As New IDPATIENT

        tmpIDPATIENT.S = IDPATIENTschemes.IDPATIENT
        tmpIDPATIENT.SV = Builder.SV
        tmpIDPATIENT.Value = aID

        Return tmpIDPATIENT
    End Function

    Public Shared Function IDPATIENT(ByVal aSystem As String, ByVal aVersion As String, ByVal aID As String) As IDPATIENT
        Dim tmpIDPATIENT As New IDPATIENT

        tmpIDPATIENT.S = IDPATIENTschemes.LOCAL
        tmpIDPATIENT.SL = aSystem
        tmpIDPATIENT.SV = aVersion
        tmpIDPATIENT.Value = aID

        Return tmpIDPATIENT

    End Function
End Class
