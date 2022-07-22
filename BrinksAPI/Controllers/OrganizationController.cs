﻿using BrinksAPI.Auth;
using BrinksAPI.Helpers;
using BrinksAPI.Interfaces;
using BrinksAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NativeOrganization;
using NativeRequest;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace BrinksAPI.Controllers
{
    [Authorize]
    public class OrganizationController : Controller
    {
        private readonly IConfigManager _configuration;
        private readonly ApplicationDbContext _context;

        public OrganizationController(IConfigManager configuaration,ApplicationDbContext context)
        {
            _configuration = configuaration;
            _context = context;
        }


        #region UPSERT ORGANIZATION
        /// <summary>
        /// Creates or Updates an Organization.
        /// </summary>
        /// <param name="organization"></param>
        /// <returns>Organization</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/organization/
        ///		{
        ///		   "requestId":"12345678",
        ///        "RiskCodeDescription": "CR1",
        ///        "name": "CENGLOBAL",
        ///        "address1": "Office 15",
        ///        "address2": "15th Floor",
        ///        "address3": "Buisness Tower",
        ///        "address4": "Sheik Zayed Road",
        ///        "city": "Burjman",
        ///        "provinceCode": "DU",
        ///        "postalCode": "784132",
        ///        "countryCode": "AE",
        ///        "phoneNumber": "971524444444",
        ///        "mobileNumber": "971524444444",
        ///        "faxNumber": "971524444444",
        ///        "emailAddress": "user@cenglobal.com",
        ///        "arAccountNumber": "123",
        ///        "apAccountNumber":"123",
        ///        "preferredCurrency": "AED",
        ///        "billingAttention": "Contact01",
        ///        "dateCreated": "2022-05-24 06:35:44",
        ///        "notes": "Note 01",
        ///        "invoiceType":"C",
        ///        "siteCode": "3210",
        ///        "globalCustomerCode": "3737EMIRATES0014",
        ///        "invoiceGlobalCustomerCode": "1234567890",
        ///        "brokerGlobalCustomerCode":"BROKER",
        ///        "taxId": "123456789",
        ///        "creditRiskNotes": "This is credit risk Note",
        ///        "knownShipper":"Y",
        ///        "customerGroupCode": "12345678",
        ///        "tsaValidationId":"00002-56",
        ///        "tsaDate":"2022-02-01 10:00:00",
        ///        "tsaType":"tsa Type",
        ///        "locationVerifiedDate": "2022-02-01 10:00:00",
        ///        "electronicInvoice": "N",
        ///        "addressValidatedFlag": "Y",
        ///        "accountOwner": "Contact02",
        ///        "einvoiceEmailAddress": "contact02@cenglobal.com",
        ///        "globalEntityName": "Global Entity Name",
        ///        "kycCreatedPrior2018": "Y",
        ///        "kycOpenProcCompleted": "Y",
        ///        "kycRefNbr": "12345678",
        ///        "kycVerifDate": "2022-05-24T06:35:44.248Z",
        ///        "kycApprovedBy": "KycApproved",
        ///        "kycOpeningStation": "kycStation",
        ///        "lob": "lob",
        ///        "allowCollect": "Y",
        ///        "adyenPay": "Y",
        ///        "adyenPayPreference": "P",
        ///        "adyenTokenId": "1234",
        ///        "adyenPayByLinkId": "12345"
        ///		}
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Route("api/organization")]
        public ActionResult<OrganizationResponse> UpsertOrganization([FromBody] Organization organization)
        {
            OrganizationResponse dataResponse = new OrganizationResponse();
            string successMessage = "";
            try
            {
                dataResponse.RequestId = organization?.requestId;
                if (!ModelState.IsValid)
                {
                    string errorString = "";
                    var errors = ModelState.Select(x => x.Value.Errors)
                         .Where(y => y.Count > 0)
                         .ToList();
                    foreach(var error in errors)
                    {
                        foreach(var subError in error)
                        {
                            errorString += String.Format("{0}", subError.ErrorMessage);
                        }
                    }
                    dataResponse.Status = "ERROR";
                    dataResponse.Message = errorString;

                    return Ok(dataResponse);
                }

                if (organization.billingAttention != null && organization.accountOwner !=null && organization.billingAttention == organization.accountOwner)
                {
                    dataResponse.Status = "ERROR";
                    dataResponse.Message = "Billing Attention and Account Owner field are same. Please check";
                    return Ok(dataResponse);
                }
                if (organization.globalCustomerCode == null)
                {
                    dataResponse.Status = "ERROR";
                    dataResponse.Message = String.Format("{0} Global customer code cannot be empty", organization.globalCustomerCode);
                    return Ok(dataResponse);
                }
                NativeOrganization.Native native = new NativeOrganization.Native();
                #region HEADER
                NativeHeader header = new NativeHeader();
                NativeOrganization.DataContext dataContext = new NativeOrganization.DataContext();
                NativeOrganization.Staff staff = new NativeOrganization.Staff();
                staff.Code = organization.userId;
                dataContext.EventUser = staff;
                header.DataContext = dataContext;
                native.Header = header;
                #endregion

                NativeOrganization.NativeBody body = new NativeOrganization.NativeBody();

                OrganizationData organizationData = SearchOrgWithRegNo(organization.globalCustomerCode);
                if (organizationData.OrgHeader == null)
                {
                    #region INSERT
                    NativeOrganization.NativeOrganization nativeOrganization = new NativeOrganization.NativeOrganization();
                    nativeOrganization.ActionSpecified = true;
                    nativeOrganization.Action = NativeOrganization.Action.INSERT;
                    nativeOrganization.FullName = organization.name;
                    nativeOrganization.Language = "EN";

                    //DEFAULT SITE ID
                    Entities.Site site = new Entities.Site();
                    if(organization.countryCode != null)
                        site = _context.sites.Where(s=>s.Country == organization.countryCode).FirstOrDefault();
                    if (site == null)
                    {
                        site.Airport = "DXB";
                        site.Country = "AE";
                        site.CompanyCode = "DXB";
                    }

                    #region CLOSEST PORT
                    if (organization.siteCode != null)
                    {
                        int siteCode = Int32.Parse(organization.siteCode);
                        site = _context.sites.Where(s => s.SiteCode == siteCode)?.FirstOrDefault();
                        if (site == null)
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "siteCode " + organization.siteCode + " is not a valid mapping in the DB.";
                            return Ok(dataResponse);
                        }
                        string unloco = site.Country + site.Airport;
                        string pk = SearchUNLOCOCode(unloco);
                        if (pk == null)
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "siteCode = " + organization.siteCode + " UNLOCO = " + unloco + " is not a valid UNLOCO Code";
                            return Ok(dataResponse);
                        }
                        NativeOrganizationClosestPort nativeOrganizationClosestPort = new NativeOrganizationClosestPort();
                        nativeOrganizationClosestPort.TableName = "RefUNLOCO";
                        nativeOrganizationClosestPort.Code = unloco;
                        nativeOrganization.ClosestPort = nativeOrganizationClosestPort;
                    }
                    else
                    {
                        NativeOrganizationClosestPort nativeOrganizationClosestPort = new NativeOrganizationClosestPort();
                        nativeOrganizationClosestPort.TableName = "RefUNLOCO";
                        nativeOrganizationClosestPort.Code = site.Country + site.Airport;
                        nativeOrganization.ClosestPort = nativeOrganizationClosestPort;
                    }

                    #endregion
                    
                    #region CONSIGNOR OR CONSIGNEE
                    if (organization.knownShipper == YesOrNo.Y)
                    {
                        nativeOrganization.IsConsigneeSpecified = true;
                        nativeOrganization.IsConsignee = true;
                    }

                    if (organization.allowCollect == YesOrNo.Y)
                    {
                        nativeOrganization.IsConsignorSpecified = true;
                        nativeOrganization.IsConsignor = true;
                    }
                    #endregion

                    #region ORGANIZATION COUNTRY DATA TSA
                    if (organization.tsaValidationId != null && organization.tsaDate != null)
                    {
                        List<NativeOrganizationOrgCountryData> orgCountryDatas = new List<NativeOrganizationOrgCountryData>();
                        NativeOrganizationOrgCountryData orgCountryData = new NativeOrganizationOrgCountryData();
                        orgCountryData.ActionSpecified = true;
                        orgCountryData.Action = NativeOrganization.Action.INSERT;
                        orgCountryData.EXApprovedOrMajorExporter = "KC";
                        orgCountryData.EXApprovalNumber = organization.tsaValidationId;
                        orgCountryData.EXExportPermissionDetails = organization.tsaType;
                        orgCountryData.EXApprovalExpiryDate = DateTime.ParseExact(organization.tsaDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTmm:ss:ff");
                        NativeOrganizationOrgCountryDataApprovedLocation location = new NativeOrganizationOrgCountryDataApprovedLocation();
                        location.TableName = "OrgAddress";
                        location.Code = organization.address1;
                        orgCountryData.ApprovedLocation = location;
                        orgCountryDatas.Add(orgCountryData);
                        nativeOrganization.OrgCountryDataCollection = orgCountryDatas.ToArray();
                    }
                    #endregion

                    #region ORGANIZATION COMPANY DATA
                    List<NativeOrganizationOrgCompanyData> orgCompanyDatas = new List<NativeOrganizationOrgCompanyData>();
                    NativeOrganizationOrgCompanyData orgCompanyData = new NativeOrganizationOrgCompanyData();
                    orgCompanyData.ActionSpecified = true;
                    orgCompanyData.Action = NativeOrganization.Action.INSERT;
                    NativeOrganizationOrgCompanyDataGlbCompany company = new NativeOrganizationOrgCompanyDataGlbCompany();
                    company.Code = site.CompanyCode;
                    orgCompanyData.GlbCompany = company;
                    if (organization.arAccountNumber != null)
                    {
                        orgCompanyData.IsDebtorSpecified = true;
                        orgCompanyData.IsDebtor = true;
                        orgCompanyData.ARExternalDebtorCode = organization.arAccountNumber;
                        if (organization.RiskCodeDescription != null)
                        {
                            var riskCodeDescription = _context.riskCodeDescriptions.Where(r=>r.BrinksCode == organization.RiskCodeDescription).FirstOrDefault();
                            if (riskCodeDescription == null)
                            {
                                dataResponse.Status = "ERROR";
                                dataResponse.Message = "Not a valid Risk Code Description " + organization.RiskCodeDescription;
                                return Ok(dataResponse);
                            }
                            orgCompanyData.ARCreditRating = riskCodeDescription.CWCode;
                        }

                        if (organization.invoiceType != null)
                        {
                            List<NativeOrganizationOrgCompanyDataOrgInvoiceType> invoiceTypes = new List<NativeOrganizationOrgCompanyDataOrgInvoiceType>();
                            NativeOrganizationOrgCompanyDataOrgInvoiceType invoiceType = new NativeOrganizationOrgCompanyDataOrgInvoiceType();
                            invoiceType.ActionSpecified = true;
                            invoiceType.Action = NativeOrganization.Action.INSERT;
                            invoiceType.Module = "ALL";
                            invoiceType.ServiceDirection = "ALL";
                            invoiceType.TransportMode = "ALL";
                            invoiceType.Interval = "MTH";
                            invoiceType.StartDay = "LMH";
                            string? invoiceTypeString = (organization.invoiceType == InvoiceTypes.C) ? "INV" : "CHG";
                            invoiceType.Type = invoiceTypeString;
                            invoiceType.SecondaryType = invoiceTypeString;
                            invoiceTypes.Add(invoiceType);
                            orgCompanyData.OrgInvoiceTypeCollection = invoiceTypes.ToArray();
                        }

                        NativeOrganizationOrgCompanyDataARDDefltCurrency arDefltCurrency = new NativeOrganizationOrgCompanyDataARDDefltCurrency();
                        arDefltCurrency.TableName = "RefCurrency";
                        arDefltCurrency.Code = organization.preferredCurrency;
                        orgCompanyData.ARDDefltCurrency = arDefltCurrency;

                        NativeOrganizationOrgCompanyDataARDebtorGroup debtorGroup = new NativeOrganizationOrgCompanyDataARDebtorGroup();
                        debtorGroup.TableName = "OrgDebtorGroup";
                        debtorGroup.Code = "TPY";
                        orgCompanyData.ARDebtorGroup = debtorGroup;
                    }
                    if (organization.apAccountNumber != null)
                    {
                        orgCompanyData.IsCreditorSpecified = true;
                        orgCompanyData.IsCreditor = true;
                        orgCompanyData.APExternalCreditorCode = organization.apAccountNumber;
                        NativeOrganizationOrgCompanyDataAPDefltCurrency apDefltCurrency = new NativeOrganizationOrgCompanyDataAPDefltCurrency();
                        apDefltCurrency.TableName = "RefCurrency";
                        apDefltCurrency.Code = organization.preferredCurrency;
                        orgCompanyData.APDefltCurrency = apDefltCurrency;

                        NativeOrganizationOrgCompanyDataAPCreditorGroup creditorGroup = new NativeOrganizationOrgCompanyDataAPCreditorGroup();
                        creditorGroup.TableName = "OrgCreditorGroup";
                        creditorGroup.Code = "TPY";
                        orgCompanyData.APCreditorGroup = creditorGroup;
                    }
                    orgCompanyDatas.Add(orgCompanyData);
                    nativeOrganization.OrgCompanyDataCollection = orgCompanyDatas.ToArray();
                    #endregion

                    #region CONTACTS
                    List<NativeOrganizationOrgContact> contacts = new List<NativeOrganizationOrgContact>();

                    if (organization.billingAttention != null)
                    {
                        NativeOrganizationOrgContact billingContact = new NativeOrganizationOrgContact();
                        billingContact.ActionSpecified = true;
                        billingContact.Action = NativeOrganization.Action.INSERT;
                        billingContact.Language = "EN";
                        billingContact.Title = "Billing Attention";
                        billingContact.NotifyMode = "DND";
                        billingContact.ContactName = organization.billingAttention;
                        contacts.Add(billingContact);
                    }
                    if (organization.accountOwner != null)
                    {
                        NativeOrganizationOrgContact ownerContact = new NativeOrganizationOrgContact();
                        ownerContact.ActionSpecified = true;
                        ownerContact.Action = NativeOrganization.Action.INSERT;
                        ownerContact.Language = "EN";
                        ownerContact.Title = "Owner Contact";
                        ownerContact.NotifyMode = "DND";
                        ownerContact.ContactName = organization.accountOwner;
                        ownerContact.Email = organization.einvoiceEmailAddress;
                        contacts.Add(ownerContact);
                    }
                    nativeOrganization.OrgContactCollection = contacts.ToArray();
                    #endregion

                    #region RESGISTRATION
                    List<NativeOrganizationOrgCusCode> registrationCusCodes = new List<NativeOrganizationOrgCusCode>();
                    NativeOrganizationOrgCusCodeCodeCountry cusCodeCountry = new NativeOrganizationOrgCusCodeCodeCountry();
                    cusCodeCountry.Code = organization.countryCode;

                    NativeOrganizationOrgCusCode globalCustomerRegistrationCusCode = new NativeOrganizationOrgCusCode();
                    globalCustomerRegistrationCusCode.ActionSpecified = true;
                    globalCustomerRegistrationCusCode.Action = NativeOrganization.Action.INSERT;
                    globalCustomerRegistrationCusCode.CustomsRegNo = organization.globalCustomerCode;
                    globalCustomerRegistrationCusCode.CodeType = "LSC";
                    globalCustomerRegistrationCusCode.CodeCountry = cusCodeCountry;
                    registrationCusCodes.Add(globalCustomerRegistrationCusCode);

                    if (organization.taxId != null)
                    {
                        NativeOrganizationOrgCusCode taxRegistrationCusCode = new NativeOrganizationOrgCusCode();
                        taxRegistrationCusCode.ActionSpecified = true;
                        taxRegistrationCusCode.Action = NativeOrganization.Action.INSERT;
                        taxRegistrationCusCode.CustomsRegNo = organization.taxId;
                        taxRegistrationCusCode.CodeType = "VAT";
                        taxRegistrationCusCode.CodeCountry = cusCodeCountry;
                        registrationCusCodes.Add(taxRegistrationCusCode);
                    }

                    nativeOrganization.OrgCusCodeCollection = registrationCusCodes.ToArray();
                    #endregion

                    #region RELATED PARTIES
                    List<NativeOrganizationOrgRelatedParty> relatedParties = new List<NativeOrganizationOrgRelatedParty>();
                    if (organization.invoiceGlobalCustomerCode != null)
                    {
                        OrganizationData invoiceOrganizationData = SearchOrgWithRegNo(organization.invoiceGlobalCustomerCode);
                        if (invoiceOrganizationData.OrgHeader != null)
                        {
                            NativeOrganizationOrgRelatedParty relatedParty = new NativeOrganizationOrgRelatedParty();
                            relatedParty.ActionSpecified = true;
                            relatedParty.Action = NativeOrganization.Action.INSERT;
                            relatedParty.PartyType = "LFW";
                            relatedParty.FreightTransportMode = "ALL";
                            relatedParty.FreightDirection = "PAD";
                            NativeOrganizationOrgRelatedPartyRelatedParty relatedPartyCode = new NativeOrganizationOrgRelatedPartyRelatedParty();
                            relatedPartyCode.PK = invoiceOrganizationData.OrgHeader.PK;
                            relatedPartyCode.Code = organization.invoiceGlobalCustomerCode;
                            relatedParty.RelatedParty = relatedPartyCode;
                            relatedParties.Add(relatedParty);
                            //nativeOrganization.OrgRelatedPartyCollection = relatedParties.ToArray();
                        }
                        else
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "The Invoice global customer code " + organization.invoiceGlobalCustomerCode + " not found in CW.";
                            return Ok(dataResponse);
                        }


                    }
                    if (organization.brokerGlobalCustomerCode != null)
                    {
                        OrganizationData brokerOrganizationData = SearchOrgWithRegNo(organization.brokerGlobalCustomerCode);
                        if (brokerOrganizationData.OrgHeader != null)
                        {
                            //List<NativeOrganizationOrgRelatedParty> relatedParties = new List<NativeOrganizationOrgRelatedParty>();
                            NativeOrganizationOrgRelatedParty relatedParty = new NativeOrganizationOrgRelatedParty();
                            relatedParty.ActionSpecified = true;
                            relatedParty.Action = NativeOrganization.Action.INSERT;
                            relatedParty.PartyType = "CAB";
                            relatedParty.FreightTransportMode = "ALL";
                            relatedParty.FreightDirection = "PAD";
                            NativeOrganizationOrgRelatedPartyRelatedParty relatedPartyCode = new NativeOrganizationOrgRelatedPartyRelatedParty();
                            relatedPartyCode.PK = brokerOrganizationData.OrgHeader.PK;
                            relatedPartyCode.Code = organization.brokerGlobalCustomerCode;
                            relatedParty.RelatedParty = relatedPartyCode;
                            relatedParties.Add(relatedParty);
                            //nativeOrganization.OrgRelatedPartyCollection = relatedParties.ToArray();
                        }
                        else
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "The Broker customer code " + organization.brokerGlobalCustomerCode + " not found in CW.";
                            return Ok(dataResponse);
                        }

                    }
                    if(relatedParties.Count > 0)
                        nativeOrganization.OrgRelatedPartyCollection = relatedParties.ToArray();
                    #endregion

                    #region NOTES
                    List<NativeOrganizationStmNote> notes = new List<NativeOrganizationStmNote>();

                    if (organization.notes != null)
                    {
                        NativeOrganizationStmNote note = new NativeOrganizationStmNote();
                        note.ActionSpecified = true;
                        note.Action = NativeOrganization.Action.INSERT;
                        note.NoteContext = "ALL";
                        note.IsCustomDescription = false;
                        note.ForceRead = true;
                        note.NoteType = "PUB";
                        note.Description = "Goods Handling Instructions";
                        note.NoteText = organization.notes;
                        notes.Add(note);
                    }

                    if (organization.creditRiskNotes != null)
                    {
                        NativeOrganizationStmNote creditRiskNote = new NativeOrganizationStmNote();
                        creditRiskNote.ActionSpecified = true;
                        creditRiskNote.Action = NativeOrganization.Action.INSERT;
                        creditRiskNote.NoteContext = "ALL";
                        creditRiskNote.IsCustomDescription = false;
                        creditRiskNote.ForceRead = true;
                        creditRiskNote.NoteType = "PUB";
                        creditRiskNote.Description = "A/R Credit Management Note";
                        creditRiskNote.NoteText = organization.creditRiskNotes;
                        notes.Add(creditRiskNote);
                    }

                    nativeOrganization.StmNoteCollection = notes.ToArray();
                    #endregion

                    #region ORGANIZATION ADDRESS
                    string additionalAddress = organization.address3 + " " + organization.address4;
                    NativeOrganizationOrgAddress nativeOrgAddress = new NativeOrganizationOrgAddress();
                    List<NativeOrganizationOrgAddress> nativeOrgAddresses = new List<NativeOrganizationOrgAddress>();
                    nativeOrgAddress.ActionSpecified = true;
                    nativeOrgAddress.Action = NativeOrganization.Action.INSERT;
                    nativeOrgAddress.IsActiveSpecified = true;
                    nativeOrgAddress.IsActive = true;
                    nativeOrgAddress.Code = organization.address1?.Substring(0, Math.Min(organization.address1.Length, 24));
                    nativeOrgAddress.Address1 = organization.address1;
                    nativeOrgAddress.Address2 = organization.address2;
                    nativeOrgAddress.AdditionalAddressInformation = additionalAddress?.Substring(0, Math.Min(additionalAddress.Length, 50));

                    nativeOrgAddress.City = organization.city;
                    nativeOrgAddress.PostCode = organization.postalCode;
                    nativeOrgAddress.State = organization.provinceCode;
                    NativeOrganizationOrgAddressCountryCode nativeOrgCountryCode = new NativeOrganizationOrgAddressCountryCode();
                    nativeOrgCountryCode.TableName = "RefCountry";
                    nativeOrgCountryCode.Code = organization.countryCode;
                    nativeOrgAddress.CountryCode = nativeOrgCountryCode;
                    NativeOrganizationOrgAddressRelatedPortCode nativeOrgAddressRelatedPortCode = new NativeOrganizationOrgAddressRelatedPortCode();
                    nativeOrgAddressRelatedPortCode.TableName = "RefUNLOCO";
                    nativeOrgAddressRelatedPortCode.Code = site.Country + site.Airport;

                    nativeOrgAddress.Phone = organization.phoneNumber;
                    nativeOrgAddress.Mobile = organization.mobileNumber;
                    nativeOrgAddress.Fax = organization.faxNumber;
                    nativeOrgAddress.Email = organization.emailAddress;
                    nativeOrgAddress.Language = "EN";
                    nativeOrgAddress.FCLEquipmentNeeded = "ANY";
                    nativeOrgAddress.LCLEquipmentNeeded = "ANY";
                    nativeOrgAddress.AIREquipmentNeeded = "ANY";
                    List<NativeOrganizationOrgAddressOrgAddressCapability> nativeOrgAddressCapabilities = new List<NativeOrganizationOrgAddressOrgAddressCapability>();
                    NativeOrganizationOrgAddressOrgAddressCapability nativeOrgAddressCapability = new NativeOrganizationOrgAddressOrgAddressCapability();
                    nativeOrgAddressCapability.ActionSpecified = true;
                    nativeOrgAddressCapability.Action = NativeOrganization.Action.INSERT;
                    nativeOrgAddressCapability.IsMainAddressSpecified = true;
                    nativeOrgAddressCapability.IsMainAddress = true;
                    nativeOrgAddressCapability.AddressType = "OFC";
                    nativeOrgAddressCapabilities.Add(nativeOrgAddressCapability);
                    nativeOrgAddress.OrgAddressCapabilityCollection = nativeOrgAddressCapabilities.ToArray();
                    nativeOrgAddresses.Add(nativeOrgAddress);
                    nativeOrganization.OrgAddressCollection = nativeOrgAddresses.ToArray();
                    #endregion

                    #region CUSTOM VALUES
                    Dictionary<string, string> customValues = new Dictionary<string, string>();
                    //customValues.Add("siteCode", organization?.siteCode);
                    customValues.Add("locationVerifiedDate", organization.locationVerifiedDate);
                    customValues.Add("kycCreatedPrior2018", organization.kycCreatedPrior2018.ToString());
                    customValues.Add("kycOpenProcCompleted", organization.kycOpenProcCompleted.ToString());
                    customValues.Add("kycRefNbr", organization.kycRefNbr);
                    customValues.Add("kycVerifDate", organization.kycVerifDate);
                    customValues.Add("kycApprovedBy", organization.kycApprovedBy);
                    customValues.Add("kycOpeningStation", organization.kycOpeningStation);
                    customValues.Add("Lob", organization.lob);
                    customValues.Add("adyenPay", organization.adyenPay?.ToString());
                    customValues.Add("adyenPayPreference", organization.adyenPayPreference);
                    customValues.Add("adyenTokenId", organization.adyenTokenId);
                    customValues.Add("adyenPayByLinkId", organization.adyenPayByLinkId);
                    customValues = customValues.Where(c => c.Value != null && c.Value != "").ToDictionary(x => x.Key, x => x.Value);
                    List<NativeOrganizationJobRequiredDocument> documents = new List<NativeOrganizationJobRequiredDocument>();
                    int count = 0;
                    foreach (KeyValuePair<string, string> entry in customValues)
                    {

                        NativeOrganizationJobRequiredDocument document = new NativeOrganizationJobRequiredDocument();
                        document.ActionSpecified = true;
                        document.Action = NativeOrganization.Action.INSERT;
                        document.DocCategory = "CSR";
                        document.DocType = "MSC";
                        document.DocUsage = "BRK";
                        document.DocPeriod = "SHP";
                        document.DocumentNotes = entry.Key;
                        document.DocDescription = entry.Value;
                        document.DocNumber = count.ToString();
                        documents.Add(document);
                        count++;
                    }
                    nativeOrganization.JobRequiredDocumentCollection = documents.ToArray();
                    #endregion

                    organizationData.OrgHeader = nativeOrganization;
                    successMessage = "Organization Created Successfully."; 
                    #endregion
                }
                else 
                {
                    #region UPDATE
                    string countryCode = "";
                    if (organization.countryCode != null && organization.countryCode != "")
                        countryCode = organization.countryCode;
                    else
                        countryCode = organizationData.OrgHeader.OrgAddressCollection[0].CountryCode.Code;

                    organizationData.OrgHeader.ActionSpecified = true;
                    organizationData.OrgHeader.Action = NativeOrganization.Action.UPDATE;
                    organizationData.OrgHeader.FullName = organization.name;

                    // DEFAULT UNLOCO VALUES
                    Entities.Site site = new Entities.Site();
                    site.Country = organizationData.OrgHeader.ClosestPort.Code.Substring(0,2);
                    site.Airport = organizationData.OrgHeader.ClosestPort.Code.Substring(2);
                    
                    string ClosestPortPK = organizationData.OrgHeader.ClosestPort.PK;

                    #region CLOSEST PORT
                    if (organization.siteCode != null)
                    {
                        site = _context.sites.Where(s => s.SiteCode == Int32.Parse(organization.siteCode))?.FirstOrDefault();
                        if (site == null)
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "siteCode " + organization.siteCode + " is not a valid mapping in the DB.";
                            return Ok(dataResponse);
                        }
                        string unloco = site.Country + site.Airport;
                        ClosestPortPK = SearchUNLOCOCode(unloco);
                        if (ClosestPortPK == null)
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "siteCode = " + organization.siteCode + " UNLOCO = " + unloco + " is not a valid UNLOCO Code";
                            return Ok(dataResponse);
                        }
                        organizationData.OrgHeader.ClosestPort.ActionSpecified = true;
                        organizationData.OrgHeader.ClosestPort.Action = NativeOrganization.Action.UPDATE;
                        organizationData.OrgHeader.ClosestPort.PK = ClosestPortPK;
                        organizationData.OrgHeader.ClosestPort.Code = unloco;
                    }

                    #endregion

                    #region CONSIGNOR OR CONSIGNEE
                    organizationData.OrgHeader.IsConsigneeSpecified = true;
                    organizationData.OrgHeader.IsConsignorSpecified = true;
                    if (organization.knownShipper == YesOrNo.Y)
                        organizationData.OrgHeader.IsConsignee = true;
                    if (organization.knownShipper == YesOrNo.N)
                        organizationData.OrgHeader.IsConsignee = false;

                    if (organization.allowCollect == YesOrNo.Y)
                        organizationData.OrgHeader.IsConsignor = true;
                    if (organization.allowCollect == YesOrNo.N)
                        organizationData.OrgHeader.IsConsignor = false;
                    #endregion

                    #region ORGANIZATION COUNTRY DATA TSA
                    if (organization.tsaValidationId != null && organization.tsaDate != null)
                    {
                        if (organizationData.OrgHeader.OrgCountryDataCollection is not null)
                        {
                            if (organizationData.OrgHeader.OrgCountryDataCollection.Length > 0)
                            {
                                organizationData.OrgHeader.OrgCountryDataCollection[0].ActionSpecified = true;
                                organizationData.OrgHeader.OrgCountryDataCollection[0].Action = NativeOrganization.Action.UPDATE;
                                organizationData.OrgHeader.OrgCountryDataCollection[0].EXApprovedOrMajorExporter = "KC";
                                organizationData.OrgHeader.OrgCountryDataCollection[0].EXApprovalNumber = organization.tsaValidationId;
                                organizationData.OrgHeader.OrgCountryDataCollection[0].EXExportPermissionDetails = organization.tsaType;
                                organizationData.OrgHeader.OrgCountryDataCollection[0].EXApprovalExpiryDate = DateTime.ParseExact(organization.tsaDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTmm:ss:ff");
                                organizationData.OrgHeader.OrgCountryDataCollection[0].ApprovedLocation.TableName = "OrgAddress";
                                organizationData.OrgHeader.OrgCountryDataCollection[0].ApprovedLocation.Code = organization.address1;
                            }
                        }
                        else
                        {
                            List<NativeOrganizationOrgCountryData> orgCountryDatas = new List<NativeOrganizationOrgCountryData>();
                            NativeOrganizationOrgCountryData orgCountryData = new NativeOrganizationOrgCountryData();
                            orgCountryData.ActionSpecified = true;
                            orgCountryData.Action = NativeOrganization.Action.INSERT;
                            orgCountryData.EXApprovedOrMajorExporter = "KC";
                            orgCountryData.EXApprovalNumber = organization.tsaValidationId;
                            orgCountryData.EXExportPermissionDetails = organization.tsaType;
                            orgCountryData.EXApprovalExpiryDate = DateTime.ParseExact(organization.tsaDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTmm:ss:ff");
                            NativeOrganizationOrgCountryDataApprovedLocation location = new NativeOrganizationOrgCountryDataApprovedLocation();
                            location.TableName = "OrgAddress";
                            location.Code = organization.address1;
                            orgCountryData.ApprovedLocation = location;
                            orgCountryDatas.Add(orgCountryData);
                            organizationData.OrgHeader.OrgCountryDataCollection = orgCountryDatas.ToArray();
                        }

                    }
                    #endregion

                    #region ORGANIZATION COMPANY DATA
                    if (organization.arAccountNumber != null || organization.apAccountNumber != null)
                    {
                        if (organizationData.OrgHeader.OrgCompanyDataCollection is not null)
                        {
                            var filterOrgCompanyData = organizationData.OrgHeader.OrgCompanyDataCollection.FirstOrDefault();
                            if (filterOrgCompanyData != null)
                            {
                                filterOrgCompanyData.ActionSpecified = true;
                                filterOrgCompanyData.Action = NativeOrganization.Action.UPDATE;
                                if(organization.siteCode != null)
                                {
                                    string companyCode = _context.sites.Where(s => s.SiteCode.ToString() == organization.siteCode).FirstOrDefault().CompanyCode;
                                    filterOrgCompanyData.GlbCompany.Code = companyCode;
                                }
                                if (organization.arAccountNumber != null)
                                {
                                    filterOrgCompanyData.IsDebtorSpecified = true;
                                    filterOrgCompanyData.IsDebtor = true;

                                    if (organization.RiskCodeDescription != null)
                                    {
                                        var riskCodeDescription = _context.riskCodeDescriptions.Where(r => r.BrinksCode == organization.RiskCodeDescription).FirstOrDefault();
                                        if (riskCodeDescription == null)
                                        {
                                            dataResponse.Status = "ERROR";
                                            dataResponse.Message = "Not a valid Risk Code Description " + organization.RiskCodeDescription;
                                            return Ok(dataResponse);
                                        }
                                        filterOrgCompanyData.ARCreditRating = riskCodeDescription.CWCode;
                                    }

                                    
                                    if (organization.invoiceType != null)
                                    {
                                        if (filterOrgCompanyData.OrgInvoiceTypeCollection is not null)
                                        {
                                            var filterOrgInvoiceType = filterOrgCompanyData.OrgInvoiceTypeCollection.FirstOrDefault();
                                            if (filterOrgInvoiceType != null)
                                            {
                                                filterOrgInvoiceType.ActionSpecified = true;
                                                filterOrgInvoiceType.Action = NativeOrganization.Action.UPDATE;
                                                string? invoiceTypeString = (organization.invoiceType == InvoiceTypes.C) ? "INV" : "CHG";
                                                filterOrgInvoiceType.Type = invoiceTypeString;
                                                filterOrgInvoiceType.SecondaryType = invoiceTypeString;
                                            }
                                        }
                                    }
                                    filterOrgCompanyData.ARExternalDebtorCode = organization.arAccountNumber;
                                    filterOrgCompanyData.ARDDefltCurrency.Code = organization.preferredCurrency;
                                }
                                if (organization.apAccountNumber != null)
                                {
                                    filterOrgCompanyData.IsCreditorSpecified = true;
                                    filterOrgCompanyData.IsCreditor = true;
                                    filterOrgCompanyData.APExternalCreditorCode = organization.apAccountNumber;
                                    filterOrgCompanyData.APDefltCurrency.Code = organization.preferredCurrency;
                                }
                            }
                            else
                            {
                                List<NativeOrganizationOrgCompanyData> orgCompanyDatas = new List<NativeOrganizationOrgCompanyData>();
                                NativeOrganizationOrgCompanyData orgCompanyData = new NativeOrganizationOrgCompanyData();
                                orgCompanyData.ActionSpecified = true;
                                orgCompanyData.Action = NativeOrganization.Action.INSERT;
                                string companyCode = "";
                                if (organization.siteCode != null)
                                     companyCode = _context.sites.Where(s => s.SiteCode.ToString() == organization.siteCode).FirstOrDefault().CompanyCode;

                                NativeOrganizationOrgCompanyDataGlbCompany company = new NativeOrganizationOrgCompanyDataGlbCompany();
                                company.Code = companyCode!=""?companyCode:"DXB";
                                orgCompanyData.GlbCompany = company;
                                if (organization.arAccountNumber != null)
                                {
                                    if (organization.RiskCodeDescription != null)
                                    {
                                        var riskCodeDescription = _context.riskCodeDescriptions.Where(r => r.BrinksCode == organization.RiskCodeDescription).FirstOrDefault();
                                        if (riskCodeDescription == null)
                                        {
                                            dataResponse.Status = "ERROR";
                                            dataResponse.Message = "Not a valid Risk Code Description " + organization.RiskCodeDescription;
                                            return Ok(dataResponse);
                                        }
                                        orgCompanyData.ARCreditRating = riskCodeDescription.CWCode;
                                    }
                                    if (organization.invoiceType != null)
                                    {
                                        List<NativeOrganizationOrgCompanyDataOrgInvoiceType> invoiceTypes = new List<NativeOrganizationOrgCompanyDataOrgInvoiceType>();
                                        NativeOrganizationOrgCompanyDataOrgInvoiceType invoiceType = new NativeOrganizationOrgCompanyDataOrgInvoiceType();
                                        invoiceType.ActionSpecified = true;
                                        invoiceType.Action = NativeOrganization.Action.INSERT;
                                        invoiceType.Module = "ALL";
                                        invoiceType.ServiceDirection = "ALL";
                                        invoiceType.TransportMode = "ALL";
                                        invoiceType.Interval = "MTH";
                                        invoiceType.StartDay = "LMH";
                                        string? invoiceTypeString = (organization.invoiceType == InvoiceTypes.C) ? "INV" : "CHG";
                                        invoiceType.Type = invoiceTypeString;
                                        invoiceType.SecondaryType = invoiceTypeString;
                                        invoiceTypes.Add(invoiceType);
                                        orgCompanyData.OrgInvoiceTypeCollection = invoiceTypes.ToArray();
                                    }
                                    orgCompanyData.IsDebtorSpecified = true;
                                    orgCompanyData.IsDebtor = true;
                                    orgCompanyData.ARExternalDebtorCode = organization.arAccountNumber;
                                    NativeOrganizationOrgCompanyDataARDDefltCurrency arDefltCurrency = new NativeOrganizationOrgCompanyDataARDDefltCurrency();
                                    arDefltCurrency.TableName = "RefCurrency";
                                    arDefltCurrency.Code = organization.preferredCurrency;
                                    orgCompanyData.ARDDefltCurrency = arDefltCurrency;

                                    NativeOrganizationOrgCompanyDataARDebtorGroup debtorGroup = new NativeOrganizationOrgCompanyDataARDebtorGroup();
                                    debtorGroup.TableName = "OrgDebtorGroup";
                                    debtorGroup.Code = "TPY";
                                    orgCompanyData.ARDebtorGroup = debtorGroup;
                                }
                                if (organization.apAccountNumber != null)
                                {
                                    orgCompanyData.IsCreditorSpecified = true;
                                    orgCompanyData.IsCreditor = true;
                                    orgCompanyData.APExternalCreditorCode = organization.apAccountNumber;
                                    NativeOrganizationOrgCompanyDataAPDefltCurrency apDefltCurrency = new NativeOrganizationOrgCompanyDataAPDefltCurrency();
                                    apDefltCurrency.TableName = "RefCurrency";
                                    apDefltCurrency.Code = organization.preferredCurrency;
                                    orgCompanyData.APDefltCurrency = apDefltCurrency;

                                    NativeOrganizationOrgCompanyDataAPCreditorGroup creditorGroup = new NativeOrganizationOrgCompanyDataAPCreditorGroup();
                                    creditorGroup.TableName = "OrgCreditorGroup";
                                    creditorGroup.Code = "TPY";
                                    orgCompanyData.APCreditorGroup = creditorGroup;
                                }
                                orgCompanyDatas.Add(orgCompanyData);
                                organizationData.OrgHeader.OrgCompanyDataCollection = orgCompanyDatas.ToArray();
                            }
                        }
                    }
                    #endregion

                    #region CONTACTS

                    List<NativeOrganizationOrgContact> contacts = new List<NativeOrganizationOrgContact>();
                    if (organization.billingAttention != null)
                    {
                        NativeOrganizationOrgContact billingContact = new NativeOrganizationOrgContact();
                        if (organizationData.OrgHeader.OrgContactCollection is not null)
                        {
                            var filteredBillingContact = organizationData.OrgHeader.OrgContactCollection.Where(bc => bc.Title == "Billing Attention").FirstOrDefault();
                            if (filteredBillingContact != null)
                            {
                                filteredBillingContact.ActionSpecified = true;
                                filteredBillingContact.Action = NativeOrganization.Action.UPDATE;
                                filteredBillingContact.PK = filteredBillingContact.PK;
                                filteredBillingContact.ContactName = organization.billingAttention;
                                contacts.Add(filteredBillingContact);
                            }
                            else
                            {
                                billingContact.ActionSpecified = true;
                                billingContact.Action = NativeOrganization.Action.INSERT;
                                billingContact.Language = "EN";
                                billingContact.Title = "Billing Attention";
                                billingContact.NotifyMode = "DND";
                                billingContact.ContactName = organization.billingAttention;
                                contacts.Add(billingContact);
                            }
                        }
                        else
                        {
                            billingContact.ActionSpecified = true;
                            billingContact.Action = NativeOrganization.Action.INSERT;
                            billingContact.Language = "EN";
                            billingContact.Title = "Billing Attention";
                            billingContact.NotifyMode = "DND";
                            billingContact.ContactName = organization.billingAttention;
                            contacts.Add(billingContact);
                        }
                    }

                    if (organization.accountOwner != null)
                    {
                        NativeOrganizationOrgContact ownerContact = new NativeOrganizationOrgContact();
                        if (organizationData.OrgHeader.OrgContactCollection is not null)
                        {
                            var filteredOwnerContact = organizationData.OrgHeader.OrgContactCollection.Where(bc => bc.Title == "Owner Contact").FirstOrDefault();
                            if (filteredOwnerContact != null)
                            {
                                filteredOwnerContact.ActionSpecified = true;
                                filteredOwnerContact.Action = NativeOrganization.Action.UPDATE;
                                filteredOwnerContact.PK = filteredOwnerContact.PK;
                                filteredOwnerContact.ContactName = organization.accountOwner;
                                filteredOwnerContact.Email = organization.einvoiceEmailAddress;
                                contacts.Add(filteredOwnerContact);
                            }
                            else
                            {
                                ownerContact.ActionSpecified = true;
                                ownerContact.Action = NativeOrganization.Action.INSERT;
                                ownerContact.Language = "EN";
                                ownerContact.Title = "Owner Contact";
                                ownerContact.NotifyMode = "DND";
                                ownerContact.ContactName = organization.accountOwner;
                                ownerContact.Email = organization.einvoiceEmailAddress;
                                contacts.Add(ownerContact);
                            }
                        }
                        else
                        {
                            ownerContact.ActionSpecified = true;
                            ownerContact.Action = NativeOrganization.Action.INSERT;
                            ownerContact.Language = "EN";
                            ownerContact.Title = "Owner Contact";
                            ownerContact.NotifyMode = "DND";
                            ownerContact.ContactName = organization.accountOwner;
                            ownerContact.Email = organization.einvoiceEmailAddress;
                            contacts.Add(ownerContact);
                        }

                    }
                    organizationData.OrgHeader.OrgContactCollection = contacts.ToArray();

                    #endregion

                    #region RESGISTRATION
                    if (organization.taxId != null)
                    {
                        if(organizationData.OrgHeader.OrgCusCodeCollection is not null)
                        {
                            List<NativeOrganizationOrgCusCode> registrationCusCodes = new List<NativeOrganizationOrgCusCode>();
                            var filterTaxRegistrationCusCode = organizationData.OrgHeader.OrgCusCodeCollection.Where(cus=>cus.CodeType == "VAT").FirstOrDefault();
                            if (filterTaxRegistrationCusCode !=null)
                            {
                                filterTaxRegistrationCusCode.ActionSpecified = true;
                                filterTaxRegistrationCusCode.Action = NativeOrganization.Action.UPDATE;
                                filterTaxRegistrationCusCode.CustomsRegNo = organization.taxId;
                            }
                            else
                            {           
                                NativeOrganizationOrgCusCodeCodeCountry cusCodeCountry = new NativeOrganizationOrgCusCodeCodeCountry();
                                cusCodeCountry.Code = organization.countryCode;
                                NativeOrganizationOrgCusCode taxRegistrationCusCode = new NativeOrganizationOrgCusCode();
                                taxRegistrationCusCode.ActionSpecified = true;
                                taxRegistrationCusCode.Action = NativeOrganization.Action.INSERT;
                                taxRegistrationCusCode.CustomsRegNo = organization.taxId;
                                taxRegistrationCusCode.CodeType = "VAT";
                                taxRegistrationCusCode.CodeCountry = cusCodeCountry;
                                registrationCusCodes.Add(taxRegistrationCusCode);
                                organizationData.OrgHeader.OrgCusCodeCollection = registrationCusCodes.ToArray();
                            }
                        }

                    }
                    #endregion

                    #region RELATED PARTIES
                    List<NativeOrganizationOrgRelatedParty> relatedParties = new List<NativeOrganizationOrgRelatedParty>();
                    if (organization.brokerGlobalCustomerCode != null)
                    {
                        OrganizationData brokerOrganizationData = SearchOrgWithRegNo(organization.brokerGlobalCustomerCode);
                        if (brokerOrganizationData.OrgHeader != null)
                        {
                            NativeOrganizationOrgRelatedParty relatedParty = new NativeOrganizationOrgRelatedParty();
                            if (organizationData.OrgHeader.OrgRelatedPartyCollection is not null)
                            {
                                var filteredOrganizationRelatedData = organizationData.OrgHeader.OrgRelatedPartyCollection.Where(orp => orp.PartyType == "CAB").FirstOrDefault();
                                if (filteredOrganizationRelatedData != null)
                                {
                                    filteredOrganizationRelatedData.ActionSpecified = true;
                                    filteredOrganizationRelatedData.Action = NativeOrganization.Action.UPDATE;
                                    filteredOrganizationRelatedData.RelatedParty.ActionSpecified = true;
                                    filteredOrganizationRelatedData.RelatedParty.Action = NativeOrganization.Action.UPDATE;
                                    filteredOrganizationRelatedData.RelatedParty.PK = brokerOrganizationData.OrgHeader.PK;
                                    filteredOrganizationRelatedData.RelatedParty.Code = brokerOrganizationData.OrgHeader.Code;
                                    relatedParties.Add(filteredOrganizationRelatedData);

                                }
                                else
                                {
                                    relatedParty.ActionSpecified = true;
                                    relatedParty.Action = NativeOrganization.Action.INSERT;
                                    relatedParty.PartyType = "CAB";
                                    relatedParty.FreightTransportMode = "ALL";
                                    relatedParty.FreightDirection = "PAD";
                                    NativeOrganizationOrgRelatedPartyRelatedParty relatedPartyCode = new NativeOrganizationOrgRelatedPartyRelatedParty();
                                    relatedPartyCode.PK = brokerOrganizationData.OrgHeader.PK;
                                    relatedPartyCode.Code = organization.brokerGlobalCustomerCode;
                                    relatedParty.RelatedParty = relatedPartyCode;
                                    relatedParties.Add(relatedParty);
                                    //organizationData.OrgHeader.OrgRelatedPartyCollection = relatedParties.ToArray();
                                }
                            }
                            else
                            {
                                relatedParty.ActionSpecified = true;
                                relatedParty.Action = NativeOrganization.Action.INSERT;
                                relatedParty.PartyType = "CAB";
                                relatedParty.FreightTransportMode = "ALL";
                                relatedParty.FreightDirection = "PAD";
                                NativeOrganizationOrgRelatedPartyRelatedParty relatedPartyCode = new NativeOrganizationOrgRelatedPartyRelatedParty();
                                relatedPartyCode.PK = brokerOrganizationData.OrgHeader.PK;
                                relatedPartyCode.Code = organization.brokerGlobalCustomerCode;
                                relatedParty.RelatedParty = relatedPartyCode;
                                relatedParties.Add(relatedParty);
                                //organizationData.OrgHeader.OrgRelatedPartyCollection = relatedParties.ToArray();
                            }
                            
                        }
                        else
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "The Broker customer code " + organization.brokerGlobalCustomerCode + " not found in CW.";
                            return Ok(dataResponse);
                        }
                    }
                    if (organization.invoiceGlobalCustomerCode != null)
                    {
                        OrganizationData invoiceOrganizationData = SearchOrgWithRegNo(organization.invoiceGlobalCustomerCode);
                        if (invoiceOrganizationData.OrgHeader != null)
                        {
                            //List<NativeOrganizationOrgRelatedParty> relatedParties = new List<NativeOrganizationOrgRelatedParty>();
                            NativeOrganizationOrgRelatedParty relatedParty = new NativeOrganizationOrgRelatedParty();
                            if (organizationData.OrgHeader.OrgRelatedPartyCollection is not null)
                            {
                                var filteredOrganizationRelatedData = organizationData.OrgHeader.OrgRelatedPartyCollection.Where(orp => orp.PartyType == "LFW").FirstOrDefault();
                                if (filteredOrganizationRelatedData != null)
                                {
                                    filteredOrganizationRelatedData.ActionSpecified = true;
                                    filteredOrganizationRelatedData.Action = NativeOrganization.Action.UPDATE;
                                    filteredOrganizationRelatedData.RelatedParty.ActionSpecified = true;
                                    filteredOrganizationRelatedData.RelatedParty.Action = NativeOrganization.Action.UPDATE;
                                    filteredOrganizationRelatedData.RelatedParty.PK = invoiceOrganizationData.OrgHeader.PK;
                                    filteredOrganizationRelatedData.RelatedParty.Code = invoiceOrganizationData.OrgHeader.Code;
                                    relatedParties.Add(filteredOrganizationRelatedData);

                                }
                                else
                                {
                                    relatedParty.ActionSpecified = true;
                                    relatedParty.Action = NativeOrganization.Action.INSERT;
                                    relatedParty.PartyType = "LFW";
                                    relatedParty.FreightTransportMode = "ALL";
                                    relatedParty.FreightDirection = "PAD";
                                    NativeOrganizationOrgRelatedPartyRelatedParty relatedPartyCode = new NativeOrganizationOrgRelatedPartyRelatedParty();
                                    relatedPartyCode.PK = invoiceOrganizationData.OrgHeader.PK;
                                    relatedPartyCode.Code = organization.invoiceGlobalCustomerCode;
                                    relatedParty.RelatedParty = relatedPartyCode;
                                    relatedParties.Add(relatedParty);
                                    //organizationData.OrgHeader.OrgRelatedPartyCollection = relatedParties.ToArray();
                                }
                            }
                            else
                            {
                                relatedParty.ActionSpecified = true;
                                relatedParty.Action = NativeOrganization.Action.INSERT;
                                relatedParty.PartyType = "LFW";
                                relatedParty.FreightTransportMode = "ALL";
                                relatedParty.FreightDirection = "PAD";
                                NativeOrganizationOrgRelatedPartyRelatedParty relatedPartyCode = new NativeOrganizationOrgRelatedPartyRelatedParty();
                                relatedPartyCode.PK = invoiceOrganizationData.OrgHeader.PK;
                                relatedPartyCode.Code = organization.invoiceGlobalCustomerCode;
                                relatedParty.RelatedParty = relatedPartyCode;
                                relatedParties.Add(relatedParty);
                                //organizationData.OrgHeader.OrgRelatedPartyCollection = relatedParties.ToArray();
                            }

                        }
                        else
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "The invoice global customer code " + organization.invoiceGlobalCustomerCode + " not found in CW.";
                            return Ok(dataResponse);
                        }
                    }
                    organizationData.OrgHeader.OrgRelatedPartyCollection = relatedParties.ToArray();
                    #endregion

                    #region NOTES
                    List<NativeOrganizationStmNote> notes = new List<NativeOrganizationStmNote>();
                    if (organization.notes != null)
                    {
                        NativeOrganizationStmNote note = new NativeOrganizationStmNote();
                        if (organizationData.OrgHeader.StmNoteCollection is not null)
                        {
                            var filteredOrganizationNote = organizationData.OrgHeader.StmNoteCollection.Where(on => on.Description == "Goods Handling Instructions").FirstOrDefault();
                            if (filteredOrganizationNote != null)
                            {
                                filteredOrganizationNote.ActionSpecified = true;
                                filteredOrganizationNote.Action = NativeOrganization.Action.UPDATE;
                                filteredOrganizationNote.NoteText = organization.notes;
                                notes.Add(filteredOrganizationNote);
                            }
                            else
                            {
                                note.ActionSpecified = true;
                                note.Action = NativeOrganization.Action.INSERT;
                                //note.NoteContext = "A";
                                note.IsCustomDescription = false;
                                note.ForceRead = true;
                                note.NoteType = "PUB";
                                note.Description = "Goods Handling Instructions";
                                note.NoteText = organization.notes;
                                notes.Add(note);
                            }
                        }
                        else
                        {
                            note.ActionSpecified = true;
                            note.Action = NativeOrganization.Action.INSERT;
                            //note.NoteContext = "A";
                            note.IsCustomDescription = false;
                            note.ForceRead = true;
                            note.NoteType = "PUB";
                            note.Description = "Goods Handling Instructions";
                            note.NoteText = organization.notes;
                            notes.Add(note);
                        }
                    }

                    if (organization.creditRiskNotes != null)
                    {
                        NativeOrganizationStmNote note = new NativeOrganizationStmNote();
                        if (organizationData.OrgHeader.StmNoteCollection is not null)
                        {
                            var filteredOrganizationCreditRiskNote = organizationData.OrgHeader.StmNoteCollection.Where(on => on.Description == "A/R Credit Management Note").FirstOrDefault();
                            if (filteredOrganizationCreditRiskNote is not null)
                            {
                                filteredOrganizationCreditRiskNote.ActionSpecified = true;
                                filteredOrganizationCreditRiskNote.Action = NativeOrganization.Action.UPDATE;
                                filteredOrganizationCreditRiskNote.NoteText = organization.creditRiskNotes;
                                notes.Add(filteredOrganizationCreditRiskNote);
                            }
                            else
                            {
                                note.ActionSpecified = true;
                                note.Action = NativeOrganization.Action.INSERT;
                                //note.NoteContext = "A";
                                note.IsCustomDescription = false;
                                note.ForceRead = true;
                                note.NoteType = "PUB";
                                note.Description = "A/R Credit Management Note";
                                note.NoteText = organization.creditRiskNotes;
                                notes.Add(note);
                            }
                        }
                        else
                        {
                            note.ActionSpecified = true;
                            note.Action = NativeOrganization.Action.INSERT;
                            //note.NoteContext = "A";
                            note.IsCustomDescription = false;
                            note.ForceRead = true;
                            note.NoteType = "PUB";
                            note.Description = "A/R Credit Management Note";
                            note.NoteText = organization.creditRiskNotes;
                            notes.Add(note);
                        }
                    }
                    organizationData.OrgHeader.StmNoteCollection = notes.ToArray();
                    #endregion

                    #region ORGANIZATION ADDRESS
                    string additionalAddress = organization.address3 + " " + organization.address4;
                    organizationData.OrgHeader.OrgAddressCollection[0].ActionSpecified = true;
                    organizationData.OrgHeader.OrgAddressCollection[0].Action = NativeOrganization.Action.UPDATE;
                    organizationData.OrgHeader.OrgAddressCollection[0].Code = organization.address1?.Substring(0, Math.Min(organization.address1.Length, 24));
                    organizationData.OrgHeader.OrgAddressCollection[0].Address1 = organization.address1;
                    organizationData.OrgHeader.OrgAddressCollection[0].Address2 = organization.address2;
                    organizationData.OrgHeader.OrgAddressCollection[0].AdditionalAddressInformation = additionalAddress?.Substring(0, Math.Min(additionalAddress.Length, 50));

                    organizationData.OrgHeader.OrgAddressCollection[0].RelatedPortCode.ActionSpecified = true;
                    organizationData.OrgHeader.OrgAddressCollection[0].RelatedPortCode.Action = NativeOrganization.Action.UPDATE;
                    organizationData.OrgHeader.OrgAddressCollection[0].RelatedPortCode.PK = ClosestPortPK;
                    organizationData.OrgHeader.OrgAddressCollection[0].RelatedPortCode.Code = site.Country+site.Airport;

                    organizationData.OrgHeader.OrgAddressCollection[0].City = organization.city;
                    organizationData.OrgHeader.OrgAddressCollection[0].PostCode = organization.postalCode;
                    organizationData.OrgHeader.OrgAddressCollection[0].State = organization.provinceCode;
                    organizationData.OrgHeader.OrgAddressCollection[0].CountryCode.Code = countryCode;
                    organizationData.OrgHeader.OrgAddressCollection[0].Phone = organization.phoneNumber;
                    organizationData.OrgHeader.OrgAddressCollection[0].Mobile = organization.mobileNumber;
                    organizationData.OrgHeader.OrgAddressCollection[0].Fax = organization.faxNumber;
                    organizationData.OrgHeader.OrgAddressCollection[0].Email = organization.emailAddress;
                    #endregion

                    

                    #region CUSTOM VALUES
                    Dictionary<string, string> customValues = new Dictionary<string, string>();
                    //customValues.Add("siteCode", organization.siteCode);
                    customValues.Add("locationVerifiedDate", organization.locationVerifiedDate);
                    customValues.Add("kycCreatedPrior2018", organization.kycCreatedPrior2018.ToString());
                    customValues.Add("kycOpenProcCompleted", organization.kycOpenProcCompleted.ToString());
                    customValues.Add("kycRefNbr", organization.kycRefNbr);
                    customValues.Add("kycVerifDate", organization.kycVerifDate);
                    customValues.Add("kycApprovedBy", organization.kycApprovedBy);
                    customValues.Add("kycOpeningStation", organization.kycOpeningStation);
                    customValues.Add("lob", organization.lob);
                    customValues.Add("adyenPay", organization.adyenPay.ToString());
                    customValues.Add("adyenPayPreference", organization.adyenPayPreference);
                    customValues.Add("adyenTokenId", organization.adyenTokenId);
                    customValues.Add("adyenPayByLinkId", organization.adyenPayByLinkId);
                    customValues = customValues.Where(c => c.Value != null && c.Value !="").ToDictionary(x => x.Key, x => x.Value);
                    List<NativeOrganizationJobRequiredDocument> documents = new List<NativeOrganizationJobRequiredDocument>();
                    int count = 0;
                    foreach (KeyValuePair<string, string> entry in customValues)
                    {
                        if (organizationData.OrgHeader.JobRequiredDocumentCollection is not null)
                        {
                            var filteredCustomObject = organizationData.OrgHeader.JobRequiredDocumentCollection.Where(jrd => jrd.DocumentNotes == entry.Key).FirstOrDefault();
                            if (filteredCustomObject != null)
                            {
                                filteredCustomObject.ActionSpecified = true;
                                filteredCustomObject.Action = NativeOrganization.Action.UPDATE;
                                filteredCustomObject.DocDescription = entry.Value;
                                documents.Add(filteredCustomObject);
                            }
                            else
                            {
                                NativeOrganizationJobRequiredDocument document = new NativeOrganizationJobRequiredDocument();
                                document.ActionSpecified = true;
                                document.Action = NativeOrganization.Action.INSERT;
                                document.DocCategory = "CSR";
                                document.DocType = "MSC";
                                document.DocUsage = "BRK";
                                document.DocPeriod = "SHP";
                                document.DocumentNotes = entry.Key;
                                document.DocDescription = entry.Value;
                                document.DocNumber = count.ToString();
                                documents.Add(document);
                                count++;
                            }
                        }
                        else
                        {
                            NativeOrganizationJobRequiredDocument document = new NativeOrganizationJobRequiredDocument();
                            document.ActionSpecified = true;
                            document.Action = NativeOrganization.Action.INSERT;
                            document.DocCategory = "CSR";
                            document.DocType = "MSC";
                            document.DocUsage = "BRK";
                            document.DocPeriod = "SHP";
                            document.DocumentNotes = entry.Key;
                            document.DocDescription = entry.Value;
                            document.DocNumber = count.ToString();
                            documents.Add(document);
                            count++;
                        }

                    }
                    organizationData.OrgHeader.JobRequiredDocumentCollection = documents.ToArray();

                    #endregion
                    successMessage = "Organization Updated Successfully.";
                    #endregion
                }

                var serilaziedBody = Utilities.SerializeToXmlElement(organizationData);

                List<XmlElement> xmlElements = new List<XmlElement>();
                xmlElements.Add(serilaziedBody);
                body.Any = xmlElements.ToArray();
                native.Body = body;

                string xml = Utilities.Serialize(native);
                var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                if (documentResponse.Status == "ERROR")
                {
                    dataResponse.Status = documentResponse.Status;
                    dataResponse.Message = documentResponse.Data.Data.FirstChild.InnerText.Replace("Error - ", "").Replace("Warning - ", "");
                    return Ok(dataResponse);
                }

                dataResponse.Status = "SUCCESS";
                dataResponse.Message = successMessage;
                return Ok(dataResponse);
            }
            catch (Exception ex)
            {
                dataResponse.Status = "ERROR";
                dataResponse.Message = ex.Message;
                return Ok(ex.Message);
            }
        }
        #endregion

        public OrganizationData SearchOrgWithCode(string orgCode)
        {
            OrganizationData organizationData = new OrganizationData();
            try 
            {
                NativeRequest.Native native = new NativeRequest.Native();
                NativeRequest.NativeBody body = new NativeRequest.NativeBody();
                body.ItemElementName = ItemChoiceType.Organization;
                CriteriaData criteria = new CriteriaData();

                CriteriaGroupType criteriaGroupType = new CriteriaGroupType();
                criteriaGroupType.Type = TypeEnum.Key;
                List<CriteriaType> criteriaTypes = new List<CriteriaType>();

                CriteriaType criteriaType1 = new CriteriaType();
                criteriaType1.Entity = "OrgHeader";
                criteriaType1.FieldName = "Code";
                criteriaType1.Value = orgCode;
                criteriaTypes.Add(criteriaType1);
                criteriaGroupType.Criteria = criteriaTypes.ToArray();

                criteria.CriteriaGroup = criteriaGroupType;
                body.ItemElementName = ItemChoiceType.Organization;
                body.Item = criteria;
                native.Body = body;
                string xml = Utilities.Serialize(native);
                var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                if (documentResponse.Status == "SUCCESS" && documentResponse.Data.Status == "PRS" && documentResponse.Data.ProcessingLog != null)
                {
                    using (TextReader reader = new StringReader(documentResponse.Data.Data.OuterXml))
                    {
                        var serializer = new XmlSerializer(typeof(NativeOrganization.Native));
                        NativeOrganization.Native result = (NativeOrganization.Native)serializer.Deserialize(reader);
                        string organization = result.Body.Any[0].OuterXml;
                        using (TextReader reader2 = new StringReader(organization))
                        {
                            var serializer2 = new XmlSerializer(typeof(OrganizationData));
                            organizationData = (OrganizationData)serializer2.Deserialize(reader2);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return organizationData;
        }
        public OrganizationData SearchOrgWithRegNo(string regNo) 
        {
            OrganizationData organizationData = new OrganizationData();
            try 
            {
                NativeRequest.Native native = new NativeRequest.Native();
                NativeRequest.NativeBody body = new NativeRequest.NativeBody();
                CriteriaData criteria= new CriteriaData();

                CriteriaGroupType criteriaGroupType = new CriteriaGroupType();
                criteriaGroupType.Type = TypeEnum.Partial;

                List<CriteriaType> criteriaTypes = new List<CriteriaType>();

                CriteriaType criteriaType1 = new CriteriaType();
                criteriaType1.Entity = "OrgHeader.OrgCusCode";
                criteriaType1.FieldName = "CodeType";
                criteriaType1.Value = "LSC";
                criteriaTypes.Add(criteriaType1);

                CriteriaType criteriaType2 = new CriteriaType();
                criteriaType2.Entity = "OrgHeader.OrgCusCode";
                criteriaType2.FieldName = "CustomsRegNo";
                criteriaType2.Value = regNo;
                criteriaTypes.Add(criteriaType2);

                criteriaGroupType.Criteria = criteriaTypes.ToArray();

                criteria.CriteriaGroup = criteriaGroupType;
                body.ItemElementName = ItemChoiceType.Organization;
                body.Item = criteria;
                native.Body = body;

                string xml = Utilities.Serialize(native);
                var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                if(documentResponse.Status == "SUCCESS" && documentResponse.Data.Status=="PRS" && documentResponse.Data.ProcessingLog!=null)
                {
                    using (TextReader reader = new StringReader(documentResponse.Data.Data.OuterXml))
                    {
                        var serializer = new XmlSerializer(typeof(NativeOrganization.Native));
                        NativeOrganization.Native result = (NativeOrganization.Native)serializer.Deserialize(reader);
                        string organization = result.Body.Any[0].OuterXml;
                        using (TextReader reader2 = new StringReader(organization))
                        {
                            var serializer2 = new XmlSerializer(typeof(OrganizationData));
                            organizationData = (OrganizationData)serializer2.Deserialize(reader2);
                        }
                    }
                }

            }
            catch (Exception ex) 
            {
                throw ex;
            }
            return organizationData; 
        }

        public string SearchUNLOCOCode(string unlocoCode)
        {
            string pk = null;
            try 
            {
                NativeRequest.Native native = new NativeRequest.Native();
                NativeRequest.NativeBody body = new NativeRequest.NativeBody();
                CriteriaData criteria = new CriteriaData();

                CriteriaGroupType criteriaGroupType = new CriteriaGroupType();
                criteriaGroupType.Type = TypeEnum.Key;
                List<CriteriaType> criteriaTypes = new List<CriteriaType>();

                CriteriaType criteriaType = new CriteriaType();
                criteriaType.Entity = "RefUNLOCO";
                criteriaType.FieldName = "Code";
                criteriaType.Value = unlocoCode;
                criteriaTypes.Add(criteriaType);

                criteriaGroupType.Criteria = criteriaTypes.ToArray();

                criteria.CriteriaGroup = criteriaGroupType;
                body.ItemElementName = ItemChoiceType.UNLOCO;
                body.Item = criteria;
                native.Body = body;

                string xml = Utilities.Serialize(native);
                var documentResponse = eAdaptor.Services.SendToCargowise(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                if (documentResponse.Status == "SUCCESS" && documentResponse.Data.Status == "PRS" && documentResponse.Data.Data != null)
                {
                    using (TextReader reader = new StringReader(documentResponse.Data.Data.OuterXml))
                    {

                        var serializer = new XmlSerializer(typeof(NativeOrganization.Native));
                        NativeOrganization.Native result = (NativeOrganization.Native)serializer.Deserialize(reader);
                        string unloco = result.Body.Any[0].OuterXml;

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(unloco);
                        pk = doc.GetElementsByTagName("PK")[0]?.InnerText;

                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return pk;
        }
    }
}
