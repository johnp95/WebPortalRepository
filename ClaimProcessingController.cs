using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using ClaimProcessingRepository.SQL;
using Infinedi.WebPortal.Areas.ClaimsProcessing;
using Infinedi.Domain.ViewModels;
using Infinedi.Domain.Models.Claims_Processing;
using System.IO;
using Infinedi.Data.Processing.Entities;
using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Infinedi.WebPortal.Areas.ClaimsProcessing.Controllers
{
    public class ClaimsProcessingController : Infinedi.WebPortal.Controllers.RestrictedController
    {
        ClaimsProcessingViewModel claimsviewmodel;
        ClaimProcessing.Interfaces.IClaimProcessingRepository claimProcessingRepository = new ClaimProcessingRepository.SQL.ClaimProcessingRepository();

        private static int sessiontracereadcount = 0;

        public ClaimsProcessingController()
        {
            claimsviewmodel = new ClaimsProcessingViewModel();

            claimsviewmodel.LoggedInSecurityUserID = Infinedi.AppServices.SessionManager.GetUserId();
            claimsviewmodel.InputSelectedAction = "Bulk action";
            claimsviewmodel.CurrentPageNumber = 1;
            claimsviewmodel.perpagedropdownvalue = "10 per page";
        }

        [HttpPost]
        public void Download277(string FileTypeDescription, string Message277, string Lights277, DateTime? ProcessedDateTime, string ReceivedFileName, DateTime? CreationDate, string AdditionalData, DateTime? InsertDate)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();
                doc.AddTitle("Infinedi Claim report");
                List<Chunk> listofchunks = new List<Chunk>();
                List<Paragraph> listofparagraphs = new List<Paragraph>();

                Chunk c1 = new Chunk("FileType");
                Paragraph p1 = new Paragraph(FileTypeDescription);
                listofparagraphs.Add(p1);
                listofchunks.Add(c1);

                Chunk c2 = new Chunk("Creation Date");
                Paragraph p2 = new Paragraph(CreationDate.ToString());
                listofparagraphs.Add(p2);
                listofchunks.Add(c2);

                Chunk c3 = new Chunk("Received File Name");
                Paragraph p3 = new Paragraph(ReceivedFileName);
                listofparagraphs.Add(p3);
                listofchunks.Add(c3);

                Chunk c4 = new Chunk("Message");
                Paragraph p4 = new Paragraph(HttpUtility.UrlDecode(Message277));
                listofparagraphs.Add(p4);
                listofchunks.Add(c4);

                for (int i = 0; i < listofchunks.Count; i++)
                {
                    doc.Add(listofchunks[i]);
                    doc.Add(listofparagraphs[i]);
                    doc.Add(Chunk.NEWLINE);
                }

                doc.Close();
                writer.Close();
                Response.ContentType = "pdf/application";
                Response.AddHeader("content-disposition",
                "attachment;filename=Infinedi Claim Report.pdf");
                Response.OutputStream.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
            }
        }

        [HttpGet]
        public void Download277reports(int claimid, int readclaimacceptanceid)
        {
            Claim277s claim277 = new Claim277s();
            claim277 = claimProcessingRepository.GetClaim277Data(readclaimacceptanceid, claimid);

            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();
                doc.AddTitle("Infinedi Claim report");
                List<Chunk> listofchunks = new List<Chunk>();
                List<Paragraph> listofparagraphs = new List<Paragraph>();

                Chunk c1 = new Chunk("FileType :", new Font(Font.FontFamily.TIMES_ROMAN, 12, Font.BOLD, BaseColor.BLUE));
                Paragraph p1 = new Paragraph(claim277.FileTypeDescription);

                listofparagraphs.Add(p1);
                listofchunks.Add(c1);

                Chunk c2 = new Chunk("Creation Date :", new Font(Font.FontFamily.TIMES_ROMAN, 12, Font.BOLD, BaseColor.BLUE));
                Paragraph p2 = new Paragraph(claim277.CreationDate.ToString());
                listofparagraphs.Add(p2);
                listofchunks.Add(c2);

                Chunk c3 = new Chunk("Received File Name :", new Font(Font.FontFamily.TIMES_ROMAN, 12, Font.BOLD, BaseColor.BLUE));
                Paragraph p3 = new Paragraph(claim277.ReceivedFileName);
                listofparagraphs.Add(p3);
                listofchunks.Add(c3);

                Chunk c4 = new Chunk("Message :", new Font(Font.FontFamily.TIMES_ROMAN, 12, Font.BOLD, BaseColor.BLUE));
                Paragraph p4 = new Paragraph(HttpUtility.UrlDecode(claim277.Message277));
                listofparagraphs.Add(p4);
                listofchunks.Add(c4);

                Chunk c5 = new Chunk("Additional Data:", new Font(Font.FontFamily.TIMES_ROMAN, 12, Font.BOLD, BaseColor.BLUE));
                Paragraph p5 = new Paragraph(claim277.AdditionalData);
                listofparagraphs.Add(p5);
                listofchunks.Add(c5);

                for (int i = 0; i < listofchunks.Count; i++)
                {
                    doc.Add(listofchunks[i]);
                    doc.Add(listofparagraphs[i]);
                    doc.Add(Chunk.NEWLINE);
                }

                doc.Close();
                writer.Close();
                Response.ContentType = "pdf/application";
                Response.AddHeader("content-disposition",
                "attachment;filename=InfinediClaimReport.pdf");
                Response.OutputStream.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
            }
        }

        // GET: ClaimsProcessing/ClaimsProcessing
        public ActionResult Index(string param)
        {
            return View();
        }

        public JsonResult GetClaimsData(string selectedclientcode)
        {
            try
            {
                int totalnumberofrecords = 0;
                var tracenumberfrombilling = Session["Tracenumber"];
                var inputProcessedDAteTimeFromBilling = Session["InputProcessedDateFrom"];

                sessiontracereadcount = sessiontracereadcount + 1;
                claimsviewmodel.InputTraceNumberFromBilling = tracenumberfrombilling;
                if (claimsviewmodel.InputTraceNumberFromBilling != null)
                {
                    claimsviewmodel.InputTraceNumber = claimsviewmodel.InputTraceNumberFromBilling.ToString();
                    claimsviewmodel.SearchFromQuickActionPending = false;
                    claimsviewmodel.SearchFromQuickActionAccepted = false;
                    claimsviewmodel.SearchFromQuickActionFailed = false;
                    claimsviewmodel.SearchFromBatchErrored = false;
                    claimsviewmodel.SearchFromBatchClean = false;
                    claimsviewmodel.SearchFromBatchRejected = false;

                    claimsviewmodel.InputProcessedDateFrom = Convert.ToDateTime(inputProcessedDAteTimeFromBilling);
                    claimsviewmodel.InputProcessedDateTo = DateTime.Now;

                    claimsviewmodel.InputIncludePending = true;
                    claimsviewmodel.InputIncludeArchived = true;
                    claimsviewmodel.InputIncludeOnHold = true;
                    claimsviewmodel.InputIncludeAttention = true;
                    claimsviewmodel.InputIncludeIgnore = true;
                    claimsviewmodel.InputIncludeViewed = true;

                    claimsviewmodel.InputIncludeInstitutional = true;
                    claimsviewmodel.InputIncludeProfessional = true;
                    claimsviewmodel.InputIncludeDental = true;
                    claimsviewmodel.InputIncludeAll = true;

                    claimsviewmodel.InputIncludeWorkerCompType = true;
                }
                else
                {
                    // Reset other sources of entry onto claims processing screen
                    claimsviewmodel.SearchFromQuickActionPending = false;
                    claimsviewmodel.SearchFromQuickActionAccepted = false;
                    claimsviewmodel.SearchFromQuickActionFailed = false;
                    claimsviewmodel.SearchFromBatchErrored = false;
                    claimsviewmodel.SearchFromBatchClean = false;
                    claimsviewmodel.SearchFromBatchRejected = false;

                    claimsviewmodel.InputProcessedDateFrom = DateTime.Today.AddDays(-30);
                    claimsviewmodel.InputProcessedDateTo = DateTime.Today.AddHours(23);
                    claimsviewmodel.InputIncludePending = true;
                }

                claimsviewmodel.LoggedInSecurityUserID = AppServices.SessionManager.GetUserId();
                claimsviewmodel.selectedclientcode = selectedclientcode;
                claimsviewmodel.LoggedInClientCode = Domain.Models.InfinediSecurityUserModel.GetClientCode(claimsviewmodel.LoggedInSecurityUserID);

                claimsviewmodel.selectedPerPage = 10;

                claimsviewmodel.Claims = claimProcessingRepository.SearchClaims(claimsviewmodel, out totalnumberofrecords);

                claimsviewmodel.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / claimsviewmodel.selectedPerPage);
                claimsviewmodel.TotalAmountOnPage = ClaimModel.TotalFileAmount;

                claimsviewmodel.InputSelectedAction = "Bulk action";

                if (sessiontracereadcount == 2)
                {
                    Session["Tracenumber"] = null;
                    sessiontracereadcount = 0;
                }

                return Json(claimsviewmodel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult clickedSearch(ClaimsProcessingViewModel vm, String ClientCode, int Pagenumber)
        {
            try
            {
                vm.IsHistoryPage = false;
                vm = NonJsonResult(vm, ClientCode, Pagenumber);
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void ViewClaimsFromBilling(ClaimsProcessingViewModel vm, string clientcode, int pagenumber)
        {
            try
            {
                vm = NonJsonResult(vm, clientcode, pagenumber);
            }
            catch (Exception) { }
        }

        private ClaimsProcessingViewModel NonJsonResult(ClaimsProcessingViewModel vm, string ClientCode, int Pagenumber)
        {
            try
            {
                vm.CurrentPageNumber = Pagenumber;
                int totalnumberofrecords = 0;
                claimsviewmodel.LoggedInSecurityUserID = Infinedi.AppServices.SessionManager.GetUserId();
                vm.Claims = claimProcessingRepository.SearchClaims(vm, out totalnumberofrecords);
                vm.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / vm.selectedPerPage);
                vm.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;
                return vm;
            }
            catch (Exception)
            {
                return null;
            }
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult ClickedAddNote(string Notes, int ClaimID, ClaimsProcessingViewModel vm)
        {
            try
            {
                claimProcessingRepository.AddNewNote(Notes, ClaimID, vm.LoggedInSecurityUserID);
                int totalnumberofrecords = 0;
                vm.Claims = claimProcessingRepository.SearchClaims(vm, out totalnumberofrecords);
                vm.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / vm.selectedPerPage);
                vm.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;

                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult SubmittedInquiry(string TraceNumber, string Department, string Subject, string Body, string LoggedInClientCode, ClaimsProcessingViewModel vm)
        {
            try
            {
                var Securityuserobject = new Infinedi.Security.SecurityUser();

                Securityuserobject = Infinedi.Domain.Models.InfinediSecurityUserModel.GetInfinediSecurityById(Infinedi.AppServices.SessionManager.GetUserId());
                string username = Securityuserobject.UserName;
                string ticketnumber = claimProcessingRepository.SubmittedInquiry(TraceNumber, Department, " TraceNumber:" + TraceNumber + " " + Subject, Body, LoggedInClientCode);
                claimProcessingRepository.InsertInquiryInInbox(ticketnumber, " TraceNumber:" + TraceNumber + " " + Subject, Body, username, Department);

                int totalnumberofrecords = 0;
                vm.Claims = claimProcessingRepository.SearchClaims(vm, out totalnumberofrecords);
                vm.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / vm.selectedPerPage);
                vm.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;
            }
            catch (Exception e)
            {
                Infinedi.Domain.Models.InfinediSecurityUserModel.InsertIntoErrorLog(e.ToString());
            }

            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult ChangedStatus(string TraceNumber, ClaimsProcessingViewModel vm, string Newstatus)
        {
            try
            {
                ChangeStatusInRepository(Newstatus, vm.LoggedInSecurityUserID, vm.LoggedInClientCode, TraceNumber);
                int totalnumberofrecords = 0;
                vm.Claims = claimProcessingRepository.SearchClaims(vm, out totalnumberofrecords);
                vm.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / vm.selectedPerPage);
                vm.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;

                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void ChangeStatusInRepository(string Newstatus, int LoggedInSecurityUserID, string LoggedInClientCode, string TraceNumber)
        {
            try
            {
                claimProcessingRepository.ChangeStatus(Newstatus, LoggedInSecurityUserID, LoggedInClientCode, TraceNumber);
            }
            catch (Exception) { }
        }

        public JsonResult GetERAID(int QEraClaimId)
        {
            try
            {
                int ERAID = claimProcessingRepository.GetEraID(QEraClaimId);

                return Json(ERAID, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public ActionResult WorkerCompEDIPage()
        {
            return View();
        }

        public string FileUpload(HttpPostedFileBase hiddenfiletype, FormCollection fc)
        {
            string defaultErrorMessage = "Error uploading file";

            try
            {
                MemoryStream target = new MemoryStream();
                byte[] data = new byte[0];

                if (hiddenfiletype != null)
                {
                    hiddenfiletype.InputStream.CopyTo(target);
                    int claimID = 0;
                    Int32.TryParse(fc["ClaimID"], out claimID);
                    if (claimID != 0)
                    {
                        data = target.ToArray();
                        return claimProcessingRepository.InsertAttachment(claimID, data, UserId);
                    }
                }

                return defaultErrorMessage;
            }
            catch (Exception)
            {
                return defaultErrorMessage;
            }
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult GetHistory(string TraceNumber, ClaimsProcessingViewModel viewmodel)
        {
            try
            {
                viewmodel.IsHistoryPage = true;
                int numberofrows = 0;
                viewmodel.Claims = claimProcessingRepository.GetClaimHistory(TraceNumber, viewmodel.LoggedInClientCode, out numberofrows, viewmodel.LoggedInSecurityUserID);
                //TO DO pagenation on claim history
                viewmodel.TotalNumberOfPages = 1;
                viewmodel.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;

            }
            catch (Exception e)
            {
                var x = e.ToString();
            }

            return Json(viewmodel);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult BulkTraceNumbers(List<string> TraceNumber, ClaimsProcessingViewModel vm, string Newstatus)
        {
            try
            {
                int totalnumberofrecords = 0;
                for (int k = 0; k < TraceNumber.Count; k++)
                {
                    ChangeStatusInRepository(Newstatus, vm.LoggedInSecurityUserID, vm.LoggedInClientCode, TraceNumber[k]);
                }

                vm.Claims = claimProcessingRepository.SearchClaims(vm, out totalnumberofrecords);
                vm.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / vm.selectedPerPage);
                vm.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;

                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public JsonResult GetRejectedClaimsForInputID(int inputID, string SortOnColumn, string SortOrder, ClaimsProcessingViewModel claimsviewmodel,
                    DateTime inputFileStartDate,
                    DateTime inputFileEndDate)
        {
            try
            {
                if (SortOnColumn == "" && SortOrder == "")
                {
                    claimsviewmodel = new ClaimsProcessingViewModel();
                    claimsviewmodel.InputProcessedDateFrom = inputFileStartDate;
                    claimsviewmodel.InputProcessedDateTo = inputFileStartDate;
                    claimsviewmodel.LoggedInSecurityUserID = Infinedi.AppServices.SessionManager.GetUserId();
                    claimsviewmodel.InputSelectedAction = "Bulk action";
                    claimsviewmodel.CurrentPageNumber = 1;
                    claimsviewmodel.perpagedropdownvalue = "10 per page";
                }

                int totalnumberofrecords = 0;
                int startindex = 0;
                claimsviewmodel.InputInputFileID = inputID;
                claimsviewmodel.SearchFromQuickActionPending = false;
                claimsviewmodel.SearchFromQuickActionAccepted = false;
                claimsviewmodel.SearchFromQuickActionFailed = false;
                claimsviewmodel.SearchFromBatchRejected = true;
                claimsviewmodel.SearchFromBatchErrored = false;
                claimsviewmodel.SearchFromBatchClean = false;

                claimsviewmodel.LoggedInSecurityUserID = Infinedi.AppServices.SessionManager.GetUserId();
                claimsviewmodel.LoggedInClientCode = Infinedi.Domain.Models.InfinediSecurityUserModel.GetClientCode(claimsviewmodel.LoggedInSecurityUserID);
                claimsviewmodel.selectedPerPage = Convert.ToInt16(claimsviewmodel.perPageOptions[0].Substring(0, 2));

                claimsviewmodel.Claims = claimProcessingRepository.GetRejectedClaimsForInputID(inputID, claimsviewmodel.LoggedInClientCode, claimsviewmodel.selectedPerPage, claimsviewmodel.LoggedInSecurityUserID, SortOnColumn, SortOrder, startindex, out totalnumberofrecords);

                claimsviewmodel.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / claimsviewmodel.selectedPerPage);
                claimsviewmodel.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;

                return Json(claimsviewmodel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public JsonResult GetCleanClaimsForInputID(int inputID, string SortOnColumn, String SortOrder, ClaimsProcessingViewModel claimsviewmodel,
                    DateTime inputFileStartDate,
                    DateTime inputFileEndDate)
        {
            try
            {
                if (SortOnColumn == "" && SortOrder == "")
                {
                    claimsviewmodel = new ClaimsProcessingViewModel();
                    claimsviewmodel.InputProcessedDateFrom = inputFileStartDate;
                    claimsviewmodel.InputProcessedDateTo = inputFileStartDate;
                    claimsviewmodel.LoggedInSecurityUserID = Infinedi.AppServices.SessionManager.GetUserId();
                    claimsviewmodel.InputSelectedAction = "Bulk action";
                    claimsviewmodel.CurrentPageNumber = 1;
                    claimsviewmodel.perpagedropdownvalue = "10 per page";
                }

                int totalnumberofrecords = 0;
                int startindex = 0;

                claimsviewmodel.SearchFromQuickActionPending = false;
                claimsviewmodel.SearchFromQuickActionAccepted = false;
                claimsviewmodel.SearchFromQuickActionFailed = false;
                claimsviewmodel.InputInputFileID = inputID;
                claimsviewmodel.SearchFromBatchClean = true;
                claimsviewmodel.SearchFromBatchRejected = false;
                claimsviewmodel.SearchFromBatchErrored = false;

                claimsviewmodel.LoggedInSecurityUserID = Infinedi.AppServices.SessionManager.GetUserId();
                claimsviewmodel.LoggedInClientCode = Infinedi.Domain.Models.InfinediSecurityUserModel.GetClientCode(claimsviewmodel.LoggedInSecurityUserID);
                claimsviewmodel.selectedPerPage = Convert.ToInt16(claimsviewmodel.perPageOptions[0].Substring(0, 2));

                claimsviewmodel.Claims = claimProcessingRepository.GetCleanClaimsForInputID(inputID, claimsviewmodel.LoggedInClientCode, claimsviewmodel.selectedPerPage, claimsviewmodel.LoggedInSecurityUserID, SortOnColumn, SortOrder, startindex, out totalnumberofrecords);

                claimsviewmodel.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / claimsviewmodel.selectedPerPage);
                claimsviewmodel.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;

                return Json(claimsviewmodel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public JsonResult GetErroredClaimsForInputID(int inputID, string SortOnColumn, String SortOrder, ClaimsProcessingViewModel claimsviewmodel,
                    DateTime inputFileStartDate,
                    DateTime inputFileEndDate)
        {
            try
            {
                int totalnumberofrecords = 0;
                int startindex = 0;

                if (SortOnColumn == "" && SortOrder == "")
                {
                    claimsviewmodel = new ClaimsProcessingViewModel();
                    claimsviewmodel.InputProcessedDateFrom = inputFileStartDate;
                    claimsviewmodel.InputProcessedDateTo = inputFileStartDate;
                    claimsviewmodel.LoggedInSecurityUserID = Infinedi.AppServices.SessionManager.GetUserId();
                    claimsviewmodel.InputSelectedAction = "Bulk action";
                    claimsviewmodel.CurrentPageNumber = 1;
                    claimsviewmodel.perpagedropdownvalue = "10 per page";
                }

                claimsviewmodel.SearchFromQuickActionPending = false;
                claimsviewmodel.SearchFromQuickActionAccepted = false;
                claimsviewmodel.SearchFromQuickActionFailed = false;
                claimsviewmodel.InputInputFileID = inputID;
                claimsviewmodel.SearchFromBatchErrored = true;
                claimsviewmodel.SearchFromBatchClean = false;
                claimsviewmodel.SearchFromBatchRejected = false;

                claimsviewmodel.LoggedInSecurityUserID = Infinedi.AppServices.SessionManager.GetUserId();
                claimsviewmodel.LoggedInClientCode = Infinedi.Domain.Models.InfinediSecurityUserModel.GetClientCode(claimsviewmodel.LoggedInSecurityUserID);
                claimsviewmodel.selectedPerPage = Convert.ToInt16(claimsviewmodel.perPageOptions[0].Substring(0, 2));

                claimsviewmodel.Claims = claimProcessingRepository.GetErroredClaimsForInputID(inputID, null, claimsviewmodel.selectedPerPage, claimsviewmodel.LoggedInSecurityUserID, SortOnColumn, SortOrder, startindex, out totalnumberofrecords);

                claimsviewmodel.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / claimsviewmodel.selectedPerPage);
                claimsviewmodel.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;

                return Json(claimsviewmodel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public JsonResult clickedSearchInBatch(
            DateTime? InputProcessedDateFrom = null,
            DateTime? InputProcessedDateTo = null,
            string InputPatientLastName = "",
            string InputPatientFirstName = "",
                string InputPatientAccountNumber = "",
                string InputTraceNumber = "",
                string InputPayerName = "",
                bool InputIncludeArchived = false,
                bool InputIncludeAttention = false,
                bool InputIncludeOnHold = false,
                bool InputIncludePending = false,
                bool InputIncludeViewed = false,
                bool InputIncludeIgnore= false,
                bool InputIncludeInstitutional = false,
                bool InputIncludeProfessional = false,
                bool InputIncludeDental = false,
                bool InputIncludeAll = false,
                bool InputIncludeWorkerCompType = false,
                string selectedclientcode = "",
                int CurrentPageNummber = 0,
                int ResultsPerPage = 0,
                bool InputIncludeFailed = false,
                bool InputIncludeAccepted = false,
                bool InputIncludeErrored = false,
                string InputClaimType = "",
                string InputSelectedDate = "")
        {
            try
            {
                if (InputPatientLastName == "")
                    InputPatientLastName = null;
                if (InputPatientFirstName == "")
                    InputPatientFirstName = null;
                if (InputPatientAccountNumber == "")
                    InputPatientAccountNumber = null;
                if (InputTraceNumber == "")
                    InputTraceNumber = null;
                if (InputPayerName == "")
                    InputPayerName = null;

                if (ResultsPerPage == 0)
                    claimsviewmodel.selectedPerPage = 10;
                else
                    claimsviewmodel.selectedPerPage = ResultsPerPage;
                if (claimsviewmodel.selectedPerPage == 10)
                    claimsviewmodel.perpagedropdownvalue = "10 per page";
                else if (claimsviewmodel.selectedPerPage == 20)
                    claimsviewmodel.perpagedropdownvalue = "20 per page";
                else if (claimsviewmodel.selectedPerPage == 30)
                    claimsviewmodel.perpagedropdownvalue = "30 per page";

                claimsviewmodel.SearchFromQuickActionAccepted = false;
                claimsviewmodel.SearchFromQuickActionFailed = false;
                claimsviewmodel.LoggedInSecurityUserID = Infinedi.AppServices.SessionManager.GetUserId();
                claimsviewmodel.LoggedInClientCode = Infinedi.Domain.Models.InfinediSecurityUserModel.GetClientCode(claimsviewmodel.LoggedInSecurityUserID);
                claimsviewmodel.InputTraceNumber = InputTraceNumber;
                claimsviewmodel.InputIncludeArchived = InputIncludeArchived;
                claimsviewmodel.InputIncludeAttention = InputIncludeAttention;
                claimsviewmodel.InputIncludeOnHold = InputIncludeOnHold;
                claimsviewmodel.InputIncludePending = InputIncludePending;
                claimsviewmodel.InputIncludeViewed = InputIncludeViewed;
                claimsviewmodel.InputIncludeInstitutional = InputIncludeInstitutional;
                claimsviewmodel.InputIncludeProfessional = InputIncludeProfessional;
                claimsviewmodel.InputIncludeDental = InputIncludeDental;
                claimsviewmodel.InputIncludeAll = InputIncludeAll;
                claimsviewmodel.InputIncludeWorkerCompType = InputIncludeWorkerCompType;
                claimsviewmodel.InputPatientAccountNumber = InputPatientAccountNumber;
                claimsviewmodel.InputPatientFirstName = InputPatientFirstName;
                claimsviewmodel.InputPatientLastName = InputPatientLastName;
                claimsviewmodel.InputPayerName = InputPayerName;
                claimsviewmodel.InputProcessedDateFrom = InputProcessedDateFrom;
                claimsviewmodel.InputProcessedDateTo = InputProcessedDateTo;
                claimsviewmodel.selectedclientcode = selectedclientcode;
                claimsviewmodel.InputIncludeIgnore = InputIncludeIgnore;
                claimsviewmodel.CurrentPageNumber = CurrentPageNummber;
                claimsviewmodel.SearchFromQuickActionAccepted = InputIncludeAccepted;
                claimsviewmodel.SearchFromQuickActionPending = InputIncludeErrored;
                claimsviewmodel.SearchFromQuickActionFailed = InputIncludeFailed;
                claimsviewmodel.InputClaimType = InputClaimType;

                //setting date range when dates are null - date range set to 1 month difference
                if (claimsviewmodel.InputProcessedDateFrom == null || claimsviewmodel.InputProcessedDateTo == null)
                {
                    if (claimsviewmodel.InputProcessedDateFrom == null)
                    {
                        claimsviewmodel.InputProcessedDateFrom = System.DateTime.Today.AddMonths(-1);
                    }
                    if (claimsviewmodel.InputProcessedDateTo == null)
                    {
                        claimsviewmodel.InputProcessedDateTo = DateTime.Today;
                    }
                }

                claimsviewmodel.InputTraceNumber = InputTraceNumber;
                int totalnumberofrecords = 0;
                claimsviewmodel.Claims = claimProcessingRepository.SearchClaims(claimsviewmodel, out totalnumberofrecords);
                claimsviewmodel.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / claimsviewmodel.selectedPerPage);
                claimsviewmodel.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;

                return Json(claimsviewmodel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public JsonResult GetAllPendingClaims(string selectedclientcode, int? CurrentPageNummber, int? ResultsPerPage)
        {
            try
            {
                if (CurrentPageNummber == null)
                {
                    claimsviewmodel.CurrentPageNumber = 1;
                }
                else
                {
                    claimsviewmodel.CurrentPageNumber = Convert.ToInt16(CurrentPageNummber);
                }

                if (ResultsPerPage == null)
                {
                    claimsviewmodel.selectedPerPage = 10;
                }
                else
                {
                    claimsviewmodel.selectedPerPage = Convert.ToInt16(ResultsPerPage);
                }

                int totalnumberofrecords = 0;

                // Reset other sources of entry onto claims processing screen
                claimsviewmodel.SearchFromQuickActionPending = true;
                claimsviewmodel.SearchFromQuickActionAccepted = false;
                claimsviewmodel.SearchFromQuickActionFailed = false;
                claimsviewmodel.SearchFromBatchErrored = false;
                claimsviewmodel.SearchFromBatchClean = false;
                claimsviewmodel.SearchFromBatchRejected = false;
                claimsviewmodel.InputProcessedDateFrom = System.DateTime.Today.AddDays(-7);
                claimsviewmodel.InputProcessedDateTo = System.DateTime.Today.AddHours(23);
                claimsviewmodel.InputIncludePending = true;
                claimsviewmodel.Claims = claimProcessingRepository.SearchClaims(claimsviewmodel, out totalnumberofrecords);

                claimsviewmodel.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / claimsviewmodel.selectedPerPage);
                claimsviewmodel.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;
                claimsviewmodel.InputSelectedAction = "Bulk action";

                return Json(claimsviewmodel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public JsonResult GetAllFailedClaims(string selectedclientcode, int? CurrentPageNummber, int? ResultsPerPage)
        {
            try
            {
                if (CurrentPageNummber == null)
                {
                    claimsviewmodel.CurrentPageNumber = 1;
                }
                else
                {
                    claimsviewmodel.CurrentPageNumber = Convert.ToInt16(CurrentPageNummber);
                }

                if (ResultsPerPage == null)
                {
                    claimsviewmodel.selectedPerPage = 10;
                }
                else
                {
                    claimsviewmodel.selectedPerPage = Convert.ToInt16(ResultsPerPage);
                }

                int totalnumberofrecords = 0;
                claimsviewmodel.selectedPerPage = 10;
                // Reset other sources of entry onto claims processing screen
                claimsviewmodel.SearchFromQuickActionPending = false;
                claimsviewmodel.SearchFromQuickActionAccepted = false;
                claimsviewmodel.SearchFromQuickActionFailed = true;
                claimsviewmodel.SearchFromBatchErrored = false;
                claimsviewmodel.SearchFromBatchClean = false;
                claimsviewmodel.SearchFromBatchRejected = false;
                claimsviewmodel.InputProcessedDateFrom = System.DateTime.Today.AddDays(-7);
                claimsviewmodel.InputProcessedDateTo = System.DateTime.Today.AddHours(23);
                claimsviewmodel.InputIncludePending = true;
                claimsviewmodel.Claims = claimProcessingRepository.SearchClaims(claimsviewmodel, out totalnumberofrecords);
                claimsviewmodel.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / claimsviewmodel.selectedPerPage);
                claimsviewmodel.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;
                claimsviewmodel.InputSelectedAction = "Bulk action";

                return Json(claimsviewmodel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public ActionResult GetAllAcceptedClaims(string selectedclientcode, int? CurrentPageNummber, int? ResultsPerPage)
        {
            try
            {
                if (CurrentPageNummber == null)
                {
                    claimsviewmodel.CurrentPageNumber = 1;
                }
                else
                {
                    claimsviewmodel.CurrentPageNumber = Convert.ToInt16(CurrentPageNummber);
                }

                if (ResultsPerPage == null)
                {
                    claimsviewmodel.selectedPerPage = 10;
                }
                else
                {
                    claimsviewmodel.selectedPerPage = Convert.ToInt16(ResultsPerPage);
                }

                int totalnumberofrecords = 0;
                // Reset other sources of entry onto claims processing screen
                claimsviewmodel.SearchFromQuickActionPending = false;
                claimsviewmodel.SearchFromQuickActionAccepted = true;
                claimsviewmodel.SearchFromQuickActionFailed = false;
                claimsviewmodel.SearchFromBatchErrored = false;
                claimsviewmodel.SearchFromBatchClean = false;
                claimsviewmodel.SearchFromBatchRejected = false;
                claimsviewmodel.InputProcessedDateFrom = System.DateTime.Today.AddDays(-7);
                claimsviewmodel.InputProcessedDateTo = System.DateTime.Today.AddHours(23);
                claimsviewmodel.InputIncludePending = true;
                claimsviewmodel.Claims = claimProcessingRepository.SearchClaims(claimsviewmodel, out totalnumberofrecords);
                claimsviewmodel.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / claimsviewmodel.selectedPerPage);
                claimsviewmodel.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;
                claimsviewmodel.InputSelectedAction = "Bulk action";

                return Json(claimsviewmodel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public JsonResult clickedResubmit(ClaimsProcessingViewModel vm, int ClaimID)
        {
            // TO DO : Add the resubmit log here
            try
            {
                int totalnumberofrecords = 0;
                vm.Claims = claimProcessingRepository.SearchClaims(vm, out totalnumberofrecords);
                vm.TotalNumberOfPages = (int)Math.Ceiling((decimal)totalnumberofrecords / vm.selectedPerPage);
                vm.TotalAmountOnPage = Domain.Models.Claims_Processing.ClaimModel.TotalFileAmount;

                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }
        public JsonResult GetInputFIleID(int ClaimID,string submissionType,string claimType)
        {
            
            try
            {
                Int64 InputFileId = 0;
                InputFileId = claimProcessingRepository.GetInputFileId(ClaimID,submissionType,claimType);

                return Json(InputFileId, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e) 
            {
                var msg = e.Message;
                return null;
            }
        }

        public string GetPayerNameFromEnrollment(string PayerID)
        {
            try
            {
                return claimProcessingRepository.GetPayerNameFromEnrollment(PayerID);
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonResult()
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior,
                MaxJsonLength = Int32.MaxValue
            };
        }
    }
}
