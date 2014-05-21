Friend Class CDBuilder

    Public Shared Function CDSTANDARD() As CDSTANDARD
        Dim tmpCDSTANDARD As New CDSTANDARD

        tmpCDSTANDARD.SV = Builder.SV
        tmpCDSTANDARD.Value = CDSTANDARDvalues.Item20030909

        Return tmpCDSTANDARD
    End Function

    Public Shared Function CDHCPARTY(ByVal aCDHCPARTYvalue As CDHCPARTYvalues) As CDHCPARTY
        Dim tmpCDHCPARTY As New CDHCPARTY

        tmpCDHCPARTY.SV = Builder.SV
        tmpCDHCPARTY.Value = aCDHCPARTYvalue

        Return tmpCDHCPARTY
    End Function

    Public Shared Function CDHCPARTY(ByVal aSystem As String, ByVal aSystemVersion As String, ByVal aType As String) As CDHCPARTY
        Dim tmpCDHCPARTY As New CDHCPARTY

        tmpCDHCPARTY.S = CDHCPARTYschemes.LOCAL
        tmpCDHCPARTY.SL = aSystem
        tmpCDHCPARTY.SV = aSystemVersion
        tmpCDHCPARTY.ValueAsString = aType

        Return tmpCDHCPARTY
    End Function

    Public Shared Function CDADDRESS(ByVal aAddressTypeCode As CDADDRESSvalues) As CDADDRESS
        Dim tmpCDADDRESS As New CDADDRESS

        tmpCDADDRESS.S = CDADDRESSschemes.CDADDRESS
        tmpCDADDRESS.SV = Builder.SV
        tmpCDADDRESS.Value = aAddressTypeCode

        Return tmpCDADDRESS
    End Function

    Public Shared Function CDCOUNTRY(ByVal aCountry As CDCOUNTRYvalues) As CDCOUNTRY
        Dim tmpCDCOUNTRY As New CDCOUNTRY

        tmpCDCOUNTRY.SV = Builder.SV
        tmpCDCOUNTRY.Value = aCountry

        Return tmpCDCOUNTRY
    End Function

    Public Shared Function CDTELECOM(ByVal aTelecomTypeCode As String) As CDTELECOM
        Dim tmpCDTELECOM As New CDTELECOM

        tmpCDTELECOM.S = CDTELECOMschemes.CDTELECOM
        tmpCDTELECOM.SV = Builder.SV
        tmpCDTELECOM.Value = aTelecomTypeCode

        Return tmpCDTELECOM
    End Function

    Public Shared Function CDACKNOWLEDGMENT(ByVal aValue As CDACKNOWLEDGMENTvalues) As CDACKNOWLEDGMENT
        Dim tmpCDACKNOWLEDGMENT As New CDACKNOWLEDGMENT

        tmpCDACKNOWLEDGMENT.SV = Builder.SV
        tmpCDACKNOWLEDGMENT.Value = aValue

        Return tmpCDACKNOWLEDGMENT
    End Function

    Public Shared Function CDURGENCY(ByVal aValue As CDURGENCYvalues) As CDURGENCY
        Dim tmpCDURGENCY As New CDURGENCY

        tmpCDURGENCY.SV = Builder.SV
        tmpCDURGENCY.Value = aValue

        Return tmpCDURGENCY
    End Function

    Public Shared Function CDSEX(ByVal aValue As CDSEXvalues) As CDSEX
        Dim tmpCDSEX As New CDSEX

        tmpCDSEX.SV = Builder.SV
        tmpCDSEX.Value = aValue

        Return tmpCDSEX
    End Function

    Public Shared Function CDTRANSACTION(ByVal aTransactionTypeCode As CDTRANSACTIONvalues) As CDTRANSACTION
        Dim tmpCDTRANSACTION As New CDTRANSACTION

        tmpCDTRANSACTION.S = CDTRANSACTIONschemes.CDTRANSACTION
        tmpCDTRANSACTION.SV = Builder.SV
        tmpCDTRANSACTION.Value = aTransactionTypeCode

        Return tmpCDTRANSACTION
    End Function

    Public Shared Function CDTRANSACTION(ByVal aSystem As String, ByVal aSystemVersion As String, ByVal aType As String) As CDTRANSACTION
        Dim tmpCDTRANSACTION As New CDTRANSACTION

        tmpCDTRANSACTION.S = CDTRANSACTIONschemes.LOCAL
        tmpCDTRANSACTION.SL = aSystem
        tmpCDTRANSACTION.SV = aSystemVersion
        tmpCDTRANSACTION.ValueAsString = aType

        Return tmpCDTRANSACTION
    End Function

    Public Shared Function CDITEM(ByVal aSystem As String, ByVal aSystemVersion As String, ByVal aItem As String) As CDITEM
        Dim tmpCDITEM As New CDITEM

        tmpCDITEM.S = CDITEMschemes.LOCAL
        tmpCDITEM.SL = aSystem
        tmpCDITEM.SV = aSystemVersion
        tmpCDITEM.ValueAsString = aItem

        Return tmpCDITEM
    End Function

    Public Shared Function CDITEM(ByVal aItem As CDITEMvalues) As CDITEM
        Dim tmpCDITEM As New CDITEM

        tmpCDITEM.S = CDITEMschemes.CDITEM
        tmpCDITEM.SV = Builder.SV
        tmpCDITEM.Value = aItem

        Return tmpCDITEM
    End Function

    Public Shared Function CDCONTENT(ByVal aType As CDCONTENTschemes, ByVal aValue As String) As CDCONTENT
        Dim tmpCDCONTENT As New CDCONTENT

        tmpCDCONTENT.S = aType
        tmpCDCONTENT.SV = Builder.SV
        tmpCDCONTENT.Value = aValue

        Return tmpCDCONTENT
    End Function

    Public Shared Function CDCONTENT(ByVal aSystem As String, ByVal aSystemVersion As String, ByVal aValue As String) As CDCONTENT
        Dim tmpCDCONTENT As New CDCONTENT

        tmpCDCONTENT.S = CDCONTENTschemes.LOCAL
        tmpCDCONTENT.SL = aSystem
        tmpCDCONTENT.SV = aSystemVersion
        tmpCDCONTENT.Value = aValue

        Return tmpCDCONTENT
    End Function

End Class
