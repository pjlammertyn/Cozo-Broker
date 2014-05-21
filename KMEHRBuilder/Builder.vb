Public Class Builder

    Public Shared ReadOnly SV As String = "1.0"

    Public Shared Function standard() As standard
        Dim tmpStandard As New standard

        tmpStandard.cd = CDBuilder.CDSTANDARD()

        Return tmpStandard
    End Function

    Public Shared Function country(ByVal aCountry As CDCOUNTRYvalues) As country
        Dim tmpCountry As New country

        tmpCountry.cd = CDBuilder.CDCOUNTRY(aCountry)

        Return tmpCountry
    End Function

    Public Shared Function texttype(ByVal aValue As String) As textType
        Dim tmpTexttype As New textType

        tmpTexttype.L = "nl"
        tmpTexttype.Value = aValue

        Return tmpTexttype
    End Function

    Public Shared Function lnkType(ByVal aMediaType As CDMEDIATYPEvalues, ByVal aURL As String) As lnkType
        Dim tmpLnkType As New lnkType

        tmpLnkType.TYPE = CDLNKvalues.multimedia
        tmpLnkType.MEDIATYPE = aMediaType
        tmpLnkType.MEDIATYPESpecified = True
        tmpLnkType.URL = aURL

        Return tmpLnkType
    End Function

    Public Shared Function lnkType(ByVal aMediaType As CDMEDIATYPEvalues, ByVal aValue As Byte()) As lnkType
        Dim tmpLnkType As New lnkType

        tmpLnkType.TYPE = CDLNKvalues.multimedia
        tmpLnkType.MEDIATYPE = aMediaType
        tmpLnkType.MEDIATYPESpecified = True
        tmpLnkType.SIZE = aValue.Length.ToString
        tmpLnkType.Value = aValue

        Return tmpLnkType
    End Function

    Public Shared Function address(ByVal aAddressTypeCode As CDADDRESSvalues, _
                            Optional ByVal aCountry As country = Nothing, _
                            Optional ByVal aZip As String = "", _
                            Optional ByVal aCity As String = "", _
                            Optional ByVal aStreet As String = "", _
                            Optional ByVal aHousenumber As String = "", _
                            Optional ByVal aPostboxnumber As String = "", _
                            Optional ByVal aText As textType = Nothing _
                            ) As address
        Dim tmpAddress As New address

        Dim items As New ArrayList
        Dim ItemsElementNames As New ArrayList
        If Not aCountry Is Nothing Then
            items.Add(aCountry)
            ItemsElementNames.Add(ItemsChoiceType1.country)
        End If
        'If aZip <> "" Then
        items.Add(aZip)
        ItemsElementNames.Add(ItemsChoiceType1.zip)
        'End If
        'If aCity <> "" Then
        items.Add(aCity)
        ItemsElementNames.Add(ItemsChoiceType1.city)
        'End If
        'If aStreet <> "" Then
        items.Add(aStreet)
        ItemsElementNames.Add(ItemsChoiceType1.street)
        'End If
        'If aHousenumber <> "" Then
        items.Add(aHousenumber)
        ItemsElementNames.Add(ItemsChoiceType1.housenumber)
        'End If
        If aPostboxnumber <> "" Then
            items.Add(aPostboxnumber)
            ItemsElementNames.Add(ItemsChoiceType1.postboxnumber)
        End If
        If Not aText Is Nothing Then
            items.Add(aText)
            ItemsElementNames.Add(ItemsChoiceType1.text)
        End If

        tmpAddress.cd = New CDADDRESS() {CDBuilder.CDADDRESS(aAddressTypeCode)}
        tmpAddress.Items = items.ToArray
        tmpAddress.ItemsElementName = DirectCast(ItemsElementNames.ToArray(GetType(ItemsChoiceType1)), ItemsChoiceType1())

        Return tmpAddress
    End Function

    Public Shared Function telecom(ByVal aTelecomTypeCode As String, ByVal aTelecomnumber As String) As telecom
        Dim tmpTelecom As New telecom

        tmpTelecom.cd = New CDTELECOM() {CDBuilder.CDTELECOM(aTelecomTypeCode)}
        tmpTelecom.telecomnumber = aTelecomnumber

        Return tmpTelecom
    End Function

    Public Shared Function hcpartyType(ByVal aHCParty As String, _
                                ByVal aCDHCPARTYvalue As CDHCPARTYvalues, _
                                Optional ByVal aName As String = "", _
                                Optional ByVal aFirstname As String = "", _
                                Optional ByVal aFamilyname As String = "", _
                                Optional ByVal aAddress As address = Nothing, _
                                Optional ByVal aTelecom As telecom = Nothing) As hcpartyType
        Dim tmpHcpartyType As New hcpartyType

        Dim items As New ArrayList
        Dim ItemsElementNames As New ArrayList
        If aName <> "" Then
            items.Add(aName)
            ItemsElementNames.Add(ItemsChoiceType.name)
        End If
        If aFirstname <> "" Then
            items.Add(aFirstname)
            ItemsElementNames.Add(ItemsChoiceType.firstname)
        End If
        If aFamilyname <> "" Then
            items.Add(aFamilyname)
            ItemsElementNames.Add(ItemsChoiceType.familyname)
        End If

        tmpHcpartyType.id = New IDHCPARTY() {IDBuilder.IDHCPARTY(aHCParty)}
        tmpHcpartyType.cd = New CDHCPARTY() {CDBuilder.CDHCPARTY(aCDHCPARTYvalue)}
        tmpHcpartyType.Items = DirectCast(items.ToArray(GetType(String)), String())
        tmpHcpartyType.ItemsElementName = DirectCast(ItemsElementNames.ToArray(GetType(ItemsChoiceType)), ItemsChoiceType())

        If Not aAddress Is Nothing Then
            tmpHcpartyType.address = New address() {aAddress}
        End If
        If Not aTelecom Is Nothing Then
            tmpHcpartyType.telecom = New telecom() {aTelecom}
        End If

        Return tmpHcpartyType
    End Function

    Public Shared Function acknowledgment(ByVal aValue As CDACKNOWLEDGMENTvalues) As acknowledgment
        Dim tmpAcknowledgment As New acknowledgment

        tmpAcknowledgment.cd = CDBuilder.CDACKNOWLEDGMENT(aValue)

        Return tmpAcknowledgment
    End Function

    Public Shared Function urgency(ByVal aValue As CDURGENCYvalues) As urgency
        Dim tmpUrgency As New urgency

        tmpUrgency.cd = CDBuilder.CDURGENCY(aValue)

        Return tmpUrgency
    End Function

    Public Shared Function header(ByVal aSender As hcpartyType, _
                           ByVal aRecipient As hcpartyType, _
                           Optional ByVal aUrgency As urgency = Nothing, _
                           Optional ByVal aAcknowledgment As acknowledgment = Nothing, _
                           Optional ByVal aComment As textType = Nothing) As header
        Dim tmpHeader As New header
        Dim creationDate As Date = Date.Now

        tmpHeader = New KMEHR.header
        tmpHeader.standard = standard()
        tmpHeader.id = New IDKMEHR() {IDBuilder.IDKMEHR(creationDate)}
        tmpHeader.date = creationDate.Date
        'tmpHeader.time = tmpHeader.time.AddHours(creationDate.Hour).AddMinutes(creationDate.Minute)
        tmpHeader.time = timeToKmehrTimeString(creationDate)
        tmpHeader.sender = New hcpartyType() {aSender}
        tmpHeader.id(0).Value = tmpHeader.sender(0).id(0).Value & "." & tmpHeader.id(0).Value
        tmpHeader.recipient = New hcpartyType() {aRecipient}
        tmpHeader.acknowledgment = aAcknowledgment
        tmpHeader.urgency = aUrgency
        If Not aComment Is Nothing Then
            tmpHeader.text = New textType() {aComment}
        End If

        Return tmpHeader
    End Function

    Public Shared Function sex(ByVal aValue As CDSEXvalues) As sex
        Dim tmpSex As New sex

        tmpSex.cd = CDBuilder.CDSEX(aValue)

        Return tmpSex
    End Function

    Public Shared Function personType(ByVal aID As String, _
                               ByVal aFirstname As String, _
                               ByVal aFamilyname As String, _
                               ByVal aSex As CDSEXvalues, _
                               Optional ByVal aBirthdate As birthdate = Nothing, _
                               Optional ByVal aAddress As address = Nothing, _
                               Optional ByVal aTelecom As telecom = Nothing, _
                               Optional ByVal aUsuallanguage As String = Nothing) As personType
        Dim tmpPersonType As New personType

        tmpPersonType.id = New IDPATIENT() {IDBuilder.IDPATIENT(aID)}
        tmpPersonType.firstname = aFirstname
        tmpPersonType.familyname = aFamilyname
        tmpPersonType.sex = sex(aSex)
        tmpPersonType.birthdate = aBirthdate
        If Not aAddress Is Nothing Then
            tmpPersonType.address = New address() {aAddress}
        End If
        If Not aTelecom Is Nothing Then
            tmpPersonType.telecom = New telecom() {aTelecom}
        End If
        tmpPersonType.usuallanguage = aUsuallanguage

        Return tmpPersonType
    End Function

    Public Shared Function content() As content
        Dim tmpContent As New content
        tmpContent.Items = Nothing
        tmpContent.ItemsElementName = Nothing
        Return tmpContent
    End Function

    Public Shared Function item(ByVal aType As CDITEMvalues, ByVal aContent As content) As item
        Dim tmpItem As New item
        tmpItem.id = New IDKMEHR() {IDBuilder.IDKMEHR(0)}
        tmpItem.cd = New CDITEM() {CDBuilder.CDITEM(aType)}
        tmpItem.content = New content() {aContent}

        Return tmpItem
    End Function

    Public Shared Function item(ByVal aSystem As String, ByVal aVersion As String, ByVal aType As String, ByVal aContent As content) As item
        Dim tmpItem As New item
        tmpItem.id = New IDKMEHR() {IDBuilder.IDKMEHR(0)}
        tmpItem.cd = New CDITEM() {CDBuilder.CDITEM(aSystem, aVersion, aType)}
        tmpItem.content = New content() {aContent}

        Return tmpItem
    End Function


    Public Shared Function transaction(ByVal aTransactionTypeCode As CDTRANSACTIONvalues, _
                                       ByVal aTransactionTs As Date, _
                                       ByVal aAuthor As hcpartyType, _
                                       ByVal iscomplete As Boolean, _
                                       ByVal isvalidated As Boolean) As transaction
        Dim tmpTransaction As New transaction

        tmpTransaction.cd = New CDTRANSACTION() {CDBuilder.CDTRANSACTION(aTransactionTypeCode)}
        tmpTransaction.id = New IDKMEHR() {IDBuilder.IDKMEHR(0)}
        tmpTransaction.date = aTransactionTs.Date
        'tmpTransaction.time = tmpTransaction.time.AddHours(aTransactionTs.Hour).AddMinutes(aTransactionTs.Minute)
        tmpTransaction.time = timeToKmehrTimeString(aTransactionTs)
        tmpTransaction.iscomplete = iscomplete
        tmpTransaction.isvalidated = isvalidated
        tmpTransaction.author = New hcpartyType() {aAuthor}

        Return tmpTransaction
    End Function

    Public Shared Function folder(ByVal aPatient As personType, _
                           Optional ByVal aTransaction As transaction = Nothing, _
                           Optional ByVal aComment As textType = Nothing) As folder
        Dim tmpFolder As New folder

        tmpFolder.id = New IDKMEHR() {IDBuilder.IDKMEHR(0)}
        tmpFolder.patient = aPatient
        If aTransaction Is Nothing Then
            tmpFolder.transaction = Nothing
        Else
            tmpFolder.transaction = New transaction() {aTransaction}
            aTransaction.id(0).Value = 1.ToString
        End If
        If Not aComment Is Nothing Then
            tmpFolder.text = New textType() {aComment}
        End If

        Return tmpFolder
    End Function

    Public Shared Function message(ByVal aHeader As header, Optional ByVal aFolder As folder = Nothing) As kmehrmessage
        Dim tmpMessage As New KMEHR.kmehrmessage

        tmpMessage.header = aHeader

        If aFolder Is Nothing Then
            tmpMessage.folder = Nothing
        Else
            tmpMessage.folder = New folder() {aFolder}
            aFolder.id(0).Value = 1.ToString
        End If

        Return tmpMessage
    End Function

    Public Shared Sub add(ByVal aPersonType As personType, ByVal aApplication As String, ByVal aVersion As String, ByVal aID As String)
        Dim tmpLength As Integer = aPersonType.id.Length
        ReDim Preserve aPersonType.id(tmpLength)
        aPersonType.id(tmpLength) = IDBuilder.IDPATIENT(aApplication, aVersion, aID)
    End Sub

    Public Shared Sub addSender(ByVal aHeader As header, ByVal aSender As hcpartyType)
        Dim tmpLength As Integer = aHeader.sender.Length
        ReDim Preserve aHeader.sender(tmpLength)
        aHeader.sender(tmpLength) = aSender
    End Sub

    Public Shared Sub addRecipient(ByVal aHeader As header, ByVal aRecipient As hcpartyType)
        Dim tmpLength As Integer = aHeader.recipient.Length
        ReDim Preserve aHeader.recipient(tmpLength)
        aHeader.recipient(tmpLength) = aRecipient
    End Sub

    Public Shared Sub add(ByVal aMessage As kmehrmessage, ByVal aFolder As folder)
        If aMessage.folder Is Nothing Then
            aMessage.folder = New folder() {aFolder}
            aFolder.id(0).Value = 1.ToString
        Else
            Dim tmpLength As Integer = aMessage.folder.Length
            ReDim Preserve aMessage.folder(tmpLength)
            aMessage.folder(tmpLength) = aFolder
            aFolder.id(0).Value = (tmpLength + 1).ToString
        End If
    End Sub

    Public Shared Sub add(ByVal aFolder As folder, ByVal aTransaction As transaction)
        If aFolder.transaction Is Nothing Then
            aFolder.transaction = New transaction() {aTransaction}
            aTransaction.id(0).Value = 1.ToString
        Else
            Dim tmpLength As Integer = aFolder.transaction.Length
            ReDim Preserve aFolder.transaction(tmpLength)
            aFolder.transaction(tmpLength) = aTransaction
            aTransaction.id(0).Value = (tmpLength + 1).ToString
        End If
    End Sub

    Public Shared Sub add(ByVal aTransaction As transaction, ByVal aSystem As String, ByVal aSystemVersion As String, ByVal aType As String)
        Dim tmpLength As Integer = aTransaction.cd.Length
        ReDim Preserve aTransaction.cd(tmpLength)
        aTransaction.cd(tmpLength) = CDBuilder.CDTRANSACTION(aSystem, aSystemVersion, aType)
    End Sub

    Public Shared Sub addID(ByVal aTransaction As transaction, ByVal aSystem As String, ByVal aSystemVersion As String, ByVal aType As String)
        Dim tmpLength As Integer = aTransaction.id.Length
        ReDim Preserve aTransaction.id(tmpLength)
        aTransaction.id(tmpLength) = IDBuilder.IDKMEHR(aSystem, aSystemVersion, aType)
    End Sub

    Public Shared Sub add(ByVal aTransaction As transaction, ByVal aTexttype As textType)
        add(aTransaction, aTexttype, ItemChoiceType3.text)
    End Sub

    Public Shared Sub add(ByVal aTransaction As transaction, ByVal aLnkType As lnkType)
        add(aTransaction, aLnkType, ItemChoiceType3.lnk)
    End Sub

    Public Shared Sub add(ByVal aTransaction As transaction, ByVal aAuthor As hcpartyType)
        Dim tmpLength As Integer = aTransaction.author.Length
        ReDim Preserve aTransaction.author(tmpLength)
        aTransaction.author(tmpLength) = aAuthor
    End Sub

    Public Shared Sub add(ByVal aTransaction As transaction, ByVal aItem As item)
        add(aTransaction, aItem, ItemChoiceType3.item)
        aItem.id(0).Value = aTransaction.Items.Length.ToString
    End Sub

    Public Shared Sub add(ByVal aTransaction As transaction, ByVal aHeading As heading)
        add(aTransaction, aHeading, ItemChoiceType3.heading)
        aHeading.id(0).Value = aTransaction.Items.Length.ToString
    End Sub

    Public Shared Sub add(ByVal aHCPartyType As hcpartyType, ByVal aHCParty As String, ByVal aSystem As String, ByVal aSystemVersion As String)
        Dim tmpLength As Integer = aHCPartyType.id.Length
        ReDim Preserve aHCPartyType.id(tmpLength)
        aHCPartyType.id(tmpLength) = IDBuilder.IDHCPARTY(aSystem, aSystemVersion, aHCParty)
    End Sub

    Public Shared Sub addID(ByVal aContent As content, ByVal aSystem As String, ByVal aSystemVersion As String, ByVal aID As String)
        add(aContent, IDBuilder.IDKMEHR(aSystem, aSystemVersion, aID), ItemsChoiceType2.id)
    End Sub

    Public Shared Sub addCD(ByVal aContent As content, ByVal aType As CDCONTENTschemes, ByVal aValue As String)
        add(aContent, CDBuilder.CDCONTENT(aType, aValue), ItemsChoiceType2.cd)
    End Sub

    Public Shared Sub addCD(ByVal aContent As content, ByVal aSystem As String, ByVal aSystemVersion As String, ByVal aValue As String)
        add(aContent, CDBuilder.CDCONTENT(aSystem, aSystemVersion, aValue), ItemsChoiceType2.cd)
    End Sub

    Public Shared Sub addCD(ByVal aHCParty As hcpartyType, ByVal aSystem As String, ByVal aSystemVersion As String, ByVal aValue As String)
        Dim tmpLength As Integer = aHCParty.cd.Length
        ReDim Preserve aHCParty.cd(tmpLength)
        aHCParty.cd(tmpLength) = CDBuilder.CDHCPARTY(aSystem, aSystemVersion, aValue)
    End Sub

    Public Shared Sub addDate(ByVal aContent As content, ByVal aDate As Date)
        add(aContent, aDate.Date, ItemsChoiceType2.date)
    End Sub

    Public Shared Sub addTime(ByVal aContent As content, ByVal aDate As Date)
        add(aContent, aDate.ToString("HH:mm:ss"), ItemsChoiceType2.time)
    End Sub

    Public Shared Sub addHCParty(ByVal aContent As content, ByVal aHCParty As hcpartyType)
        add(aContent, aHCParty, ItemsChoiceType2.hcparty)
    End Sub

    Public Shared Sub addText(ByVal aContent As content, ByVal aText As textType)
        add(aContent, aText, ItemsChoiceType2.text)
    End Sub

    Public Shared Sub addLink(ByVal aContent As content, ByVal aLink As lnkType)
        add(aContent, aLink, ItemsChoiceType2.lnk)
    End Sub

    Public Shared Sub add(ByVal aItem As item, ByVal aSystem As String, ByVal aVersion As String, ByVal aType As String)
        Dim tmpLength As Integer = aItem.cd.Length
        ReDim Preserve aItem.cd(tmpLength)
        aItem.cd(tmpLength) = CDBuilder.CDITEM(aSystem, aVersion, aType)
    End Sub

    Private Shared Sub add(ByVal aTransaction As transaction, ByVal aItem As Object, ByVal aItemType As ItemChoiceType3)
        If aTransaction.Items Is Nothing Then
            aTransaction.Items = New Object() {aItem}
            aTransaction.ItemsElementName = New ItemChoiceType3() {aItemType}
        Else
            Dim tmpLength As Integer = aTransaction.Items.Length
            ReDim Preserve aTransaction.Items(tmpLength)
            ReDim Preserve aTransaction.ItemsElementName(tmpLength)
            aTransaction.Items(tmpLength) = aItem
            aTransaction.ItemsElementName(tmpLength) = aItemType
        End If
    End Sub

    Private Shared Sub add(ByVal aContent As content, ByVal aItem As Object, ByVal aItemType As ItemsChoiceType2)
        If aContent.Items Is Nothing Then
            aContent.Items = New Object() {aItem}
            aContent.ItemsElementName = New ItemsChoiceType2() {aItemType}
        Else
            Dim tmpLength As Integer = aContent.Items.Length
            ReDim Preserve aContent.Items(tmpLength)
            ReDim Preserve aContent.ItemsElementName(tmpLength)
            aContent.Items(tmpLength) = aItem
            aContent.ItemsElementName(tmpLength) = aItemType
        End If
    End Sub

    Private Shared Function timeToKmehrTimeString(ByVal aTime As DateTime) As String
        Return aTime.Hour.ToString.PadLeft(2, "0"c) & ":" & aTime.Minute.ToString.PadLeft(2, "0"c) & ":" & aTime.Second.ToString.PadLeft(2, "0"c)
    End Function

    Public Sub New()

    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class
