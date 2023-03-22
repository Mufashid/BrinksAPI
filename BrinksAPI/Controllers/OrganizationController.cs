using BrinksAPI.Auth;
using BrinksAPI.Helpers;
using BrinksAPI.Interfaces;
using BrinksAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NativeOrganization;
using NativeRequest;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace BrinksAPI.Controllers
{
    [Authorize]
    public class OrganizationController : Controller
    {
        private readonly ILogger<OrganizationController> _logger;
        private readonly IConfigManager _configuration;
        private readonly ApplicationDbContext _context;

        public OrganizationController(IConfigManager configuaration,ApplicationDbContext context, ILogger<OrganizationController> logger)
        {
            _configuration = configuaration;
            _context = context;
            _logger = logger;
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
        ///        "RiskCodeDescription": "Good",
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
        public ActionResult<Response> UpsertOrganization([FromBody] Organization organization)
        {
            Response dataResponse = new Response();
            string successMessage = "";
            try
            {
                dataResponse.RequestId = organization?.requestId;

                #region MODEL VALIDATION
                if (!ModelState.IsValid)
                {
                    string errorString = "";
                    var errors = ModelState.Select(x => x.Value.Errors)
                         .Where(y => y.Count > 0)
                         .ToList();
                    foreach (var error in errors)
                    {
                        foreach (var subError in error)
                        {
                            errorString += String.Format("{0}", subError.ErrorMessage);
                        }
                    }
                    dataResponse.Status = "ERROR";
                    dataResponse.Message = errorString;

                    return Ok(dataResponse);
                }
                #endregion

                if (!string.IsNullOrEmpty(organization.billingAttention) && !string.IsNullOrEmpty(organization.contactName) && organization.billingAttention == organization.contactName)
                {
                    dataResponse.Status = "ERROR";
                    dataResponse.Message = "Billing Attention and Contact Name field cannot be same.";
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


                    #region SITE CODE
                    //DEFAULT SITE ID (Check sitecode first if not then check country code)
                    string? unlocoCode = "";
                    string? cwCompanyCode = "";
                    string? cwBranchCode = "";
                    if (organization.countryCode != null)
                    {
                        var organizationUnloco = _context.organizationUnloco.Where(s => s.Alpha2Code == organization.countryCode).FirstOrDefault();
                        unlocoCode = organizationUnloco?.DefaultUNLOCO;

                        var organizationSite = _context.organizationSites.Where(s => s.CountryCode == organization.countryCode).FirstOrDefault();
                        cwCompanyCode = organizationSite?.CWBranchCode;

                        if (string.IsNullOrEmpty(unlocoCode))
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "countryCode " + organization.countryCode + " is not a valid mapping in the DB (organizationUnloco).";
                            return Ok(dataResponse);
                        }
                    }

                    if (organization.siteCode != null)
                    {
                        var organizationSite = _context.organizationSites.Where(s => s.SiteCode == organization.siteCode)?.FirstOrDefault();
                        var organizationMngSite = _context.sites.Where(s => s.FinancialMgmt == organization.siteCode)?.FirstOrDefault();
                        unlocoCode = organizationSite.Unloco;
                        cwCompanyCode = organizationSite.CWBranchCode;// This is company code
                        cwBranchCode  = organizationMngSite?.BranchCode;

                        if (string.IsNullOrEmpty(unlocoCode))
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "siteCode " + organization.siteCode + " is not a valid mapping in the DB (organizationSites).";
                            return Ok(dataResponse);
                        }
                    } 
                    #endregion

                    #region CLOSEST PORT
                    if (!string.IsNullOrEmpty(unlocoCode))
                    {
                        string pk = SearchUNLOCOCode(unlocoCode);
                        if (pk == null)
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "siteCode = " + organization.siteCode + ". UNLOCO = " + unlocoCode + " is not a valid UNLOCO code in CW.";
                            return Ok(dataResponse);
                        }
                        NativeOrganizationClosestPort nativeOrganizationClosestPort = new NativeOrganizationClosestPort();
                        nativeOrganizationClosestPort.TableName = "RefUNLOCO";
                        nativeOrganizationClosestPort.Code = unlocoCode;
                        nativeOrganization.ClosestPort = nativeOrganizationClosestPort;
                    }
                    else
                    {
                        dataResponse.Status = "ERROR";
                        dataResponse.Message = "Please provide a valid country code or site code. Closest port(unloco) is a required field in CW";
                        return Ok(dataResponse);
                    }
                    #endregion

                    #region CONSIGNOR OR CONSIGNEE
                    nativeOrganization.IsConsigneeSpecified = true;
                    nativeOrganization.IsConsignee = true;
                    nativeOrganization.IsConsignorSpecified = true;
                    nativeOrganization.IsConsignor = true;
                    #endregion

                    #region ORGANIZATION COUNTRY DATA TSA
                    //if (!String.IsNullOrEmpty(organization.tsaValidationId) && !String.IsNullOrEmpty(organization.locationVerifiedDate) && organization.countryCode?.ToLower() == "us")
                    //{
                    //    List<NativeOrganizationOrgCountryData> orgCountryDatas = new List<NativeOrganizationOrgCountryData>();
                    //    NativeOrganizationOrgCountryData orgCountryData = new NativeOrganizationOrgCountryData();
                    //    orgCountryData.ActionSpecified = true;
                    //    orgCountryData.Action = NativeOrganization.Action.INSERT;
                    //    orgCountryData.EXApprovedOrMajorExporter = "NO";
                    //    orgCountryData.EXApprovalNumber = organization.tsaValidationId;
                    //    orgCountryData.EXExportPermissionDetails = organization.tsaType;
                    //    orgCountryData.EXSiteInspectionDate = DateTime.ParseExact(organization.locationVerifiedDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTmm:ss:ff");
                    //    //orgCountryData.EXSiteInspectionDate = DateTime.ParseExact(organization.locationVerifiedDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTmm:ss:ff");

                    //    NativeOrganizationOrgCountryDataClientCountryRelation clientCountryRelation = new NativeOrganizationOrgCountryDataClientCountryRelation();
                    //    clientCountryRelation.TableName = "RefCountry";
                    //    clientCountryRelation.Code = organization.countryCode;
                    //    orgCountryData.ClientCountryRelation = clientCountryRelation;

                    //    string? approvedLocation = organization.address1?.Substring(0, Math.Min(organization.address1.Length, 24));
                    //    NativeOrganizationOrgCountryDataApprovedLocation dataApprovedLocation = new NativeOrganizationOrgCountryDataApprovedLocation();
                    //    dataApprovedLocation.TableName = "OrgAddress";
                    //    dataApprovedLocation.Code = approvedLocation;
                    //    orgCountryData.ApprovedLocation = dataApprovedLocation;

                    //    NativeOrganizationOrgCountryDataIssuingAuthorityCountry authorityCountry = new NativeOrganizationOrgCountryDataIssuingAuthorityCountry();
                    //    authorityCountry.TableName = "RefCountry";
                    //    authorityCountry.Code = organization.countryCode;
                    //    orgCountryData.IssuingAuthorityCountry = authorityCountry;

                    //    orgCountryDatas.Add(orgCountryData);
                    //    nativeOrganization.OrgCountryDataCollection = orgCountryDatas.ToArray();
                    //}
                    #endregion

                    #region COMPANY DATA AND CONTROLLING BRANCH

                    List<NativeOrganizationOrgCompanyData> orgCompanyDatas = new List<NativeOrganizationOrgCompanyData>();
                    NativeOrganizationOrgCompanyData orgCompanyData = new NativeOrganizationOrgCompanyData();
                    orgCompanyData.ActionSpecified = true;
                    orgCompanyData.Action = NativeOrganization.Action.INSERT;

                    if (String.IsNullOrEmpty(cwCompanyCode))
                    {
                        dataResponse.Status = "ERROR";
                        dataResponse.Message = "Not a valid company code in DB. Please check the site code or the country code.";
                        return Ok(dataResponse);
                    }
                    NativeOrganizationOrgCompanyDataGlbCompany company = new NativeOrganizationOrgCompanyDataGlbCompany();
                    company.Code = cwCompanyCode;
                    orgCompanyData.GlbCompany = company;

                    NativeOrganizationOrgCompanyDataControllingBranch branch = new NativeOrganizationOrgCompanyDataControllingBranch();
                    branch.Code = cwBranchCode;
                    orgCompanyData.ControllingBranch = branch;

                    if (!String.IsNullOrEmpty(organization.RiskCodeDescription) || !String.IsNullOrEmpty(organization.preferredCurrency))
                    {
                        orgCompanyData.IsDebtorSpecified = true;
                        orgCompanyData.IsDebtor = true;

                        NativeOrganizationOrgCompanyDataARDebtorGroup debtorGroup = new NativeOrganizationOrgCompanyDataARDebtorGroup();
                        debtorGroup.TableName = "OrgDebtorGroup";
                        debtorGroup.Code = "TPY";
                        orgCompanyData.ARDebtorGroup = debtorGroup;
                        if (!String.IsNullOrEmpty(organization.RiskCodeDescription))
                        {
                            var riskCodeDescription = _context.riskCodeDescriptions.Where(r => r.BrinksCode.ToLower() == organization.RiskCodeDescription.ToLower()).FirstOrDefault();
                            if (riskCodeDescription == null)
                            {
                                dataResponse.Status = "ERROR";
                                dataResponse.Message = "Not a valid Risk Code Description '" + organization.RiskCodeDescription + "' in DB.";
                                return Ok(dataResponse);
                            }
                            orgCompanyData.ARCreditRating = riskCodeDescription.CWCode;
                        }

                        if(!String.IsNullOrEmpty(organization.preferredCurrency))
                        {
                            NativeOrganizationOrgCompanyDataARDDefltCurrency arDefltCurrency = new NativeOrganizationOrgCompanyDataARDDefltCurrency();
                            arDefltCurrency.TableName = "RefCurrency";
                            arDefltCurrency.Code = organization.preferredCurrency;
                            orgCompanyData.ARDDefltCurrency = arDefltCurrency;
                        }

                    }
                    orgCompanyDatas.Add(orgCompanyData);
                    nativeOrganization.OrgCompanyDataCollection = orgCompanyDatas.ToArray();
                    #endregion

                    #region CONTACTS
                    List<NativeOrganizationOrgContact> contacts = new List<NativeOrganizationOrgContact>();

                    if (!string.IsNullOrEmpty(organization.contactName))
                    {
                        NativeOrganizationOrgContact contactName = new NativeOrganizationOrgContact();
                        contactName.ActionSpecified = true;
                        contactName.Action = NativeOrganization.Action.INSERT;
                        contactName.Language = "EN";
                        contactName.Title = "Contact Name";
                        contactName.NotifyMode = "DND";
                        contactName.ContactName = organization.contactName;
                        contactName.Email = organization.emailAddress;
                        contactName.Phone = organization.phoneNumber;
                        contactName.Mobile = organization.mobileNumber;
                        contactName.Fax = organization.faxNumber;
                        contacts.Add(contactName);
                    }
                    if (!string.IsNullOrEmpty(organization.billingAttention))
                    {
                        NativeOrganizationOrgContact billingContact = new NativeOrganizationOrgContact();
                        billingContact.ActionSpecified = true;
                        billingContact.Action = NativeOrganization.Action.INSERT;
                        billingContact.Language = "EN";
                        billingContact.Title = "Billing Attention";
                        billingContact.NotifyMode = "DND";
                        billingContact.ContactName = organization.billingAttention;
                        billingContact.Email = organization.einvoiceEmailAddress;
                        contacts.Add(billingContact);
                    }
                    nativeOrganization.OrgContactCollection = contacts.ToArray();
                    #endregion

                    #region STAFFS
                    if (organization.accountOwner != null)
                    {
                        List<NativeOrganizationOrgStaffAssignments> staffAssignments = new List<NativeOrganizationOrgStaffAssignments>();
                        NativeOrganizationOrgStaffAssignments staffAssignment = new NativeOrganizationOrgStaffAssignments();
                        staffAssignment.ActionSpecified = true;
                        staffAssignment.Action = NativeOrganization.Action.INSERT;
                        NativeOrganizationOrgStaffAssignmentsPersonResponsible accountOwner = new NativeOrganizationOrgStaffAssignmentsPersonResponsible();
                        accountOwner.Code = organization.accountOwner;
                        staffAssignment.Role = "ACT";
                        staffAssignment.PersonResponsible = accountOwner;
                        staffAssignment.Department = "ALL";
                        staffAssignments.Add(staffAssignment);
                        nativeOrganization.OrgStaffAssignmentsCollection = staffAssignments.ToArray();
                    }
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

                    if(organization.eoriNumber != null)
                    {
                        NativeOrganizationOrgCusCode eorRegistrationCusCode = new NativeOrganizationOrgCusCode();
                        eorRegistrationCusCode.ActionSpecified = true;
                        eorRegistrationCusCode.Action = NativeOrganization.Action.INSERT;
                        eorRegistrationCusCode.CustomsRegNo = organization.eoriNumber;
                        eorRegistrationCusCode.CodeType = "EOR";
                        eorRegistrationCusCode.CodeCountry = cusCodeCountry;
                        registrationCusCodes.Add(eorRegistrationCusCode);
                    }

                    nativeOrganization.OrgCusCodeCollection = registrationCusCodes.ToArray();
                    #endregion

                    #region RELATED PARTIES
                    List<NativeOrganizationOrgRelatedParty> relatedParties = new List<NativeOrganizationOrgRelatedParty>();
                    //if (organization.invoiceGlobalCustomerCode != null)
                    //{
                    //    OrganizationData invoiceOrganizationData = SearchOrgWithRegNo(organization.invoiceGlobalCustomerCode);
                    //    if (invoiceOrganizationData.OrgHeader != null)
                    //    {
                    //        NativeOrganizationOrgRelatedParty relatedParty = new NativeOrganizationOrgRelatedParty();
                    //        relatedParty.ActionSpecified = true;
                    //        relatedParty.Action = NativeOrganization.Action.INSERT;
                    //        relatedParty.PartyType = "LFW";
                    //        relatedParty.FreightTransportMode = "ALL";
                    //        relatedParty.FreightDirection = "PAD";
                    //        NativeOrganizationOrgRelatedPartyRelatedParty relatedPartyCode = new NativeOrganizationOrgRelatedPartyRelatedParty();
                    //        relatedPartyCode.PK = invoiceOrganizationData.OrgHeader.PK;
                    //        relatedPartyCode.Code = organization.invoiceGlobalCustomerCode;
                    //        relatedParty.RelatedParty = relatedPartyCode;
                    //        relatedParties.Add(relatedParty);
                    //        //nativeOrganization.OrgRelatedPartyCollection = relatedParties.ToArray();
                    //    }
                    //    else
                    //    {
                    //        dataResponse.Status = "ERROR";
                    //        dataResponse.Message = "The Invoice global customer code " + organization.invoiceGlobalCustomerCode + " not found in CW.";
                    //        return Ok(dataResponse);
                    //    }


                    //}
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
                    if(organization.siteCode != null)
                    {
                        NativeOrganizationStmNote siteCodeNote = new NativeOrganizationStmNote();
                        siteCodeNote.ActionSpecified = true;
                        siteCodeNote.Action = NativeOrganization.Action.INSERT;
                        siteCodeNote.NoteContext = "ALL";
                        siteCodeNote.IsCustomDescriptionSpecified = true;
                        siteCodeNote.IsCustomDescription = true;
                        siteCodeNote.ForceRead = true;
                        siteCodeNote.NoteType = "PUB";
                        siteCodeNote.Description = "Site Code";
                        siteCodeNote.NoteText = organization.siteCode;
                        notes.Add(siteCodeNote);
                    }

                    Dictionary<string, string> kycDict = new Dictionary<string, string>();
                    kycDict.Add("kycCreatedPrior2018",string.IsNullOrEmpty(organization.kycCreatedPrior2018.ToString())?null:organization.kycCreatedPrior2018.ToString());
                    kycDict.Add("kycOpenProcCompleted", string.IsNullOrEmpty(organization.kycOpenProcCompleted.ToString()) ? null : organization.kycOpenProcCompleted.ToString());
                    kycDict.Add("kycRefNbr", organization.kycRefNbr);
                    kycDict.Add("kycVerifDate", organization.kycVerifDate);
                    kycDict.Add("kycApprovedBy", organization.kycApprovedBy);
                    kycDict.Add("kycOpeningStation", organization.kycOpeningStation);

                    string kycNotes = "";
                    foreach (KeyValuePair<string, string> entry in kycDict)
                        if (entry.Value != null)
                            kycNotes += String.Format("{0} : {1}\n", entry.Key, entry.Value);
                    
                    if(kycNotes != "")
                    {
                        NativeOrganizationStmNote kycNote = new NativeOrganizationStmNote();
                        kycNote.ActionSpecified = true;
                        kycNote.Action = NativeOrganization.Action.INSERT;
                        kycNote.NoteContext = "ALL";
                        kycNote.IsCustomDescription = false;
                        kycNote.ForceRead = true;
                        kycNote.NoteType = "PUB";
                        kycNote.Description = "Additional Information";
                        kycNote.NoteText = kycNotes;
                        notes.Add(kycNote);
                    }

                    nativeOrganization.StmNoteCollection = notes.ToArray();
                    #endregion

                    #region ORGANIZATION ADDRESS
                    NativeOrganizationOrgAddress nativeOrgAddress = new NativeOrganizationOrgAddress();
                    List<NativeOrganizationOrgAddress> nativeOrgAddresses = new List<NativeOrganizationOrgAddress>();
                    nativeOrgAddress.ActionSpecified = true;
                    nativeOrgAddress.Action = NativeOrganization.Action.INSERT;
                    nativeOrgAddress.IsActiveSpecified = true;
                    nativeOrgAddress.IsActive = true;
                    nativeOrgAddress.Code = organization.address1?.Substring(0, Math.Min(organization.address1.Length, 24));
                    nativeOrgAddress.Address1 = organization.address1;
                    nativeOrgAddress.Address2 = organization.address2;
                    nativeOrgAddress.AdditionalAddressInformation = organization?.address3;

                    List<NativeOrganizationOrgAddressOrgAddressAdditionalInfo> additionalInfoAddresses = new List<NativeOrganizationOrgAddressOrgAddressAdditionalInfo>();
                    if (!String.IsNullOrEmpty(organization.address3))
                    {
                        NativeOrganizationOrgAddressOrgAddressAdditionalInfo additionalInfoAddress3 = new NativeOrganizationOrgAddressOrgAddressAdditionalInfo();
                        additionalInfoAddress3.ActionSpecified = true;
                        additionalInfoAddress3.Action = NativeOrganization.Action.INSERT;
                        additionalInfoAddress3.IsPrimarySpecified = true;
                        additionalInfoAddress3.IsPrimary = true;
                        additionalInfoAddress3.AdditionalInfo = organization?.address3;
                        additionalInfoAddresses.Add(additionalInfoAddress3);
                    }
                    if (!String.IsNullOrEmpty(organization.address4))
                    {
                        NativeOrganizationOrgAddressOrgAddressAdditionalInfo additionalInfoAddress4 = new NativeOrganizationOrgAddressOrgAddressAdditionalInfo();
                        additionalInfoAddress4.ActionSpecified = true;
                        additionalInfoAddress4.Action = NativeOrganization.Action.INSERT;
                        additionalInfoAddress4.AdditionalInfo = organization?.address4;
                        additionalInfoAddresses.Add(additionalInfoAddress4);
                    }
                    nativeOrgAddress.OrgAddressAdditionalInfoCollection = additionalInfoAddresses.ToArray();

                    nativeOrgAddress.City = organization.city;
                    nativeOrgAddress.PostCode = organization.postalCode;
                    nativeOrgAddress.State = organization.provinceCode;
                    NativeOrganizationOrgAddressCountryCode nativeOrgCountryCode = new NativeOrganizationOrgAddressCountryCode();
                    nativeOrgCountryCode.TableName = "RefCountry";
                    nativeOrgCountryCode.Code = organization.countryCode;
                    nativeOrgAddress.CountryCode = nativeOrgCountryCode;
                    NativeOrganizationOrgAddressRelatedPortCode nativeOrgAddressRelatedPortCode = new NativeOrganizationOrgAddressRelatedPortCode();
                    nativeOrgAddressRelatedPortCode.TableName = "RefUNLOCO";
                    nativeOrgAddressRelatedPortCode.Code = unlocoCode;

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

                    #region DOCUMENT VALUES
                    Dictionary<string, string> customValues = new Dictionary<string, string>();
                    customValues.Add("Lob", organization.lob);
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
                    #region SITE CODE
                    string? unlocoCode = "";
                    string? cwCompanyCode = "";
                    string? cwBranchCode = "";
                    if (organization.countryCode != null)
                    {
                        var organizationUnloco = _context.organizationUnloco.Where(s => s.Alpha2Code == organization.countryCode).FirstOrDefault();
                        unlocoCode = organizationUnloco?.DefaultUNLOCO;

                        var organizationSite = _context.organizationSites.Where(s => s.CountryCode == organization.countryCode).FirstOrDefault();
                        cwCompanyCode = organizationSite?.CWBranchCode;

                        if (string.IsNullOrEmpty(unlocoCode))
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "countryCode " + organization.countryCode + " is not a valid mapping in the DB (organizationUnloco).";
                            return Ok(dataResponse);
                        }
                    }

                    if (organization.siteCode != null)
                    {
                        var organizationSite = _context.organizationSites.Where(s => s.SiteCode == organization.siteCode)?.FirstOrDefault();
                        var organizationMngSite = _context.sites.Where(s => s.FinancialMgmt == organization.siteCode)?.FirstOrDefault();
                        unlocoCode = organizationSite.Unloco;
                        cwCompanyCode = organizationSite.CWBranchCode;
                        cwBranchCode = organizationMngSite?.BranchCode;

                        if (string.IsNullOrEmpty(unlocoCode))
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "siteCode " + organization.siteCode + " is not a valid mapping in the DB (organizationSites).";
                            return Ok(dataResponse);
                        }

                    } 
                    #endregion

                    #region CLOSEST PORT
                    if (!string.IsNullOrEmpty(unlocoCode))
                    {
                        string unlocoPK = SearchUNLOCOCode(unlocoCode);
                        if (string.IsNullOrEmpty(unlocoPK))
                        {
                            dataResponse.Status = "ERROR";
                            dataResponse.Message = "siteCode = " + organization.siteCode + ". UNLOCO = " + unlocoCode + " is not a valid UNLOCO code in CW.";
                            return Ok(dataResponse);
                        }
                        organizationData.OrgHeader.ClosestPort.ActionSpecified = true;
                        organizationData.OrgHeader.ClosestPort.Action = NativeOrganization.Action.UPDATE;
                        organizationData.OrgHeader.ClosestPort.PK = unlocoPK;
                        organizationData.OrgHeader.ClosestPort.Code = unlocoCode;
                    }

                    #endregion

                    #region CONSIGNOR OR CONSIGNEE
                    organizationData.OrgHeader.IsConsigneeSpecified = true;
                    organizationData.OrgHeader.IsConsignorSpecified = true;
                    organizationData.OrgHeader.IsConsignee = true;
                    organizationData.OrgHeader.IsConsignor = true;
                    #endregion

                    #region ORGANIZATION COUNTRY DATA TSA
                    //string? currentCountryCode = organizationData.OrgHeader?.OrgAddressCollection.FirstOrDefault()?.CountryCode?.Code;
                    //if (!string.IsNullOrEmpty(organization.tsaValidationId) && !string.IsNullOrEmpty(organization.locationVerifiedDate) && organization.countryCode?.ToLower() == "us")
                    //{
                    //    if (organizationData.OrgHeader.OrgCountryDataCollection is not null)
                    //    {
                    //        NativeOrganizationOrgCountryData? filterdOrganizationCountryData = organizationData.OrgHeader.OrgCountryDataCollection.Where(o => o.ClientCountryRelation?.Code?.ToLower() == "us" && o.IssuingAuthorityCountry?.Code?.ToLower() == "us").FirstOrDefault();
                    //        if (filterdOrganizationCountryData is not null)
                    //        {
                    //            filterdOrganizationCountryData.ActionSpecified = true;
                    //            filterdOrganizationCountryData.Action = NativeOrganization.Action.UPDATE;
                    //            filterdOrganizationCountryData.EXApprovalNumber = organization.tsaValidationId;
                    //            filterdOrganizationCountryData.EXExportPermissionDetails = organization.tsaType;
                    //            filterdOrganizationCountryData.EXSiteInspectionDate = DateTime.ParseExact(organization.locationVerifiedDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTmm:ss:ff");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        List<NativeOrganizationOrgCountryData> orgCountryDatas = new List<NativeOrganizationOrgCountryData>();
                    //        NativeOrganizationOrgCountryData orgCountryData = new NativeOrganizationOrgCountryData();
                    //        orgCountryData.ActionSpecified = true;
                    //        orgCountryData.Action = NativeOrganization.Action.INSERT;
                    //        orgCountryData.EXApprovedOrMajorExporter = "NO";
                    //        orgCountryData.EXApprovalNumber = organization.tsaValidationId;
                    //        orgCountryData.EXExportPermissionDetails = organization.tsaType;
                    //        orgCountryData.EXSiteInspectionDate = DateTime.ParseExact(organization.locationVerifiedDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTmm:ss:ff");

                    //        NativeOrganizationOrgCountryDataClientCountryRelation clientCountryRelation = new NativeOrganizationOrgCountryDataClientCountryRelation();
                    //        clientCountryRelation.TableName = "RefCountry";
                    //        clientCountryRelation.Code = organization.countryCode;
                    //        orgCountryData.ClientCountryRelation = clientCountryRelation;

                    //        string? approvedLocation = organization.address1?.Substring(0, Math.Min(organization.address1.Length, 24));
                    //        NativeOrganizationOrgCountryDataApprovedLocation dataApprovedLocation = new NativeOrganizationOrgCountryDataApprovedLocation();
                    //        dataApprovedLocation.TableName = "OrgAddress";
                    //        dataApprovedLocation.Code = approvedLocation;
                    //        orgCountryData.ApprovedLocation = dataApprovedLocation;

                    //        NativeOrganizationOrgCountryDataIssuingAuthorityCountry authorityCountry = new NativeOrganizationOrgCountryDataIssuingAuthorityCountry();
                    //        authorityCountry.TableName = "RefCountry";
                    //        authorityCountry.Code = organization.countryCode;
                    //        orgCountryData.IssuingAuthorityCountry = authorityCountry;

                    //        orgCountryDatas.Add(orgCountryData);
                    //        organizationData.OrgHeader.OrgCountryDataCollection = orgCountryDatas.ToArray();
                    //    }

                    //}
                    #endregion

                    #region ORGANIZATION COMPANY DATA                      
                    if (organizationData.OrgHeader.OrgCompanyDataCollection is not null)
                    {
                        var filterOrgCompanyData = organizationData.OrgHeader.OrgCompanyDataCollection.FirstOrDefault(x => x.GlbCompany.Code == cwCompanyCode);
                        if (filterOrgCompanyData != null)
                        {
                            filterOrgCompanyData.ActionSpecified = true;
                            filterOrgCompanyData.Action = NativeOrganization.Action.UPDATE;

                            if (string.IsNullOrEmpty(filterOrgCompanyData.ControllingBranch?.Code))
                            {
                                NativeOrganizationOrgCompanyDataControllingBranch branch = new NativeOrganizationOrgCompanyDataControllingBranch();
                                branch.Code = cwBranchCode;
                                filterOrgCompanyData.ControllingBranch = branch;
                            }
                            else
                            {
                                filterOrgCompanyData.ControllingBranch.Code = cwBranchCode;
                                filterOrgCompanyData.ControllingBranch.PK = null;
                            }

                            if (!String.IsNullOrEmpty(organization.RiskCodeDescription) || !String.IsNullOrEmpty(organization.preferredCurrency))
                            {
                                filterOrgCompanyData.IsDebtorSpecified = true;
                                filterOrgCompanyData.IsDebtor = true;
                                if (!String.IsNullOrEmpty(organization.RiskCodeDescription))
                                {
                                    var riskCodeDescription = _context.riskCodeDescriptions.Where(r => r.BrinksCode.ToLower() == organization.RiskCodeDescription.ToLower()).FirstOrDefault();
                                    if (riskCodeDescription == null)
                                    {
                                        dataResponse.Status = "ERROR";
                                        dataResponse.Message = "Not a valid Risk Code Description '" + organization.RiskCodeDescription + "' in DB.";
                                        return Ok(dataResponse);
                                    }
                                    filterOrgCompanyData.ARCreditRating = riskCodeDescription.CWCode;
                                }
                                if (!String.IsNullOrEmpty(organization.preferredCurrency))
                                {
                                    filterOrgCompanyData.ARDDefltCurrency.Code = organization.preferredCurrency;
                                    filterOrgCompanyData.ARDDefltCurrency.PK = null;
                                    filterOrgCompanyData.ARDDefltCurrency.Action = NativeOrganization.Action.UPDATE;
                                }

                            }

                        }
                        else
                        {
                            List<NativeOrganizationOrgCompanyData> orgCompanyDatas = new List<NativeOrganizationOrgCompanyData>();
                            NativeOrganizationOrgCompanyData orgCompanyData = new NativeOrganizationOrgCompanyData();
                            orgCompanyData.ActionSpecified = true;
                            orgCompanyData.Action = NativeOrganization.Action.INSERT;

                            if (String.IsNullOrEmpty(cwCompanyCode))
                            {
                                dataResponse.Status = "ERROR";
                                dataResponse.Message = "Not a valid company code in DB. Please check the site code or the country code.";
                                return Ok(dataResponse);
                            }
                            NativeOrganizationOrgCompanyDataGlbCompany company = new NativeOrganizationOrgCompanyDataGlbCompany();
                            company.Code = cwCompanyCode;
                            orgCompanyData.GlbCompany = company;

                            NativeOrganizationOrgCompanyDataControllingBranch branch = new NativeOrganizationOrgCompanyDataControllingBranch();
                            branch.Code = cwBranchCode;
                            orgCompanyData.ControllingBranch = branch;


                            if (!String.IsNullOrEmpty(organization.RiskCodeDescription) || !String.IsNullOrEmpty(organization.preferredCurrency))
                            {
                                orgCompanyData.IsDebtorSpecified = true;
                                orgCompanyData.IsDebtor = true;

                                NativeOrganizationOrgCompanyDataARDebtorGroup debtorGroup = new NativeOrganizationOrgCompanyDataARDebtorGroup();
                                debtorGroup.TableName = "OrgDebtorGroup";
                                debtorGroup.Code = "TPY";
                                orgCompanyData.ARDebtorGroup = debtorGroup;
                                if (!String.IsNullOrEmpty(organization.RiskCodeDescription))
                                {
                                    var riskCodeDescription = _context.riskCodeDescriptions.Where(r => r.BrinksCode.ToLower() == organization.RiskCodeDescription.ToLower()).FirstOrDefault();
                                    if (riskCodeDescription == null)
                                    {
                                        dataResponse.Status = "ERROR";
                                        dataResponse.Message = "Not a valid Risk Code Description '" + organization.RiskCodeDescription + "' in DB.";
                                        return Ok(dataResponse);
                                    }
                                    orgCompanyData.ARCreditRating = riskCodeDescription.CWCode;
                                }

                                if (!String.IsNullOrEmpty(organization.preferredCurrency))
                                {
                                    NativeOrganizationOrgCompanyDataARDDefltCurrency arDefltCurrency = new NativeOrganizationOrgCompanyDataARDDefltCurrency();
                                    arDefltCurrency.TableName = "RefCurrency";
                                    arDefltCurrency.Code = organization.preferredCurrency;
                                    orgCompanyData.ARDDefltCurrency = arDefltCurrency;
                                }
                            }


                            orgCompanyDatas.Add(orgCompanyData);
                            organizationData.OrgHeader.OrgCompanyDataCollection = orgCompanyDatas.ToArray();
                        }
                    }
                    #endregion

                    #region CONTACTS
                    List<NativeOrganizationOrgContact> contacts = new List<NativeOrganizationOrgContact>();
                    if (!string.IsNullOrEmpty(organization.billingAttention))
                    {
                        NativeOrganizationOrgContact billingContact = new NativeOrganizationOrgContact();
                        if (organizationData.OrgHeader.OrgContactCollection is not null)
                        {
                            var filteredBillingContact = organizationData.OrgHeader.OrgContactCollection.Where(bc => bc.Title == "Billing Attention").FirstOrDefault();
                            if (filteredBillingContact != null)
                            {
                                filteredBillingContact.ActionSpecified = true;
                                filteredBillingContact.Action = NativeOrganization.Action.UPDATE;
                                filteredBillingContact.ContactName = organization.billingAttention;
                                billingContact.Email = organization.einvoiceEmailAddress;
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
                                billingContact.Email = organization.einvoiceEmailAddress;
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
                            billingContact.Email = organization.einvoiceEmailAddress;
                            contacts.Add(billingContact);
                        }
                    }

                    if (!string.IsNullOrEmpty(organization.contactName))
                    {
                        NativeOrganizationOrgContact contactName = new NativeOrganizationOrgContact();
                        if (organizationData.OrgHeader.OrgContactCollection is not null)
                        {
                            var filteredContactName = organizationData.OrgHeader.OrgContactCollection.Where(bc => bc.Title == "Contact Name").FirstOrDefault();
                            if (filteredContactName != null)
                            {
                                filteredContactName.ActionSpecified = true;
                                filteredContactName.Action = NativeOrganization.Action.UPDATE;
                                filteredContactName.ContactName = organization.contactName;
                                filteredContactName.Email = organization.emailAddress;
                                filteredContactName.Phone = organization.phoneNumber;
                                filteredContactName.Mobile = organization.mobileNumber;
                                filteredContactName.Fax = organization.faxNumber;
                                contacts.Add(filteredContactName);
                            }
                            else
                            {
                                contactName.ActionSpecified = true;
                                contactName.Action = NativeOrganization.Action.INSERT;
                                contactName.Language = "EN";
                                contactName.Title = "Contact Name";
                                contactName.NotifyMode = "DND";
                                contactName.ContactName = organization.contactName;
                                contactName.Email = organization.emailAddress;
                                contactName.Phone = organization.phoneNumber;
                                contactName.Mobile = organization.mobileNumber;
                                contactName.Fax = organization.faxNumber;
                                contacts.Add(contactName);
                            }
                        }
                        else
                        {
                            contactName.ActionSpecified = true;
                            contactName.Action = NativeOrganization.Action.INSERT;
                            contactName.Language = "EN";
                            contactName.Title = "Contact Name";
                            contactName.NotifyMode = "DND";
                            contactName.ContactName = organization.contactName;
                            contactName.Email = organization.emailAddress;
                            contactName.Phone = organization.phoneNumber;
                            contactName.Mobile = organization.mobileNumber;
                            contactName.Fax = organization.faxNumber;
                            contacts.Add(contactName);
                        }

                    }
                    organizationData.OrgHeader.OrgContactCollection = contacts.ToArray();

                    #endregion

                    #region STAFF
                    if (organization.accountOwner != null)
                    {
                        List<NativeOrganizationOrgStaffAssignments> staffAssignments = new List<NativeOrganizationOrgStaffAssignments>();
                        NativeOrganizationOrgStaffAssignments staffAssignment = new NativeOrganizationOrgStaffAssignments();
                        if (organizationData.OrgHeader.OrgStaffAssignmentsCollection is not null)
                        {
                            var filteredOwnerContact = organizationData.OrgHeader.OrgStaffAssignmentsCollection.Where(s => s.Role == "ACT").FirstOrDefault();
                            if (filteredOwnerContact != null)
                            {
                                filteredOwnerContact.ActionSpecified = true;
                                filteredOwnerContact.Action = NativeOrganization.Action.UPDATE;
                                filteredOwnerContact.PK = filteredOwnerContact.PK;
                                NativeOrganizationOrgStaffAssignmentsPersonResponsible accountOwner = new NativeOrganizationOrgStaffAssignmentsPersonResponsible();
                                accountOwner.Code = organization.accountOwner;
                                filteredOwnerContact.PersonResponsible = accountOwner;
                                staffAssignments.Add(filteredOwnerContact);
                            }
                            else
                            {
                                staffAssignment.ActionSpecified = true;
                                staffAssignment.Action = NativeOrganization.Action.INSERT;
                                NativeOrganizationOrgStaffAssignmentsPersonResponsible accountOwner = new NativeOrganizationOrgStaffAssignmentsPersonResponsible();
                                accountOwner.Code = organization.accountOwner;
                                staffAssignment.PersonResponsible = accountOwner;
                                staffAssignment.Role = "ACT";
                                staffAssignment.Department = "ALL";
                                staffAssignments.Add(staffAssignment);
                            }
                        }
                        else
                        {
                            staffAssignment.ActionSpecified = true;
                            staffAssignment.Action = NativeOrganization.Action.INSERT;
                            NativeOrganizationOrgStaffAssignmentsPersonResponsible accountOwner = new NativeOrganizationOrgStaffAssignmentsPersonResponsible();
                            accountOwner.Code = organization.accountOwner;
                            staffAssignment.PersonResponsible = accountOwner;
                            staffAssignment.Role = "ACT";
                            staffAssignment.Department = "ALL";
                            staffAssignments.Add(staffAssignment);
                        }

                    } 
                    #endregion

                    #region REGISTRATION

                    if (organizationData.OrgHeader.OrgCusCodeCollection is not null)
                    {
                        List<NativeOrganizationOrgCusCode> registrationCusCodes = new List<NativeOrganizationOrgCusCode>();
                        //if (organization.taxId != null)
                        //{
                        //    var filterTaxRegistrationCusCode = organizationData.OrgHeader.OrgCusCodeCollection.Where(cus => cus.CodeType == "VAT").FirstOrDefault();
                        //    if (filterTaxRegistrationCusCode != null)
                        //    {
                        //        filterTaxRegistrationCusCode.ActionSpecified = true;
                        //        filterTaxRegistrationCusCode.Action = NativeOrganization.Action.UPDATE;
                        //        filterTaxRegistrationCusCode.PK = filterTaxRegistrationCusCode.PK;
                        //        filterTaxRegistrationCusCode.CustomsRegNo = organization.taxId;
                        //        registrationCusCodes.Add(filterTaxRegistrationCusCode);
                        //    }
                        //    else
                        //    {
                        //        NativeOrganizationOrgCusCodeCodeCountry cusCodeCountry = new NativeOrganizationOrgCusCodeCodeCountry();
                        //        cusCodeCountry.Code = organization.countryCode;
                        //        NativeOrganizationOrgCusCode taxRegistrationCusCode = new NativeOrganizationOrgCusCode();
                        //        taxRegistrationCusCode.ActionSpecified = true;
                        //        taxRegistrationCusCode.Action = NativeOrganization.Action.INSERT;
                        //        taxRegistrationCusCode.CustomsRegNo = organization.taxId;
                        //        taxRegistrationCusCode.CodeType = "VAT";
                        //        taxRegistrationCusCode.CodeCountry = cusCodeCountry;
                        //        registrationCusCodes.Add(taxRegistrationCusCode);
                        //        //organizationData.OrgHeader.OrgCusCodeCollection = registrationCusCodes.ToArray();
                        //    }
                        //}
                        //if (organization.arAccountNumber != null)
                        //{
                        //    var filterArRegistrationCusCode = organizationData.OrgHeader.OrgCusCodeCollection.Where(cus => cus.CodeType == "ECR").FirstOrDefault();
                        //    if (filterArRegistrationCusCode != null)
                        //    {
                        //        filterArRegistrationCusCode.ActionSpecified = true;
                        //        filterArRegistrationCusCode.Action = NativeOrganization.Action.UPDATE;
                        //        filterArRegistrationCusCode.PK = filterArRegistrationCusCode.PK;
                        //        filterArRegistrationCusCode.CustomsRegNo = organization.arAccountNumber;
                        //        registrationCusCodes.Add(filterArRegistrationCusCode);
                        //    }
                        //    else
                        //    {
                        //        NativeOrganizationOrgCusCodeCodeCountry cusCodeCountry = new NativeOrganizationOrgCusCodeCodeCountry();
                        //        cusCodeCountry.Code = organization.countryCode;
                        //        NativeOrganizationOrgCusCode arRegistrationCusCode = new NativeOrganizationOrgCusCode();
                        //        arRegistrationCusCode.ActionSpecified = true;
                        //        arRegistrationCusCode.Action = NativeOrganization.Action.INSERT;
                        //        arRegistrationCusCode.CustomsRegNo = organization.arAccountNumber;
                        //        arRegistrationCusCode.CodeType = "ECR";
                        //        arRegistrationCusCode.CodeCountry = cusCodeCountry;
                        //        registrationCusCodes.Add(arRegistrationCusCode);

                        //    }
                        //}
                        //if (organization.apAccountNumber != null)
                        //{
                        //    var filterApRegistrationCusCode = organizationData.OrgHeader.OrgCusCodeCollection.Where(cus => cus.CodeType == "EDR").FirstOrDefault();
                        //    if (filterApRegistrationCusCode != null)
                        //    {
                        //        filterApRegistrationCusCode.ActionSpecified = true;
                        //        filterApRegistrationCusCode.Action = NativeOrganization.Action.UPDATE;
                        //        filterApRegistrationCusCode.PK = filterApRegistrationCusCode.PK;
                        //        filterApRegistrationCusCode.CustomsRegNo = organization.apAccountNumber;
                        //        registrationCusCodes.Add(filterApRegistrationCusCode);
                        //    }
                        //    else
                        //    {
                        //        NativeOrganizationOrgCusCodeCodeCountry cusCodeCountry = new NativeOrganizationOrgCusCodeCodeCountry();
                        //        cusCodeCountry.Code = organization.countryCode;
                        //        NativeOrganizationOrgCusCode apRegistrationCusCode = new NativeOrganizationOrgCusCode();
                        //        apRegistrationCusCode.ActionSpecified = true;
                        //        apRegistrationCusCode.Action = NativeOrganization.Action.INSERT;
                        //        apRegistrationCusCode.CustomsRegNo = organization.apAccountNumber;
                        //        apRegistrationCusCode.CodeType = "EDR";
                        //        apRegistrationCusCode.CodeCountry = cusCodeCountry;
                        //        registrationCusCodes.Add(apRegistrationCusCode);

                        //    }
                        //}
                        if (organization.eoriNumber != null)
                        {
                            var filterEORRegistrationCusCode = organizationData.OrgHeader.OrgCusCodeCollection.Where(cus => cus.CodeType == "EOR").FirstOrDefault();
                            if (filterEORRegistrationCusCode != null)
                            {
                                filterEORRegistrationCusCode.ActionSpecified = true;
                                filterEORRegistrationCusCode.Action = NativeOrganization.Action.UPDATE;
                                filterEORRegistrationCusCode.PK = filterEORRegistrationCusCode.PK;
                                filterEORRegistrationCusCode.CustomsRegNo = organization.eoriNumber;
                                registrationCusCodes.Add(filterEORRegistrationCusCode);
                            }
                            else
                            {
                                NativeOrganizationOrgCusCodeCodeCountry cusCodeCountry = new NativeOrganizationOrgCusCodeCodeCountry();
                                cusCodeCountry.Code = organization.countryCode;
                                NativeOrganizationOrgCusCode eorRegistrationCusCode = new NativeOrganizationOrgCusCode();
                                eorRegistrationCusCode.ActionSpecified = true;
                                eorRegistrationCusCode.Action = NativeOrganization.Action.INSERT;
                                eorRegistrationCusCode.CustomsRegNo = organization.eoriNumber;
                                eorRegistrationCusCode.CodeType = "EOR";
                                eorRegistrationCusCode.CodeCountry = cusCodeCountry;
                                registrationCusCodes.Add(eorRegistrationCusCode);

                            }
                        }
                        organizationData.OrgHeader.OrgCusCodeCollection = registrationCusCodes.ToArray();
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
                    //if (organization.invoiceGlobalCustomerCode != null)
                    //{
                    //    OrganizationData invoiceOrganizationData = SearchOrgWithRegNo(organization.invoiceGlobalCustomerCode);
                    //    if (invoiceOrganizationData.OrgHeader != null)
                    //    {
                    //        //List<NativeOrganizationOrgRelatedParty> relatedParties = new List<NativeOrganizationOrgRelatedParty>();
                    //        NativeOrganizationOrgRelatedParty relatedParty = new NativeOrganizationOrgRelatedParty();
                    //        if (organizationData.OrgHeader.OrgRelatedPartyCollection is not null)
                    //        {
                    //            var filteredOrganizationRelatedData = organizationData.OrgHeader.OrgRelatedPartyCollection.Where(orp => orp.PartyType == "LFW").FirstOrDefault();
                    //            if (filteredOrganizationRelatedData != null)
                    //            {
                    //                filteredOrganizationRelatedData.ActionSpecified = true;
                    //                filteredOrganizationRelatedData.Action = NativeOrganization.Action.UPDATE;
                    //                filteredOrganizationRelatedData.RelatedParty.ActionSpecified = true;
                    //                filteredOrganizationRelatedData.RelatedParty.Action = NativeOrganization.Action.UPDATE;
                    //                filteredOrganizationRelatedData.RelatedParty.PK = invoiceOrganizationData.OrgHeader.PK;
                    //                filteredOrganizationRelatedData.RelatedParty.Code = invoiceOrganizationData.OrgHeader.Code;
                    //                relatedParties.Add(filteredOrganizationRelatedData);

                    //            }
                    //            else
                    //            {
                    //                relatedParty.ActionSpecified = true;
                    //                relatedParty.Action = NativeOrganization.Action.INSERT;
                    //                relatedParty.PartyType = "LFW";
                    //                relatedParty.FreightTransportMode = "ALL";
                    //                relatedParty.FreightDirection = "PAD";
                    //                NativeOrganizationOrgRelatedPartyRelatedParty relatedPartyCode = new NativeOrganizationOrgRelatedPartyRelatedParty();
                    //                relatedPartyCode.PK = invoiceOrganizationData.OrgHeader.PK;
                    //                relatedPartyCode.Code = organization.invoiceGlobalCustomerCode;
                    //                relatedParty.RelatedParty = relatedPartyCode;
                    //                relatedParties.Add(relatedParty);
                    //                //organizationData.OrgHeader.OrgRelatedPartyCollection = relatedParties.ToArray();
                    //            }
                    //        }
                    //        else
                    //        {
                    //            relatedParty.ActionSpecified = true;
                    //            relatedParty.Action = NativeOrganization.Action.INSERT;
                    //            relatedParty.PartyType = "LFW";
                    //            relatedParty.FreightTransportMode = "ALL";
                    //            relatedParty.FreightDirection = "PAD";
                    //            NativeOrganizationOrgRelatedPartyRelatedParty relatedPartyCode = new NativeOrganizationOrgRelatedPartyRelatedParty();
                    //            relatedPartyCode.PK = invoiceOrganizationData.OrgHeader.PK;
                    //            relatedPartyCode.Code = organization.invoiceGlobalCustomerCode;
                    //            relatedParty.RelatedParty = relatedPartyCode;
                    //            relatedParties.Add(relatedParty);
                    //            //organizationData.OrgHeader.OrgRelatedPartyCollection = relatedParties.ToArray();
                    //        }

                    //    }
                    //    else
                    //    {
                    //        dataResponse.Status = "ERROR";
                    //        dataResponse.Message = "The invoice global customer code " + organization.invoiceGlobalCustomerCode + " not found in CW.";
                    //        return Ok(dataResponse);
                    //    }
                    //}
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
                    if (organization.siteCode != null)
                    {
                        NativeOrganizationStmNote note = new NativeOrganizationStmNote();
                        if (organizationData.OrgHeader.StmNoteCollection is not null)
                        {
                            var filteredOrganizationNote = organizationData.OrgHeader.StmNoteCollection.Where(on => on.Description == "Site Code").FirstOrDefault();
                            if (filteredOrganizationNote != null)
                            {
                                filteredOrganizationNote.ActionSpecified = true;
                                filteredOrganizationNote.Action = NativeOrganization.Action.UPDATE;
                                filteredOrganizationNote.NoteText = organization.siteCode;
                                notes.Add(filteredOrganizationNote);
                            }
                            else
                            {
                                note.ActionSpecified = true;
                                note.Action = NativeOrganization.Action.INSERT;
                                //note.NoteContext = "A";
                                note.IsCustomDescriptionSpecified = true;
                                note.IsCustomDescription = true;
                                note.ForceRead = true;
                                note.NoteType = "PUB";
                                note.Description = "Site Code";
                                note.NoteText = organization.siteCode;
                                notes.Add(note);
                            }
                        }
                        else
                        {
                            note.ActionSpecified = true;
                            note.Action = NativeOrganization.Action.INSERT;
                            //note.NoteContext = "A";
                            note.IsCustomDescriptionSpecified = true;
                            note.IsCustomDescription = true;
                            note.ForceRead = true;
                            note.NoteType = "PUB";
                            note.Description = "Site Code";
                            note.NoteText = organization.siteCode;
                            notes.Add(note);
                        }
                    }
                    if (organization.kycApprovedBy != null)
                    {
                        Dictionary<string, string> kycDict = new Dictionary<string, string>();
                        kycDict.Add("kycCreatedPrior2018", organization.kycCreatedPrior2018.ToString());
                        kycDict.Add("kycOpenProcCompleted", organization.kycOpenProcCompleted.ToString());
                        kycDict.Add("kycRefNbr", organization.kycRefNbr);
                        kycDict.Add("kycVerifDate", organization.kycVerifDate);
                        kycDict.Add("kycApprovedBy", organization.kycApprovedBy);
                        kycDict.Add("kycOpeningStation", organization.kycOpeningStation);

                        string kycNotes = "";
                        foreach (KeyValuePair<string, string> entry in kycDict)
                            if (entry.Value != null)
                                kycNotes += String.Format("{0} : {1}\n", entry.Key, entry.Value);

                        NativeOrganizationStmNote note = new NativeOrganizationStmNote();
                        if (organizationData.OrgHeader.StmNoteCollection is not null)
                        {
                            var filteredOrganizationKycNote = organizationData.OrgHeader.StmNoteCollection.Where(on => on.Description == "Additional Information").FirstOrDefault();
                            if (filteredOrganizationKycNote is not null)
                            {
                                filteredOrganizationKycNote.ActionSpecified = true;
                                filteredOrganizationKycNote.Action = NativeOrganization.Action.UPDATE;
                                filteredOrganizationKycNote.NoteText = kycNotes;
                                notes.Add(filteredOrganizationKycNote);
                            }
                            else
                            {
                                note.ActionSpecified = true;
                                note.Action = NativeOrganization.Action.INSERT;
                                note.IsCustomDescription = false;
                                note.ForceRead = true;
                                note.NoteType = "PUB";
                                note.Description = "Additional Information";
                                note.NoteText = kycNotes;
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
                            note.Description = "Additional Information";
                            note.NoteText = kycNotes;
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
                    organizationData.OrgHeader.OrgAddressCollection[0].ActionSpecified = true;
                    organizationData.OrgHeader.OrgAddressCollection[0].Action = NativeOrganization.Action.UPDATE;
                    organizationData.OrgHeader.OrgAddressCollection[0].Code = organization.address1?.Substring(0, Math.Min(organization.address1.Length, 24));
                    organizationData.OrgHeader.OrgAddressCollection[0].Address1 = organization.address1;
                    organizationData.OrgHeader.OrgAddressCollection[0].Address2 = organization.address2;

                    List<NativeOrganizationOrgAddressOrgAddressAdditionalInfo> additionalInfoAddresses = new List<NativeOrganizationOrgAddressOrgAddressAdditionalInfo>();
                    if (!String.IsNullOrEmpty(organization?.address3))
                    {
                        organizationData.OrgHeader.OrgAddressCollection[0].AdditionalAddressInformation = organization?.address3;
                        if (organizationData.OrgHeader.OrgAddressCollection[0]?.OrgAddressAdditionalInfoCollection?.Count() >= 1)
                        {
                            organizationData.OrgHeader.OrgAddressCollection[0].OrgAddressAdditionalInfoCollection[0].ActionSpecified = !String.IsNullOrEmpty(organization.address3);
                            organizationData.OrgHeader.OrgAddressCollection[0].OrgAddressAdditionalInfoCollection[0].Action = NativeOrganization.Action.UPDATE;
                            organizationData.OrgHeader.OrgAddressCollection[0].OrgAddressAdditionalInfoCollection[0].AdditionalInfo = organization.address3;
                        }
                        else
                        {
                            NativeOrganizationOrgAddressOrgAddressAdditionalInfo additionalInfoAddress3 = new NativeOrganizationOrgAddressOrgAddressAdditionalInfo();
                            additionalInfoAddress3.ActionSpecified = true;
                            additionalInfoAddress3.Action = NativeOrganization.Action.INSERT;
                            additionalInfoAddress3.IsPrimarySpecified = true;
                            additionalInfoAddress3.IsPrimary = true;
                            additionalInfoAddress3.AdditionalInfo = organization?.address3;
                            additionalInfoAddresses.Add(additionalInfoAddress3);
                        }

                    }
                    if(!String.IsNullOrEmpty(organization?.address4))
                    {
                        if (organizationData.OrgHeader.OrgAddressCollection[0]?.OrgAddressAdditionalInfoCollection?.Count() >= 2)
                        {
                            organizationData.OrgHeader.OrgAddressCollection[0].OrgAddressAdditionalInfoCollection[1].ActionSpecified = !String.IsNullOrEmpty(organization.address4);
                            organizationData.OrgHeader.OrgAddressCollection[0].OrgAddressAdditionalInfoCollection[1].Action = NativeOrganization.Action.UPDATE;
                            organizationData.OrgHeader.OrgAddressCollection[0].OrgAddressAdditionalInfoCollection[1].AdditionalInfo = organization.address4;
                        }

                        else
                        {
                            NativeOrganizationOrgAddressOrgAddressAdditionalInfo additionalInfoAddress4 = new NativeOrganizationOrgAddressOrgAddressAdditionalInfo();
                            additionalInfoAddress4.ActionSpecified = true;
                            additionalInfoAddress4.Action = NativeOrganization.Action.INSERT;
                            additionalInfoAddress4.AdditionalInfo = organization?.address4;
                            additionalInfoAddresses.Add(additionalInfoAddress4);
                        }
                        organizationData.OrgHeader.OrgAddressCollection[0].OrgAddressAdditionalInfoCollection = additionalInfoAddresses.ToArray();
                }

                    organizationData.OrgHeader.OrgAddressCollection[0].RelatedPortCode.ActionSpecified = true;
                    organizationData.OrgHeader.OrgAddressCollection[0].RelatedPortCode.Action = NativeOrganization.Action.UPDATE;
                    organizationData.OrgHeader.OrgAddressCollection[0].RelatedPortCode.PK = organizationData.OrgHeader.ClosestPort.PK;
                    organizationData.OrgHeader.OrgAddressCollection[0].RelatedPortCode.Code = organizationData.OrgHeader.ClosestPort.Code;

                    organizationData.OrgHeader.OrgAddressCollection[0].City = organization.city;
                    organizationData.OrgHeader.OrgAddressCollection[0].PostCode = organization.postalCode;
                    organizationData.OrgHeader.OrgAddressCollection[0].State = organization.provinceCode;
                    organizationData.OrgHeader.OrgAddressCollection[0].CountryCode.Code = countryCode;
                    organizationData.OrgHeader.OrgAddressCollection[0].Phone = organization.phoneNumber;
                    organizationData.OrgHeader.OrgAddressCollection[0].Mobile = organization.mobileNumber;
                    organizationData.OrgHeader.OrgAddressCollection[0].Fax = organization.faxNumber;
                    organizationData.OrgHeader.OrgAddressCollection[0].Email = organization.emailAddress;

                    #endregion

                    #region DOCUMENT VALUES
                    Dictionary<string, string> customValues = new Dictionary<string, string>();
                    //customValues.Add("locationVerifiedDate", organization.locationVerifiedDate);
                    customValues.Add("Lob", organization.lob);
                    //customValues.Add("adyenPay", organization.adyenPay.ToString());
                    //customValues.Add("adyenPayPreference", organization.adyenPayPreference);
                    //customValues.Add("adyenTokenId", organization.adyenTokenId);
                    //customValues.Add("adyenPayByLinkId", organization.adyenPayByLinkId);
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
                var documentResponse = eAdaptor.Services.SendToCargowise2(xml, _configuration.URI, _configuration.Username, _configuration.Password);
                if (documentResponse.Status == "ERROR")
                {
                    //string errorMessage = documentResponse.Data.Data.FirstChild.InnerText;
                    string? errorMessage = documentResponse.Message;
                    dataResponse.Status = documentResponse.Status;
                    MatchCollection matchedError = Regex.Matches(errorMessage, "(Error)(.*)");
                    string[] groupedErrors = matchedError.GroupBy(x => x.Value).Select(y => y.Key).ToArray();
                    dataResponse.Message = string.Join(",", groupedErrors);
                    _logger.LogError("Error: {@Error} Request: {@Request}", dataResponse.Message, organization);
                    return Ok(dataResponse);
                }
                _logger.LogInformation("Success: {@Success} Request: {@Request}", dataResponse.Message, organization);
                dataResponse.Status = "SUCCESS";
                dataResponse.Message = successMessage;
                return Ok(dataResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error: {@Error} Request: {@Request}", ex.Message, organization);
                dataResponse.Status = "ERROR";
                dataResponse.Message = ex.Message;
                return Ok(dataResponse);
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
                if (documentResponse.Status == "SUCCESS" && documentResponse.Data.Status == "PRS" && documentResponse.Data.Data != null && documentResponse.Data.Data.Name == "Native")
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

        public static bool HasPropertyOne(Type obj, string propertyName)
        {
            return obj.GetProperty(propertyName) != null;
        }
    }
}